using CarAssemblyErp.Data;
using CarAssemblyErp.Features.BomNodes;
using CarAssemblyErp.Features.Inventory;
using CarAssemblyErp.Features.Parts;
using CarAssemblyErp.Features.ProductionOrders;
using CarAssemblyErp.Features.Workstations;
using CarAssemblyErp.Middleware;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var app = builder.Build();

app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Health check
app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1");
        return Results.Ok(new { status = "healthy", database = "connected" });
    }
    catch (Exception ex)
    {
        return Results.StatusCode(503);
    }
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
