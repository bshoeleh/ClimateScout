using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class DesignStrategyConfiguration : IEntityTypeConfiguration<DesignStrategy>
    {
        public void Configure(EntityTypeBuilder<DesignStrategy> builder)
        {
            builder.Property(s => s.Name).HasMaxLength(FieldLengths.Name);
            builder.Property(s => s.Slug).HasMaxLength(FieldLengths.Slug);
            builder.Property(s => s.Summary).HasMaxLength(FieldLengths.Summary);
            builder.Property(s => s.Palette2030Url).HasMaxLength(FieldLengths.Url);

            builder.HasIndex(s => s.Slug).IsUnique();

            // Media that a strategy still uses can't be deleted.
            builder.HasOne(s => s.ImageAsset).WithMany()
                .HasForeignKey(s => s.ImageAssetId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(s => s.DiagramImageAsset).WithMany()
                .HasForeignKey(s => s.DiagramImageAssetId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
