using CarAssemblyErp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarAssemblyErp.Data.Configurations;

public class ProductionOrderConfiguration : IEntityTypeConfiguration<ProductionOrder>
{
    public void Configure(EntityTypeBuilder<ProductionOrder> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Quantity);
        builder.Property(x => x.CompletedQuantity);
        builder.HasOne(x => x.TargetPart).WithMany().HasForeignKey(x => x.TargetPartId);
        builder.HasOne(x => x.Workstation).WithMany().HasForeignKey(x => x.WorkstationId);
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint("CK_ProductionOrder_Quantity", "\"Quantity\" > 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_ProductionOrder_CompletedQuantity", "\"CompletedQuantity\" >= 0 AND \"CompletedQuantity\" <= \"Quantity\""));
    }
}
