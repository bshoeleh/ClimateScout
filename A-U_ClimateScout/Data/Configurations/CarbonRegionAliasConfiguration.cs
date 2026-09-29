using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class CarbonRegionAliasConfiguration : IEntityTypeConfiguration<CarbonRegionAlias>
    {
        public void Configure(EntityTypeBuilder<CarbonRegionAlias> builder)
        {
            builder.Property(a => a.Alias).HasMaxLength(FieldLengths.Name);
            builder.Property(a => a.Source).HasMaxLength(50);

            // A CSV name must lead to exactly one region.
            builder.HasIndex(a => a.Alias).IsUnique();

            builder.HasOne(a => a.Region).WithMany(r => r.Aliases)
                .HasForeignKey(a => a.RegionId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
