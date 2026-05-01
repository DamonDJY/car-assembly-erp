using CarAssemblyErp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarAssemblyErp.Data.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity);
        builder.Property(x => x.RunningBalance);
        builder.Property(x => x.ReferenceType).HasMaxLength(50);
        builder.Property(x => x.Remark).HasMaxLength(500);
        builder.HasOne(x => x.Part).WithMany().HasForeignKey(x => x.PartId);
        builder.ToTable(t => t.HasCheckConstraint("CK_InventoryTransaction_Quantity", "\"Quantity\" <> 0"));
    }
}
