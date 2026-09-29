using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
    {
        public void Configure(EntityTypeBuilder<ContactMessage> builder)
        {
            builder.Property(m => m.Name).HasMaxLength(FieldLengths.Name);
            builder.Property(m => m.Email).HasMaxLength(256);
            // Message length is limited by the contact form (5,000 characters), not the column.

            builder.HasIndex(m => m.CreatedAt);

            builder.HasOne(m => m.HandledBy).WithMany()
                .HasForeignKey(m => m.HandledById).OnDelete(DeleteBehavior.SetNull);
        }
    }
}
