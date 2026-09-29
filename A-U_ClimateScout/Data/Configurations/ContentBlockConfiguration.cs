using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class ContentBlockConfiguration : IEntityTypeConfiguration<ContentBlock>
    {
        public void Configure(EntityTypeBuilder<ContentBlock> builder)
        {
            builder.HasKey(c => c.Key);

            builder.Property(c => c.Key).HasMaxLength(FieldLengths.Slug);
            builder.Property(c => c.Title).HasMaxLength(FieldLengths.Name);

            builder.HasOne(c => c.UpdatedBy).WithMany()
                .HasForeignKey(c => c.UpdatedById).OnDelete(DeleteBehavior.SetNull);
        }
    }
}
