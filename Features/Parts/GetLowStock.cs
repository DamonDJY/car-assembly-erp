using CarAssemblyErp.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.Parts;

public record GetLowStockQuery() : IRequest<List<LowStockItem>>;

public class GetLowStockHandler : IRequestHandler<GetLowStockQuery, List<LowStockItem>>
{
    private readonly AppDbContext _db;

    public GetLowStockHandler(AppDbContext db) => _db = db;

    public async Task<List<LowStockItem>> Handle(GetLowStockQuery request, CancellationToken cancellationToken)
    {
        var items = await _db.Parts.AsNoTracking()
            .Where(p => p.StockQuantity < p.SafetyStock)
            .OrderBy(p => p.StockQuantity)
            .Select(p => new LowStockItem(p.Id, p.Sku, p.Name, p.StockQuantity, p.SafetyStock, p.SafetyStock - p.StockQuantity))
            .ToListAsync(cancellationToken);
        return items;
    }
}
