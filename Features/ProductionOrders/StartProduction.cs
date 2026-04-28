using CarAssemblyErp.Data;
using CarAssemblyErp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.ProductionOrders;

public record StartProductionCommand(Guid Id) : IRequest<ProductionOrderDto>;

public class StartProductionHandler : IRequestHandler<StartProductionCommand, ProductionOrderDto>
{
    private readonly AppDbContext _db;

    public StartProductionHandler(AppDbContext db) => _db = db;

    public async Task<ProductionOrderDto> Handle(StartProductionCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.ProductionOrders
            .Include(o => o.TargetPart)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order == null)
            throw new Common.BusinessException("OrderNotFound", $"Production order '{request.Id}' not found.");

        if (order.Status != ProductionStatus.Ready)
            throw new Common.BusinessException("InvalidStatus", "Only Ready orders can be started.");

        order.Status = ProductionStatus.InProgress;
        order.ActualStartDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var ws = order.WorkstationId.HasValue
            ? await _db.Workstations.AsNoTracking().FirstOrDefaultAsync(w => w.Id == order.WorkstationId, cancellationToken)
            : null;

        return CreateProductionOrderHandler.MapToDto(order, order.TargetPart.Name, ws?.Name);
    }
}
