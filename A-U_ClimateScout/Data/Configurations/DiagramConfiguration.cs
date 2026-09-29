using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class DiagramConfiguration : IEntityTypeConfiguration<Diagram>
    {
        public void Configure(EntityTypeBuilder<Diagram> builder)
        {
            builder.Property(d => d.Name).HasMaxLength(FieldLengths.Name);
            builder.Property(d => d.Slug).HasMaxLength(FieldLengths.Slug);

            builder.HasIndex(d => d.Slug).IsUnique();

            builder.HasOne(d => d.SvgAsset).WithMany()
                .HasForeignKey(d => d.SvgAssetId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
