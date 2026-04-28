using CarAssemblyErp.Data;
using CarAssemblyErp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.ProductionOrders;

public record CheckMaterialCommand(Guid Id) : IRequest<MaterialCheckResult>;

public class CheckMaterialHandler : IRequestHandler<CheckMaterialCommand, MaterialCheckResult>
{
    private readonly AppDbContext _db;

    public CheckMaterialHandler(AppDbContext db) => _db = db;

    public async Task<MaterialCheckResult> Handle(CheckMaterialCommand request, CancellationToken cancellationToken)
    {
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

        var requirements = new Dictionary<Guid, decimal>();

        void Collect(Guid partId, decimal multiplier)
        {
            var children = bomNodes.Where(n => n.ParentPartId == partId).ToList();
            foreach (var child in children)
            {
                var req = child.Quantity * multiplier;
                if (!requirements.ContainsKey(child.ChildPartId))
                    requirements[child.ChildPartId] = 0;
                requirements[child.ChildPartId] += req;
                Collect(child.ChildPartId, req);
            }
        }

        Collect(order.TargetPartId, order.Quantity);

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
            return new MaterialCheckResult("Ready", new List<MaterialShortage>());
        }
        else
        {
            order.Status = ProductionStatus.MaterialShortage;
            await _db.SaveChangesAsync(cancellationToken);
            return new MaterialCheckResult("MaterialShortage", shortages);
        }
    }
}
