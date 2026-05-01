using CarAssemblyErp.Data;
using CarAssemblyErp.Domain.Entities;
using CarAssemblyErp.Infrastructure.Redis;
using MediatR;

namespace CarAssemblyErp.Features.Workstations;

public record CreateWorkstationCommand(string Name, string? Location) : IRequest<WorkstationDto>;

public record WorkstationDto(Guid Id, string Name, string? Location, bool IsActive);

public class CreateWorkstationHandler : IRequestHandler<CreateWorkstationCommand, WorkstationDto>
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateWorkstationHandler(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkstationDto> Handle(CreateWorkstationCommand request, CancellationToken cancellationToken)
    {
        _httpContextAccessor.HttpContext?.Items.Add("DB", "Primary");
        _httpContextAccessor.HttpContext?.Items.Add("Cache", "Miss");

        var ws = new Workstation
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Location = request.Location?.Trim(),
            IsActive = true
        };

        _db.Workstations.Add(ws);
        await _db.SaveChangesAsync(cancellationToken);

        return new WorkstationDto(ws.Id, ws.Name, ws.Location, ws.IsActive);
    }
}
