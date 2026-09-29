using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class CarbonRegionConfiguration : IEntityTypeConfiguration<CarbonRegion>
    {
        public void Configure(EntityTypeBuilder<CarbonRegion> builder)
        {
            builder.Property(r => r.Code).HasMaxLength(FieldLengths.Code);
            builder.Property(r => r.Name).HasMaxLength(FieldLengths.Name);
            builder.Property(r => r.RegionType).HasConversion<string>().HasMaxLength(FieldLengths.EnumText);
            builder.Property(r => r.Continent).HasMaxLength(50);

            builder.HasIndex(r => r.Code).IsUnique();

            // A parent region or a source still in use can't be deleted.
            builder.HasOne(r => r.ParentRegion).WithMany()
                .HasForeignKey(r => r.ParentRegionId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(r => r.PreferredSource).WithMany()
                .HasForeignKey(r => r.PreferredSourceId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
