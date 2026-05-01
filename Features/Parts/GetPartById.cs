using CarAssemblyErp.Data;
using CarAssemblyErp.Infrastructure.Redis;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.Parts;

public record GetPartByIdQuery(Guid Id) : IRequest<PartDto?>;

public class GetPartByIdHandler : IRequestHandler<GetPartByIdQuery, PartDto?>
{
    private readonly AppReadDbContext _readDb;
    private readonly ICacheService _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetPartByIdHandler(AppReadDbContext readDb, ICacheService cache, IHttpContextAccessor httpContextAccessor)
    {
        _readDb = readDb;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PartDto?> Handle(GetPartByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"part:{request.Id}";

        // 检查缓存
        var cached = await _cache.GetAsync<PartDto>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _httpContextAccessor.HttpContext?.Items.Add("DB", "Replica");
            _httpContextAccessor.HttpContext?.Items.Add("Cache", "Hit");
            return cached;
        }

        _httpContextAccessor.HttpContext?.Items.Add("DB", "Replica");
        _httpContextAccessor.HttpContext?.Items.Add("Cache", "Miss");

        var part = await _readDb.Parts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (part == null) return null;

        var dto = new PartDto(part.Id, part.Sku, part.Name, part.Specification, part.Unit, part.SafetyStock, part.StockQuantity, part.CreatedAt);

        // 写入缓存，TTL 10分钟
        await _cache.SetAsync(cacheKey, dto, absoluteExpiration: TimeSpan.FromMinutes(10), ct: cancellationToken);

        return dto;
    }
}
