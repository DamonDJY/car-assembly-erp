using CarAssemblyErp.Data;
using CarAssemblyErp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.Parts;

public record GetBomExplosionQuery(Guid PartId) : IRequest<List<BomExplosionItem>>;

public class GetBomExplosionHandler : IRequestHandler<GetBomExplosionQuery, List<BomExplosionItem>>
{
    private readonly AppDbContext _db;

    public GetBomExplosionHandler(AppDbContext db) => _db = db;

    public async Task<List<BomExplosionItem>> Handle(GetBomExplosionQuery request, CancellationToken cancellationToken)
    {
        var bomNodes = await _db.BomNodes.AsNoTracking().ToListAsync(cancellationToken);
        var parts = await _db.Parts.AsNoTracking().ToListAsync(cancellationToken);
        var partDict = parts.ToDictionary(p => p.Id);

        var result = new List<BomExplosionItem>();
        var visited = new HashSet<(Guid, int)>();

        void Explode(Guid partId, decimal multiplier, int level, HashSet<Guid> path)
        {
            if (path.Contains(partId))
                throw new Common.BusinessException("CircularReference", "BOM contains a circular reference.");

            var children = bomNodes.Where(n => n.ParentPartId == partId).ToList();
            foreach (var child in children)
            {
                var key = (child.ChildPartId, level + 1);
                if (!visited.Add(key)) continue;

                if (!partDict.TryGetValue(child.ChildPartId, out var childPart)) continue;

                var totalQty = child.Quantity * multiplier;
                result.Add(new BomExplosionItem(child.ChildPartId, childPart.Sku, childPart.Name, totalQty, level + 1));

                var newPath = new HashSet<Guid>(path) { partId };
                Explode(child.ChildPartId, totalQty, level + 1, newPath);
            }
        }

        Explode(request.PartId, 1, 0, new HashSet<Guid>());
        return result;
    }
}
