namespace CarAssemblyErp.Features.Parts;

public record PartDto(Guid Id, string Sku, string Name, string? Specification, string Unit, int SafetyStock, int StockQuantity, DateTime CreatedAt);

public record BomExplosionItem(Guid PartId, string Sku, string Name, int TotalQuantity, int Level);

public record LowStockItem(Guid Id, string Sku, string Name, int StockQuantity, int SafetyStock, int Shortage);
