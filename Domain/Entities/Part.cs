namespace CarAssemblyErp.Domain.Entities;

public class Part
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Specification { get; set; }
    public string Unit { get; set; } = null!;
    public int SafetyStock { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
}
