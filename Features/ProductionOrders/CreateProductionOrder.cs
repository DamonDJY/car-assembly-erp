using CarAssemblyErp.Data;
using CarAssemblyErp.Domain.Entities;
using CarAssemblyErp.Domain.Enums;
using CarAssemblyErp.Infrastructure.Database;
using CarAssemblyErp.Infrastructure.Redis;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.ProductionOrders;

public record CreateProductionOrderCommand(Guid TargetPartId, int Quantity, DateTime PlannedStartDate, Guid? WorkstationId) : IRequest<ProductionOrderDto>;

public record ProductionOrderDto(
    Guid Id,
    string OrderNumber,
    Guid TargetPartId,
    string TargetPartName,
    int Quantity,
    int CompletedQuantity,
    string Status,
    DateTime PlannedStartDate,
    DateTime? ActualStartDate,
    DateTime? CompletedAt,
    Guid? WorkstationId,
    string? WorkstationName,
    DateTime CreatedAt);

public record MaterialCheckResult(string Status, List<MaterialShortage> Shortages);

public record MaterialShortage(Guid PartId, string Sku, string Name, int Required, int Available, int Short);

public class CreateProductionOrderHandler : IRequestHandler<CreateProductionOrderCommand, ProductionOrderDto>
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IConnectionRouter _router;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly SemaphoreSlim _orderNumberLock = new(1, 1);

    public CreateProductionOrderHandler(AppDbContext db, ICacheService cache, IConnectionRouter router, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _cache = cache;
        _router = router;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ProductionOrderDto> Handle(CreateProductionOrderCommand request, CancellationToken cancellationToken)
    {
        _httpContextAccessor.HttpContext?.Items.Add("DB", "Primary");
        _httpContextAccessor.HttpContext?.Items.Add("Cache", "Miss");

        var orderNumber = await GenerateOrderNumberAsync(cancellationToken);

        var order = new ProductionOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            TargetPartId = request.TargetPartId,
            Quantity = request.Quantity,
            CompletedQuantity = 0,
            Status = ProductionStatus.Draft,
            PlannedStartDate = request.PlannedStartDate,
            WorkstationId = request.WorkstationId,
            CreatedAt = DateTime.UtcNow
        };

        _db.ProductionOrders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        // 写后读一致性标记：5秒内读取走 Primary
        await _router.MarkAsWrittenAsync($"po:{order.Id}", TimeSpan.FromSeconds(5));

        // 清除安全库存缓存
        await _cache.RemoveAsync("parts:low-stock", cancellationToken);

        var part = await _db.Parts.AsNoTracking().FirstAsync(p => p.Id == order.TargetPartId, cancellationToken);
        var ws = order.WorkstationId.HasValue
            ? await _db.Workstations.AsNoTracking().FirstOrDefaultAsync(w => w.Id == order.WorkstationId, cancellationToken)
            : null;

        return MapToDto(order, part.Name, ws?.Name);
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        await _orderNumberLock.WaitAsync(cancellationToken);
        try
        {
            var datePrefix = $"PO-{DateTime.UtcNow:yyyyMMdd}-";
            var lastOrder = await _db.ProductionOrders.AsNoTracking()
                .Where(o => o.OrderNumber.StartsWith(datePrefix))
                .OrderByDescending(o => o.OrderNumber)
                .FirstOrDefaultAsync(cancellationToken);

            int seq = 1;
            if (lastOrder != null)
            {
                var lastSeq = lastOrder.OrderNumber[(lastOrder.OrderNumber.LastIndexOf('-') + 1)..];
                if (int.TryParse(lastSeq, out var parsed))
                    seq = parsed + 1;
            }

            return $"{datePrefix}{seq:D4}";
        }
        finally
        {
            _orderNumberLock.Release();
        }
    }

    public static ProductionOrderDto MapToDto(ProductionOrder order, string targetPartName, string? workstationName)
    {
        return new ProductionOrderDto(
            order.Id,
            order.OrderNumber,
            order.TargetPartId,
            targetPartName,
            order.Quantity,
            order.CompletedQuantity,
            order.Status.ToString(),
            order.PlannedStartDate,
            order.ActualStartDate,
            order.CompletedAt,
            order.WorkstationId,
            workstationName,
            order.CreatedAt);
    }
}
