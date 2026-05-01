using CarAssemblyErp.Data;
using CarAssemblyErp.Infrastructure.Redis;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.Parts;

public record GetLowStockQuery() : IRequest<List<LowStockItem>>;

public class GetLowStockHandler : IRequestHandler<GetLowStockQuery, List<LowStockItem>>
{
    private readonly AppReadDbContext _readDb;
    private readonly ICacheService _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetLowStockHandler(AppReadDbContext readDb, ICacheService cache, IHttpContextAccessor httpContextAccessor)
    {
        _readDb = readDb;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<LowStockItem>> Handle(GetLowStockQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = "parts:low-stock";

        var cached = await _cache.GetAsync<List<LowStockItem>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _httpContextAccessor.HttpContext?.Items.Add("DB", "Replica");
            _httpContextAccessor.HttpContext?.Items.Add("Cache", "Hit");
            return cached;
        }

        _httpContextAccessor.HttpContext?.Items.Add("DB", "Replica");
        _httpContextAccessor.HttpContext?.Items.Add("Cache", "Miss");

        var items = await _readDb.Parts.AsNoTracking()
            .Where(p => p.StockQuantity < p.SafetyStock)
            .OrderBy(p => p.StockQuantity)
            .Select(p => new LowStockItem(p.Id, p.Sku, p.Name, p.StockQuantity, p.SafetyStock, p.SafetyStock - p.StockQuantity))
            .ToListAsync(cancellationToken);

        // TTL 1分钟
        await _cache.SetAsync(cacheKey, items, absoluteExpiration: TimeSpan.FromMinutes(1), ct: cancellationToken);

        return items;
    }
}
