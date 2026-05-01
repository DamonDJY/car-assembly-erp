using CarAssemblyErp.Data;
using CarAssemblyErp.Domain.Enums;
using CarAssemblyErp.Infrastructure.Redis;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.ProductionOrders;

public record StartProductionCommand(Guid Id) : IRequest<ProductionOrderDto>;

public class StartProductionHandler : IRequestHandler<StartProductionCommand, ProductionOrderDto>
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StartProductionHandler(AppDbContext db, ICacheService cache, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ProductionOrderDto> Handle(StartProductionCommand request, CancellationToken cancellationToken)
    {
        _httpContextAccessor.HttpContext?.Items.Add("DB", "Primary");
        _httpContextAccessor.HttpContext?.Items.Add("Cache", "Miss");

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

        // 清除缓存
        await _cache.RemoveAsync($"po:{order.Id}", cancellationToken);
        await _cache.RemoveAsync("parts:low-stock", cancellationToken);

        var ws = order.WorkstationId.HasValue
            ? await _db.Workstations.AsNoTracking().FirstOrDefaultAsync(w => w.Id == order.WorkstationId, cancellationToken)
            : null;

        return CreateProductionOrderHandler.MapToDto(order, order.TargetPart.Name, ws?.Name);
    }
}
