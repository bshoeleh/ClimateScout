using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class SponsorConfiguration : IEntityTypeConfiguration<Sponsor>
    {
        public void Configure(EntityTypeBuilder<Sponsor> builder)
        {
            builder.ToTable(t => t.HasCheckConstraint("CK_Sponsors_Dates",
                "[StartDate] IS NULL OR [EndDate] IS NULL OR [EndDate] >= [StartDate]"));

            builder.Property(s => s.Name).HasMaxLength(FieldLengths.Name);
            builder.Property(s => s.Url).HasMaxLength(FieldLengths.Url);

            // A logo a sponsor still uses can't be deleted.
            builder.HasOne(s => s.LogoAsset).WithMany()
                .HasForeignKey(s => s.LogoAssetId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
