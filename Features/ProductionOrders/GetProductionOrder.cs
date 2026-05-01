using CarAssemblyErp.Data;
using CarAssemblyErp.Infrastructure.Database;
using CarAssemblyErp.Infrastructure.Redis;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.ProductionOrders;

public record GetProductionOrderQuery(Guid Id) : IRequest<ProductionOrderDto?>;

public class GetProductionOrderHandler : IRequestHandler<GetProductionOrderQuery, ProductionOrderDto?>
{
    private readonly AppReadDbContext _readDb;
    private readonly AppDbContext _primaryDb;
    private readonly ICacheService _cache;
    private readonly IConnectionRouter _router;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetProductionOrderHandler(
        AppReadDbContext readDb,
        AppDbContext primaryDb,
        ICacheService cache,
        IConnectionRouter router,
        IHttpContextAccessor httpContextAccessor)
    {
        _readDb = readDb;
        _primaryDb = primaryDb;
        _cache = cache;
        _router = router;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ProductionOrderDto?> Handle(GetProductionOrderQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"po:{request.Id}";

        // 写后读一致性检查
        var recentlyWritten = await _router.IsRecentlyWrittenAsync(cacheKey);
        if (recentlyWritten)
        {
            _httpContextAccessor.HttpContext?.Items.Add("DB", "Primary");
            _httpContextAccessor.HttpContext?.Items.Add("Cache", "Miss");

            var order = await _primaryDb.ProductionOrders
                .AsNoTracking()
                .Include(o => o.TargetPart)
                .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

            if (order == null) return null;

            var ws = order.WorkstationId.HasValue
                ? await _primaryDb.Workstations.AsNoTracking().FirstOrDefaultAsync(w => w.Id == order.WorkstationId, cancellationToken)
                : null;

            return CreateProductionOrderHandler.MapToDto(order, order.TargetPart.Name, ws?.Name);
        }

        // 正常读流程：先查缓存
        var cached = await _cache.GetAsync<ProductionOrderDto>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _httpContextAccessor.HttpContext?.Items.Add("DB", "Replica");
            _httpContextAccessor.HttpContext?.Items.Add("Cache", "Hit");
            return cached;
        }

        _httpContextAccessor.HttpContext?.Items.Add("DB", "Replica");
        _httpContextAccessor.HttpContext?.Items.Add("Cache", "Miss");

        var orderFromDb = await _readDb.ProductionOrders
            .AsNoTracking()
            .Include(o => o.TargetPart)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (orderFromDb == null) return null;

        var wsFromDb = orderFromDb.WorkstationId.HasValue
            ? await _readDb.Workstations.AsNoTracking().FirstOrDefaultAsync(w => w.Id == orderFromDb.WorkstationId, cancellationToken)
            : null;

        var dto = CreateProductionOrderHandler.MapToDto(orderFromDb, orderFromDb.TargetPart.Name, wsFromDb?.Name);

        // TTL 5分钟
        await _cache.SetAsync(cacheKey, dto, absoluteExpiration: TimeSpan.FromMinutes(5), ct: cancellationToken);

        return dto;
    }
}
