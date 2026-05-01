using CarAssemblyErp.Data;
using CarAssemblyErp.Domain.Entities;
using CarAssemblyErp.Domain.Enums;
using CarAssemblyErp.Infrastructure.Redis;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.Inventory;

public record InboundCommand(Guid PartId, int Quantity) : IRequest<Parts.PartDto>;

public class InboundHandler : IRequestHandler<InboundCommand, Parts.PartDto>
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public InboundHandler(AppDbContext db, ICacheService cache, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Parts.PartDto> Handle(InboundCommand request, CancellationToken cancellationToken)
    {
        _httpContextAccessor.HttpContext?.Items.Add("DB", "Primary");
        _httpContextAccessor.HttpContext?.Items.Add("Cache", "Miss");

        if (request.Quantity <= 0)
            throw new Common.BusinessException("InvalidQuantity", "Quantity must be positive.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var part = await _db.Parts.FirstOrDefaultAsync(p => p.Id == request.PartId, cancellationToken);
        if (part == null)
            throw new Common.BusinessException("PartNotFound", $"Part '{request.PartId}' not found.");

        part.StockQuantity += request.Quantity;

        _db.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            PartId = part.Id,
            TransactionType = TransactionType.Inbound,
            Quantity = request.Quantity,
            RunningBalance = part.StockQuantity,
            ReferenceType = "Inbound",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // 清除相关缓存
        await _cache.RemoveAsync($"part:{part.Id}", cancellationToken);
        await _cache.RemoveAsync("parts:low-stock", cancellationToken);

        return new Parts.PartDto(part.Id, part.Sku, part.Name, part.Specification, part.Unit, part.SafetyStock, part.StockQuantity, part.CreatedAt);
    }
}
