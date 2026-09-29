using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.Property(a => a.UserId).HasMaxLength(450);         // same length as AspNetUsers.Id
            builder.Property(a => a.UserName).HasMaxLength(256);
            builder.Property(a => a.Action).HasMaxLength(50);
            builder.Property(a => a.EntityType).HasMaxLength(100);
            builder.Property(a => a.EntityId).HasMaxLength(100);
            builder.Property(a => a.Summary).HasMaxLength(FieldLengths.Summary);

            // No foreign keys (see AuditLog). Indexes for the audit log screen: newest first, and per record.
            builder.HasIndex(a => a.OccurredAt);
            builder.HasIndex(a => new { a.EntityType, a.EntityId });
        }
    }
}
