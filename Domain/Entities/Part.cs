namespace CarAssemblyErp.Domain.Entities;

public class Part
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Specification { get; set; }
    public string Unit { get; set; } = null!;
    public decimal SafetyStock { get; set; }
    public decimal StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
}
