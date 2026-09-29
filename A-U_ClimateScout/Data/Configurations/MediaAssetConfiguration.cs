using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
    {
        public void Configure(EntityTypeBuilder<MediaAsset> builder)
        {
            builder.Property(m => m.FileName).HasMaxLength(FieldLengths.FileName);
            builder.Property(m => m.ContentType).HasMaxLength(FieldLengths.ContentType);
            builder.Property(m => m.StoragePath).HasMaxLength(FieldLengths.StoragePath);
            builder.Property(m => m.AltText).HasMaxLength(FieldLengths.AltText);

            builder.HasIndex(m => m.StoragePath).IsUnique();
        }
    }
}
