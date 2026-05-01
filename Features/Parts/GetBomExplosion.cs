using CarAssemblyErp.Data;
using CarAssemblyErp.Domain.Entities;
using CarAssemblyErp.Infrastructure.Database;
using CarAssemblyErp.Infrastructure.Redis;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.Parts;

public record GetBomExplosionQuery(Guid PartId) : IRequest<List<BomExplosionItem>>;

public class GetBomExplosionHandler : IRequestHandler<GetBomExplosionQuery, List<BomExplosionItem>>
{
    private readonly AppReadDbContext _readDb;
    private readonly AppDbContext _primaryDb;
    private readonly ICacheService _cache;
    private readonly IConnectionRouter _router;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetBomExplosionHandler(
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

    public async Task<List<BomExplosionItem>> Handle(GetBomExplosionQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"bom:{request.PartId}:v1";
        var emptyKey = $"{cacheKey}:empty";

        // 1. 检查缓存
        var cached = await _cache.GetAsync<List<BomExplosionItem>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _httpContextAccessor.HttpContext?.Items.Add("DB", "Replica");
            _httpContextAccessor.HttpContext?.Items.Add("Cache", "Hit");
            return cached;
        }

        // 2. 检查空值缓存（缓存穿透防护）
        if (await _cache.ExistsAsync(emptyKey, cancellationToken))
        {
            _httpContextAccessor.HttpContext?.Items.Add("DB", "Replica");
            _httpContextAccessor.HttpContext?.Items.Add("Cache", "Hit");
            return new List<BomExplosionItem>();
        }

        // 3. 获取分布式锁防止缓存击穿
        var lockKey = $"lock:{cacheKey}";
        var acquired = await _cache.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10), cancellationToken);

        try
        {
            // 双重检查
            cached = await _cache.GetAsync<List<BomExplosionItem>>(cacheKey, cancellationToken);
            if (cached != null)
            {
                _httpContextAccessor.HttpContext?.Items.Add("DB", "Replica");
                _httpContextAccessor.HttpContext?.Items.Add("Cache", "Hit");
                return cached;
            }

            // 4. 查询数据库（Replica）
            var db = _readDb;
            _httpContextAccessor.HttpContext?.Items.Add("DB", "Replica");
            _httpContextAccessor.HttpContext?.Items.Add("Cache", "Miss");

            // 先验证 Part 是否存在
            var partExists = await db.Parts.AsNoTracking().AnyAsync(p => p.Id == request.PartId, cancellationToken);
            if (!partExists)
            {
                // 缓存空值防止穿透
                await _cache.SetEmptyAsync(emptyKey, TimeSpan.FromMinutes(5), cancellationToken);
                return new List<BomExplosionItem>();
            }

            var bomNodes = await db.BomNodes.AsNoTracking().ToListAsync(cancellationToken);
            var parts = await db.Parts.AsNoTracking().ToListAsync(cancellationToken);
            var partDict = parts.ToDictionary(p => p.Id);

            var result = new List<BomExplosionItem>();

            void Explode(Guid partId, int multiplier, int level, HashSet<Guid> path)
            {
                if (path.Contains(partId))
                    throw new Common.BusinessException("CircularReference", "BOM contains a circular reference.");

                var children = bomNodes.Where(n => n.ParentPartId == partId).ToList();
                foreach (var child in children)
                {
                    if (!partDict.TryGetValue(child.ChildPartId, out var childPart)) continue;

                    var totalQty = child.Quantity * multiplier;
                    result.Add(new BomExplosionItem(child.ChildPartId, childPart.Sku, childPart.Name, totalQty, level + 1));

                    var newPath = new HashSet<Guid>(path) { partId };
                    Explode(child.ChildPartId, totalQty, level + 1, newPath);
                }
            }

            Explode(request.PartId, 1, 0, new HashSet<Guid>());

            // 5. 写入缓存（TTL 1小时，带随机偏移已在 RedisCacheService 中实现）
            await _cache.SetAsync(cacheKey, result, absoluteExpiration: TimeSpan.FromHours(1), ct: cancellationToken);

            return result;
        }
        finally
        {
            if (acquired)
                await _cache.ReleaseLockAsync(lockKey, cancellationToken);
        }
    }
}
