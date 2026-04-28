using CarAssemblyErp.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.ProductionOrders;

public record GetProductionOrderQuery(Guid Id) : IRequest<ProductionOrderDto?>;

public class GetProductionOrderHandler : IRequestHandler<GetProductionOrderQuery, ProductionOrderDto?>
{
    private readonly AppDbContext _db;

    public GetProductionOrderHandler(AppDbContext db) => _db = db;

    public async Task<ProductionOrderDto?> Handle(GetProductionOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _db.ProductionOrders
            .AsNoTracking()
            .Include(o => o.TargetPart)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order == null) return null;

        var ws = order.WorkstationId.HasValue
            ? await _db.Workstations.AsNoTracking().FirstOrDefaultAsync(w => w.Id == order.WorkstationId, cancellationToken)
            : null;

        return CreateProductionOrderHandler.MapToDto(order, order.TargetPart.Name, ws?.Name);
    }
}
