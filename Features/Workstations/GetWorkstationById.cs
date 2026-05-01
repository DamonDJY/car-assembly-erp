using CarAssemblyErp.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.Workstations;

public record GetWorkstationByIdQuery(Guid Id) : IRequest<WorkstationDto?>;

public class GetWorkstationByIdHandler : IRequestHandler<GetWorkstationByIdQuery, WorkstationDto?>
{
    private readonly AppReadDbContext _readDb;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetWorkstationByIdHandler(AppReadDbContext readDb, IHttpContextAccessor httpContextAccessor)
    {
        _readDb = readDb;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkstationDto?> Handle(GetWorkstationByIdQuery request, CancellationToken cancellationToken)
    {
        _httpContextAccessor.HttpContext?.Items.Add("DB", "Replica");
        _httpContextAccessor.HttpContext?.Items.Add("Cache", "Miss");

        var ws = await _readDb.Workstations.AsNoTracking().FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);
        if (ws == null) return null;
        return new WorkstationDto(ws.Id, ws.Name, ws.Location, ws.IsActive);
    }
}
