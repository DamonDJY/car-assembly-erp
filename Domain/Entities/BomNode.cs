namespace CarAssemblyErp.Domain.Entities;

public class BomNode
{
    public Guid Id { get; set; }
    public Guid ParentPartId { get; set; }
    public Guid ChildPartId { get; set; }
    public decimal Quantity { get; set; }
    public Part ParentPart { get; set; } = null!;
    public Part ChildPart { get; set; } = null!;
}
