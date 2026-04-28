using CarAssemblyErp.Domain.Enums;

namespace CarAssemblyErp.Domain.Entities;

public class InventoryTransaction
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Quantity { get; set; }
    public decimal RunningBalance { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public Part Part { get; set; } = null!;
}
