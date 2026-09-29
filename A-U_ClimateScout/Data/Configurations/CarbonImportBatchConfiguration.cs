using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class CarbonImportBatchConfiguration : IEntityTypeConfiguration<CarbonImportBatch>
    {
        public void Configure(EntityTypeBuilder<CarbonImportBatch> builder)
        {
            builder.Property(b => b.FileName).HasMaxLength(FieldLengths.FileName);
            builder.Property(b => b.Profile).HasMaxLength(50);
            builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(FieldLengths.EnumText);

            builder.HasOne(b => b.Source).WithMany()
                .HasForeignKey(b => b.SourceId).OnDelete(DeleteBehavior.Restrict);

            // Deleting a user keeps their import history; the batch just loses the link.
            builder.HasOne(b => b.UploadedBy).WithMany()
                .HasForeignKey(b => b.UploadedById).OnDelete(DeleteBehavior.SetNull);
        }
    }
}
