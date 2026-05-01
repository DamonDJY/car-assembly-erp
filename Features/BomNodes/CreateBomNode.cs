using CarAssemblyErp.Data;
using CarAssemblyErp.Domain.Entities;
using CarAssemblyErp.Infrastructure.Redis;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Features.BomNodes;

public record CreateBomNodeCommand(Guid ParentPartId, Guid ChildPartId, int Quantity) : IRequest<BomNodeDto>;

public record BomNodeDto(Guid Id, Guid ParentPartId, Guid ChildPartId, int Quantity);

public class CreateBomNodeHandler : IRequestHandler<CreateBomNodeCommand, BomNodeDto>
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateBomNodeHandler(AppDbContext db, ICacheService cache, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BomNodeDto> Handle(CreateBomNodeCommand request, CancellationToken cancellationToken)
    {
        _httpContextAccessor.HttpContext?.Items.Add("DB", "Primary");
        _httpContextAccessor.HttpContext?.Items.Add("Cache", "Miss");

        if (request.ParentPartId == request.ChildPartId)
            throw new Common.BusinessException("SelfReference", "Parent and child part cannot be the same.");

        var existing = await _db.BomNodes.FirstOrDefaultAsync(
            b => b.ParentPartId == request.ParentPartId && b.ChildPartId == request.ChildPartId, cancellationToken);
        if (existing != null)
            throw new Common.BusinessException("DuplicateBomNode", "BOM node already exists for this parent-child combination.");

        var allNodes = await _db.BomNodes.AsNoTracking().ToListAsync(cancellationToken);
        if (WouldCreateCycle(allNodes, request.ParentPartId, request.ChildPartId))
            throw new Common.BusinessException("CircularReference", "This BOM node would create a circular reference.");

        var node = new BomNode
        {
            Id = Guid.NewGuid(),
            ParentPartId = request.ParentPartId,
            ChildPartId = request.ChildPartId,
            Quantity = request.Quantity
        };

        _db.BomNodes.Add(node);
        await _db.SaveChangesAsync(cancellationToken);

        // 缓存失效：清除该 Part 及其所有祖先的 BOM 缓存
        var ancestors = FindAllAncestors(allNodes, request.ParentPartId);
        ancestors.Add(request.ParentPartId);

        foreach (var partId in ancestors)
        {
            await _cache.RemoveAsync($"bom:{partId}:v1", cancellationToken);
            await _cache.RemoveAsync($"bom:{partId}:v1:empty", cancellationToken);
        }

        // 清除安全库存缓存
        await _cache.RemoveAsync("parts:low-stock", cancellationToken);

        return new BomNodeDto(node.Id, node.ParentPartId, node.ChildPartId, node.Quantity);
    }

    private static List<Guid> FindAllAncestors(List<BomNode> allNodes, Guid partId)
    {
        var ancestors = new List<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(partId);
        var visited = new HashSet<Guid>();
        visited.Add(partId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var parents = allNodes.Where(n => n.ChildPartId == current).Select(n => n.ParentPartId).ToList();
            foreach (var parent in parents)
            {
                if (visited.Add(parent))
                {
                    ancestors.Add(parent);
                    queue.Enqueue(parent);
                }
            }
        }
        return ancestors;
    }

    private static bool WouldCreateCycle(List<BomNode> nodes, Guid parentId, Guid childId)
    {
        var adj = nodes.GroupBy(n => n.ParentPartId)
            .ToDictionary(g => g.Key, g => g.Select(n => n.ChildPartId).ToList());
        if (!adj.ContainsKey(parentId)) adj[parentId] = new List<Guid>();
        adj[parentId].Add(childId);

        var visited = new HashSet<Guid>();
        var recStack = new HashSet<Guid>();

        bool Dfs(Guid node)
        {
            visited.Add(node);
            recStack.Add(node);
            if (adj.TryGetValue(node, out var children))
            {
                foreach (var c in children)
                {
                    if (!visited.Contains(c) && Dfs(c)) return true;
                    if (recStack.Contains(c)) return true;
                }
            }
            recStack.Remove(node);
            return false;
        }

        foreach (var start in adj.Keys)
            if (!visited.Contains(start) && Dfs(start))
                return true;
        return false;
    }
}
