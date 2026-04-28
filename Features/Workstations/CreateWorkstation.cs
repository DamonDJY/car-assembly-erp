using CarAssemblyErp.Data;
using CarAssemblyErp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.Workstations;

public record CreateWorkstationCommand(string Name, string? Location) : IRequest<WorkstationDto>;

public record WorkstationDto(Guid Id, string Name, string? Location, bool IsActive);

public class CreateWorkstationHandler : IRequestHandler<CreateWorkstationCommand, WorkstationDto>
{
    private readonly AppDbContext _db;

    public CreateWorkstationHandler(AppDbContext db) => _db = db;

    public async Task<WorkstationDto> Handle(CreateWorkstationCommand request, CancellationToken cancellationToken)
    {
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
