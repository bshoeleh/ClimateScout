using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class ClimateZoneGroupConfiguration : IEntityTypeConfiguration<ClimateZoneGroup>
    {
        public void Configure(EntityTypeBuilder<ClimateZoneGroup> builder)
        {
            builder.ToTable(t => t.HasCheckConstraint("CK_ClimateZoneGroups_Color",
                string.Format(FieldLengths.ColorCheckSql, nameof(ClimateZoneGroup.Color))));

            builder.Property(g => g.Code).HasMaxLength(1).IsFixedLength();
            builder.Property(g => g.Name).HasMaxLength(FieldLengths.Name);
            builder.Property(g => g.Slug).HasMaxLength(FieldLengths.Slug);
            builder.Property(g => g.Color).HasMaxLength(FieldLengths.Color).IsFixedLength();

            builder.HasIndex(g => g.Code).IsUnique();
            builder.HasIndex(g => g.Slug).IsUnique();
        }
    }
}
