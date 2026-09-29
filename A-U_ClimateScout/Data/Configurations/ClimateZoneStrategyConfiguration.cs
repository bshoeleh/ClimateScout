using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class ClimateZoneStrategyConfiguration : IEntityTypeConfiguration<ClimateZoneStrategy>
    {
        public void Configure(EntityTypeBuilder<ClimateZoneStrategy> builder)
        {
            builder.HasKey(zs => new { zs.ZoneId, zs.StrategyId });

            // Deleting a zone or a strategy removes its links.
            builder.HasOne(zs => zs.Zone).WithMany(z => z.Strategies)
                .HasForeignKey(zs => zs.ZoneId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(zs => zs.Strategy).WithMany(s => s.Zones)
                .HasForeignKey(zs => zs.StrategyId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
