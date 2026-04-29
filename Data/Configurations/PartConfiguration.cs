using CarAssemblyErp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarAssemblyErp.Data.Configurations;

public class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Sku).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(20).IsRequired();
        builder.Property(x => x.StockQuantity);
        builder.Property(x => x.SafetyStock);
        builder.HasIndex(x => x.Sku)
            .IsUnique()
            .HasDatabaseName("IX_Part_Sku")
            .HasAnnotation("Npgsql:IndexExpression", "LOWER(\"Sku\")");
        builder.ToTable(t => t.HasCheckConstraint("CK_Part_StockQuantity", "\"StockQuantity\" >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_Part_SafetyStock", "\"SafetyStock\" >= 0"));
    }
}
