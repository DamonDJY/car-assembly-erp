using CarAssemblyErp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarAssemblyErp.Data.Configurations;

public class BomNodeConfiguration : IEntityTypeConfiguration<BomNode>
{
    public void Configure(EntityTypeBuilder<BomNode> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.HasOne(x => x.ParentPart).WithMany().HasForeignKey(x => x.ParentPartId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ChildPart).WithMany().HasForeignKey(x => x.ChildPartId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.ParentPartId, x.ChildPartId }).IsUnique();
    }
}
