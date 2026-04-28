using CarAssemblyErp.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.Parts;

public record GetPartByIdQuery(Guid Id) : IRequest<PartDto?>;

public class GetPartByIdHandler : IRequestHandler<GetPartByIdQuery, PartDto?>
{
    private readonly AppDbContext _db;

    public GetPartByIdHandler(AppDbContext db) => _db = db;

    public async Task<PartDto?> Handle(GetPartByIdQuery request, CancellationToken cancellationToken)
    {
        var part = await _db.Parts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (part == null) return null;
        return new PartDto(part.Id, part.Sku, part.Name, part.Specification, part.Unit, part.SafetyStock, part.StockQuantity, part.CreatedAt);
    }
}
