using CarAssemblyErp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarAssemblyErp.Data.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.RunningBalance).HasPrecision(18, 4);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(100);
        builder.HasOne(x => x.Part).WithMany().HasForeignKey(x => x.PartId);
    }
}
