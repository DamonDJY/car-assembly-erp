using CarAssemblyErp.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.Workstations;

public record GetWorkstationByIdQuery(Guid Id) : IRequest<WorkstationDto?>;

public class GetWorkstationByIdHandler : IRequestHandler<GetWorkstationByIdQuery, WorkstationDto?>
{
    private readonly AppDbContext _db;

    public GetWorkstationByIdHandler(AppDbContext db) => _db = db;

    public async Task<WorkstationDto?> Handle(GetWorkstationByIdQuery request, CancellationToken cancellationToken)
    {
        var ws = await _db.Workstations.AsNoTracking().FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);
        if (ws == null) return null;
        return new WorkstationDto(ws.Id, ws.Name, ws.Location, ws.IsActive);
    }
}
