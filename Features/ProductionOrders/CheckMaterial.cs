using CarAssemblyErp.Data;
using CarAssemblyErp.Domain.Enums;
using CarAssemblyErp.Infrastructure.Redis;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.ProductionOrders;

public record CheckMaterialCommand(Guid Id) : IRequest<MaterialCheckResult>;

public class CheckMaterialHandler : IRequestHandler<CheckMaterialCommand, MaterialCheckResult>
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CheckMaterialHandler(AppDbContext db, ICacheService cache, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<MaterialCheckResult> Handle(CheckMaterialCommand request, CancellationToken cancellationToken)
    {
        _httpContextAccessor.HttpContext?.Items.Add("DB", "Primary");
        _httpContextAccessor.HttpContext?.Items.Add("Cache", "Miss");

        var order = await _db.ProductionOrders
            .Include(o => o.TargetPart)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order == null)
            throw new Common.BusinessException("OrderNotFound", $"Production order '{request.Id}' not found.");

        if (order.Status != ProductionStatus.Draft && order.Status != ProductionStatus.MaterialShortage)
            throw new Common.BusinessException("InvalidStatus", $"Cannot check material for order in status '{order.Status}'.");

        var bomNodes = await _db.BomNodes.AsNoTracking().ToListAsync(cancellationToken);
        var parts = await _db.Parts.AsNoTracking().ToListAsync(cancellationToken);
        var partDict = parts.ToDictionary(p => p.Id);

        var requirements = new Dictionary<Guid, int>();

        void Collect(Guid partId, int multiplier, HashSet<Guid> path)
        {
            if (path.Contains(partId))
                throw new Common.BusinessException("CircularReference", "BOM contains a circular reference.");

            var children = bomNodes.Where(n => n.ParentPartId == partId).ToList();
            foreach (var child in children)
            {
                var req = child.Quantity * multiplier;
                requirements[child.ChildPartId] = requirements.GetValueOrDefault(child.ChildPartId) + req;
                var newPath = new HashSet<Guid>(path) { partId };
                Collect(child.ChildPartId, req, newPath);
            }
        }

        Collect(order.TargetPartId, order.Quantity, new HashSet<Guid>());

        var shortages = new List<MaterialShortage>();
        foreach (var (partId, required) in requirements)
        {
            if (!partDict.TryGetValue(partId, out var part)) continue;
            if (part.StockQuantity < required)
            {
                shortages.Add(new MaterialShortage(partId, part.Sku, part.Name, required, part.StockQuantity, required - part.StockQuantity));
            }
        }

        if (shortages.Count == 0)
        {
            order.Status = ProductionStatus.Ready;
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            order.Status = ProductionStatus.MaterialShortage;
            await _db.SaveChangesAsync(cancellationToken);
        }

        // 清除生产订单缓存和安全库存缓存
        await _cache.RemoveAsync($"po:{order.Id}", cancellationToken);
        await _cache.RemoveAsync("parts:low-stock", cancellationToken);

        return new MaterialCheckResult(
            shortages.Count == 0 ? "Ready" : "MaterialShortage",
            shortages);
    }
}
