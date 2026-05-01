using CarAssemblyErp.Data;
using CarAssemblyErp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.Parts;

public record CreatePartCommand(string Sku, string Name, string? Specification, string Unit, int SafetyStock) : IRequest<PartDto>;

public class CreatePartHandler : IRequestHandler<CreatePartCommand, PartDto>
{
    private readonly AppDbContext _db;

    public CreatePartHandler(AppDbContext db) => _db = db;

    public async Task<PartDto> Handle(CreatePartCommand request, CancellationToken cancellationToken)
    {
        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        if (await _db.Parts.AnyAsync(p => p.Sku.ToUpper() == normalizedSku, cancellationToken))
            throw new Common.BusinessException("DuplicateSku", $"Part with SKU '{request.Sku}' already exists.");

        var part = new Part
        {
            Id = Guid.NewGuid(),
            Sku = request.Sku.Trim(),
            Name = request.Name.Trim(),
            Specification = request.Specification?.Trim(),
            Unit = request.Unit.Trim(),
            SafetyStock = request.SafetyStock,
            StockQuantity = 0,
            CreatedAt = DateTime.UtcNow
        };

        _db.Parts.Add(part);
        await _db.SaveChangesAsync(cancellationToken);

        return new PartDto(part.Id, part.Sku, part.Name, part.Specification, part.Unit, part.SafetyStock, part.StockQuantity, part.CreatedAt);
    }
}
