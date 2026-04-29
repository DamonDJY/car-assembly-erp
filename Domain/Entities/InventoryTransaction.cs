using CarAssemblyErp.Domain.Enums;

namespace CarAssemblyErp.Domain.Entities;

public class InventoryTransaction
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public TransactionType TransactionType { get; set; }
    public int Quantity { get; set; }
    public int RunningBalance { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public Guid? WorkstationId { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public Part Part { get; set; } = null!;
}
