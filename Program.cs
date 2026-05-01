using CarAssemblyErp.Data;
using CarAssemblyErp.Features.BomNodes;
using CarAssemblyErp.Features.Inventory;
using CarAssemblyErp.Features.Parts;
using CarAssemblyErp.Features.ProductionOrders;
using CarAssemblyErp.Features.Workstations;
using CarAssemblyErp.Infrastructure.Database;
using CarAssemblyErp.Infrastructure.Redis;
using CarAssemblyErp.Middleware;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Redis
var redisConnectionString = builder.Configuration.GetSection("Redis")["ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Connection Router (for read/write splitting + read-your-write consistency)
builder.Services.AddScoped<IConnectionRouter, PrimaryReplicaRouter>();

// HttpContextAccessor for passing DB/Cache state to middleware
builder.Services.AddHttpContextAccessor();

// DbContexts: Primary for writes, Replica for reads
builder.Services.AddDbContext<AppDbContext>((provider, options) =>
{
    var router = provider.GetRequiredService<IConnectionRouter>();
    options.UseNpgsql(router.GetPrimaryConnectionString());
});

builder.Services.AddDbContext<AppReadDbContext>((provider, options) =>
{
    var router = provider.GetRequiredService<IConnectionRouter>();
    options.UseNpgsql(router.GetReplicaConnectionString());
});

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var app = builder.Build();

app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 数据库初始化：开发环境快速创建，生产环境执行 Migration
using (var scope = app.Services.CreateScope())
{
    var primaryDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var replicaDb = scope.ServiceProvider.GetRequiredService<AppReadDbContext>();

    if (app.Environment.IsDevelopment())
    {
        primaryDb.Database.EnsureCreated();
        // Replica is read-only in streaming replication, EnsureCreated will fail there
        // but for local dev with bitnami/repmgr, replica is read-only so we skip
        try { replicaDb.Database.ExecuteSqlRaw("SELECT 1"); } catch { /* replica may be read-only */ }
    }
    else
    {
        try
        {
            primaryDb.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Database migration failed. Please ensure the connection string is correct and the database is accessible.");
            throw;
        }
    }
}

// Health check
app.MapGet("/health", async (AppDbContext primaryDb, AppReadDbContext replicaDb, IConnectionMultiplexer redis) =>
{
    var results = new Dictionary<string, string>();

    try
    {
        await primaryDb.Database.ExecuteSqlRawAsync("SELECT 1");
        results["Primary"] = "connected";
    }
    catch (Exception ex)
    {
        results["Primary"] = $"error: {ex.Message}";
    }

    try
    {
        await replicaDb.Database.ExecuteSqlRawAsync("SELECT 1");
        results["Replica"] = "connected";
    }
    catch (Exception ex)
    {
        results["Replica"] = $"error: {ex.Message}";
    }

    try
    {
        await redis.GetDatabase().PingAsync();
        results["Redis"] = "connected";
    }
    catch (Exception ex)
    {
        results["Redis"] = $"error: {ex.Message}";
    }

    var allHealthy = results.Values.All(v => v == "connected");
    return allHealthy
        ? Results.Ok(new { status = "healthy", details = results })
        : Results.Json(new { status = "degraded", details = results }, statusCode: 503);
});

// Parts
app.MapPost("/api/parts", async (CreatePartCommand cmd, IMediator mediator) =>
{
    var result = await mediator.Send(cmd);
    return Results.Created($"/api/parts/{result.Id}", result);
});

app.MapGet("/api/parts/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetPartByIdQuery(id));
    return result == null ? Results.NotFound() : Results.Ok(result);
});

app.MapGet("/api/parts/{id:guid}/bom-explosion", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetBomExplosionQuery(id));
    return Results.Ok(result);
});

app.MapGet("/api/parts/low-stock", async (IMediator mediator) =>
{
    var result = await mediator.Send(new GetLowStockQuery());
    return Results.Ok(result);
});

// BOM Nodes
app.MapPost("/api/bom-nodes", async (CreateBomNodeCommand cmd, IMediator mediator) =>
{
    var result = await mediator.Send(cmd);
    return Results.Created($"/api/bom-nodes/{result.Id}", result);
});

// Inventory
app.MapPost("/api/inventory/inbound", async (InboundCommand cmd, IMediator mediator) =>
{
    var result = await mediator.Send(cmd);
    return Results.Ok(result);
});

app.MapPost("/api/inventory/outbound", async (OutboundCommand cmd, IMediator mediator) =>
{
    var result = await mediator.Send(cmd);
    return Results.Ok(result);
});

// Workstations
app.MapPost("/api/workstations", async (CreateWorkstationCommand cmd, IMediator mediator) =>
{
    var result = await mediator.Send(cmd);
    return Results.Created($"/api/workstations/{result.Id}", result);
});

app.MapGet("/api/workstations/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetWorkstationByIdQuery(id));
    return result == null ? Results.NotFound() : Results.Ok(result);
});

// Production Orders
app.MapPost("/api/production-orders", async (CreateProductionOrderCommand cmd, IMediator mediator) =>
{
    var result = await mediator.Send(cmd);
    return Results.Created($"/api/production-orders/{result.Id}", result);
});

app.MapGet("/api/production-orders/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetProductionOrderQuery(id));
    return result == null ? Results.NotFound() : Results.Ok(result);
});

app.MapPost("/api/production-orders/{id:guid}/check-material", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new CheckMaterialCommand(id));
    return Results.Ok(result);
});

app.MapPost("/api/production-orders/{id:guid}/start", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new StartProductionCommand(id));
    return Results.Ok(result);
});

app.MapPost("/api/production-orders/{id:guid}/confirm-assembly", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new ConfirmAssemblyCommand(id));
    return Results.Ok(result);
});

app.Run();
