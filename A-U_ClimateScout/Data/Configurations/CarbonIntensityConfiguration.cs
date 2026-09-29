using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class CarbonIntensityConfiguration : IEntityTypeConfiguration<CarbonIntensity>
    {
        public void Configure(EntityTypeBuilder<CarbonIntensity> builder)
        {
            builder.ToTable(t =>
            {
                t.HasCheckConstraint("CK_CarbonIntensities_Value", "[ValueGPerKWh] >= 0");
                t.HasCheckConstraint("CK_CarbonIntensities_Year", "[Year] BETWEEN 1990 AND 2100");
            });

            builder.Property(i => i.ValueGPerKWh).HasPrecision(10, 3);

            // Only one current value per region, source and year; superseded rows are history.
            builder.HasIndex(i => new { i.RegionId, i.SourceId, i.Year })
                .IsUnique()
                .HasFilter("[SupersededByBatchId] IS NULL");

            // Values go away through a batch rollback, never by deleting a region, source or batch.
            builder.HasOne(i => i.Region).WithMany(r => r.Intensities)
                .HasForeignKey(i => i.RegionId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(i => i.Source).WithMany()
                .HasForeignKey(i => i.SourceId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(i => i.ImportBatch).WithMany()
                .HasForeignKey(i => i.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(i => i.SupersededByBatch).WithMany()
                .HasForeignKey(i => i.SupersededByBatchId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
