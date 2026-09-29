using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class ClimateZoneConfiguration : IEntityTypeConfiguration<ClimateZone>
    {
        public void Configure(EntityTypeBuilder<ClimateZone> builder)
        {
            builder.ToTable(t => t.HasCheckConstraint("CK_ClimateZones_Color",
                string.Format(FieldLengths.ColorCheckSql, nameof(ClimateZone.Color))));

            builder.Property(z => z.KoppenCode).HasMaxLength(3);
            builder.Property(z => z.Name).HasMaxLength(FieldLengths.Name);
            builder.Property(z => z.Slug).HasMaxLength(FieldLengths.Slug);
            builder.Property(z => z.Color).HasMaxLength(FieldLengths.Color).IsFixedLength();

            builder.HasIndex(z => z.KoppenCode).IsUnique();
            builder.HasIndex(z => z.Slug).IsUnique();
            builder.HasIndex(z => z.MapId).IsUnique().HasFilter("[MapId] IS NOT NULL");

            // A group or diagram that still has zones can't be deleted.
            builder.HasOne(z => z.Group).WithMany(g => g.Zones)
                .HasForeignKey(z => z.GroupId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(z => z.Diagram).WithMany()
                .HasForeignKey(z => z.DiagramId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
