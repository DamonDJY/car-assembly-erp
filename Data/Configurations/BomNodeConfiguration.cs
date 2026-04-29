using CarAssemblyErp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarAssemblyErp.Data.Configurations;

public class BomNodeConfiguration : IEntityTypeConfiguration<BomNode>
{
    public void Configure(EntityTypeBuilder<BomNode> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity);
        builder.HasOne(x => x.ParentPart).WithMany().HasForeignKey(x => x.ParentPartId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ChildPart).WithMany().HasForeignKey(x => x.ChildPartId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ParentPartId, x.ChildPartId }).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint("CK_BomNode_Quantity", "\"Quantity\" > 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_BomNode_NoSelfRef", "\"ParentPartId\" <> \"ChildPartId\""));
    }
}
