namespace CarAssemblyErp.Features.Parts;

public record PartDto(Guid Id, string Sku, string Name, string? Specification, string Unit, decimal SafetyStock, decimal StockQuantity, DateTime CreatedAt);

public record BomExplosionItem(Guid PartId, string Sku, string Name, decimal TotalQuantity, int Level);

public record LowStockItem(Guid Id, string Sku, string Name, decimal StockQuantity, decimal SafetyStock, decimal Shortage);
