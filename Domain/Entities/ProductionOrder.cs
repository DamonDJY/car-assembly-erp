using CarAssemblyErp.Domain.Enums;

namespace CarAssemblyErp.Domain.Entities;

public class ProductionOrder
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public Guid TargetPartId { get; set; }
    public decimal Quantity { get; set; }
    public decimal CompletedQuantity { get; set; }
    public ProductionStatus Status { get; set; }
    public DateTime PlannedStartDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? WorkstationId { get; set; }
    public Part TargetPart { get; set; } = null!;
    public Workstation? Workstation { get; set; }
    public DateTime CreatedAt { get; set; }
}
