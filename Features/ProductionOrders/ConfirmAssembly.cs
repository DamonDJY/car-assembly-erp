using CarAssemblyErp.Data;
using CarAssemblyErp.Domain.Entities;
using CarAssemblyErp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.ProductionOrders;

public record ConfirmAssemblyCommand(Guid Id) : IRequest<ProductionOrderDto>;

public class ConfirmAssemblyHandler : IRequestHandler<ConfirmAssemblyCommand, ProductionOrderDto>
{
    private readonly AppDbContext _db;

    public ConfirmAssemblyHandler(AppDbContext db) => _db = db;

    public async Task<ProductionOrderDto> Handle(ConfirmAssemblyCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.ProductionOrders
            .Include(o => o.TargetPart)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order == null)
            throw new Common.BusinessException("OrderNotFound", $"Production order '{request.Id}' not found.");

        if (order.Status != ProductionStatus.InProgress)
            throw new Common.BusinessException("InvalidStatus", "Only InProgress orders can confirm assembly.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var bomNodes = await _db.BomNodes.AsNoTracking().ToListAsync(cancellationToken);
        var parts = await _db.Parts.ToListAsync(cancellationToken);
        var partDict = parts.ToDictionary(p => p.Id);

        var requirements = new Dictionary<Guid, decimal>();
        void Collect(Guid partId, decimal multiplier)
        {
            var children = bomNodes.Where(n => n.ParentPartId == partId).ToList();
            foreach (var child in children)
            {
                if (!requirements.ContainsKey(child.ChildPartId))
                    requirements[child.ChildPartId] = 0;
                requirements[child.ChildPartId] += child.Quantity * multiplier;
                Collect(child.ChildPartId, child.Quantity * multiplier);
            }
        }
        Collect(order.TargetPartId, 1);

        foreach (var (partId, required) in requirements)
        {
            if (!partDict.TryGetValue(partId, out var part))
                throw new Common.BusinessException("PartNotFound", $"Part '{partId}' not found in BOM.");

            if (part.StockQuantity < required)
                throw new Common.BusinessException("InsufficientStock", $"Part '{part.Sku}' stock insufficient for assembly. Required: {required}, Available: {part.StockQuantity}");
        }

        foreach (var (partId, required) in requirements)
        {
            var part = partDict[partId];
            part.StockQuantity -= required;
            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                PartId = part.Id,
                TransactionType = TransactionType.ProductionConsume,
                Quantity = -required,
                RunningBalance = part.StockQuantity,
                ReferenceNumber = order.OrderNumber,
                CreatedAt = DateTime.UtcNow
            });
        }

        var targetPart = partDict[order.TargetPartId];
        targetPart.StockQuantity += 1;
        _db.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            PartId = targetPart.Id,
            TransactionType = TransactionType.ProductionOutput,
            Quantity = 1,
            RunningBalance = targetPart.StockQuantity,
            ReferenceNumber = order.OrderNumber,
            CreatedAt = DateTime.UtcNow
        });

        order.CompletedQuantity += 1;
        if (order.CompletedQuantity >= order.Quantity)
        {
            order.Status = ProductionStatus.Completed;
            order.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var ws = order.WorkstationId.HasValue
            ? await _db.Workstations.AsNoTracking().FirstOrDefaultAsync(w => w.Id == order.WorkstationId, cancellationToken)
            : null;

        return CreateProductionOrderHandler.MapToDto(order, order.TargetPart.Name, ws?.Name);
    }
}
