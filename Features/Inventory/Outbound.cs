using CarAssemblyErp.Data;
using CarAssemblyErp.Domain.Entities;
using CarAssemblyErp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.Inventory;

public record OutboundCommand(Guid PartId, int Quantity) : IRequest<Parts.PartDto>;

public class OutboundHandler : IRequestHandler<OutboundCommand, Parts.PartDto>
{
    private readonly AppDbContext _db;

    public OutboundHandler(AppDbContext db) => _db = db;

    public async Task<Parts.PartDto> Handle(OutboundCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            throw new Common.BusinessException("InvalidQuantity", "Quantity must be positive.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var part = await _db.Parts.FirstOrDefaultAsync(p => p.Id == request.PartId, cancellationToken);
        if (part == null)
            throw new Common.BusinessException("PartNotFound", $"Part '{request.PartId}' not found.");

        if (part.StockQuantity < request.Quantity)
            throw new Common.BusinessException("InsufficientStock", $"Part '{part.Sku}' stock insufficient. Required: {request.Quantity}, Available: {part.StockQuantity}");

        part.StockQuantity -= request.Quantity;

        _db.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            PartId = part.Id,
            TransactionType = TransactionType.Outbound,
            Quantity = -request.Quantity,
            RunningBalance = part.StockQuantity,
            ReferenceType = "Outbound",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new Parts.PartDto(part.Id, part.Sku, part.Name, part.Specification, part.Unit, part.SafetyStock, part.StockQuantity, part.CreatedAt);
    }
}
