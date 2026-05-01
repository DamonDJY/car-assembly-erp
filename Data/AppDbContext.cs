using CarAssemblyErp.Domain.Entities;
using CarAssemblyErp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarAssemblyErp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Part> Parts => Set<Part>();
    public DbSet<BomNode> BomNodes => Set<BomNode>();
    public DbSet<Workstation> Workstations => Set<Workstation>();
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var chassisId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tireId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var engineId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var pistonId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var screwId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var carId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var wsId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        modelBuilder.Entity<Workstation>().HasData(new Workstation
        {
            Id = wsId,
            Name = "总装线A",
            Location = "车间1",
            IsActive = true
        });

        modelBuilder.Entity<Part>().HasData(
            new Part { Id = chassisId, Sku = "CHASSIS-001", Name = "底盘", Specification = "标准底盘", Unit = "个", SafetyStock = 5, StockQuantity = 10, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Part { Id = tireId, Sku = "TIRE-001", Name = "轮胎", Specification = "标准轮胎", Unit = "个", SafetyStock = 20, StockQuantity = 100, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Part { Id = engineId, Sku = "ENGINE-001", Name = "引擎", Specification = "V6引擎", Unit = "个", SafetyStock = 2, StockQuantity = 5, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Part { Id = pistonId, Sku = "PISTON-001", Name = "活塞", Specification = "标准活塞", Unit = "个", SafetyStock = 10, StockQuantity = 20, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Part { Id = screwId, Sku = "SCREW-001", Name = "螺丝", Specification = "M8螺丝", Unit = "个", SafetyStock = 100, StockQuantity = 500, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Part { Id = carId, Sku = "CAR-001", Name = "整车", Specification = "标准轿车", Unit = "辆", SafetyStock = 1, StockQuantity = 0, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<BomNode>().HasData(
            new BomNode { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), ParentPartId = carId, ChildPartId = chassisId, Quantity = 1 },
            new BomNode { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), ParentPartId = carId, ChildPartId = tireId, Quantity = 4 },
            new BomNode { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), ParentPartId = carId, ChildPartId = engineId, Quantity = 1 },
            new BomNode { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), ParentPartId = engineId, ChildPartId = pistonId, Quantity = 4 },
            new BomNode { Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), ParentPartId = engineId, ChildPartId = screwId, Quantity = 12 }
        );
    }
}
