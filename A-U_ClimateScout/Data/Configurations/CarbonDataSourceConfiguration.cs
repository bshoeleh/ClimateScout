using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class CarbonDataSourceConfiguration : IEntityTypeConfiguration<CarbonDataSource>
    {
        public void Configure(EntityTypeBuilder<CarbonDataSource> builder)
        {
            builder.Property(s => s.Name).HasMaxLength(FieldLengths.Name);
            builder.Property(s => s.Url).HasMaxLength(FieldLengths.Url);
            builder.Property(s => s.Notes).HasMaxLength(FieldLengths.Summary);

            builder.HasIndex(s => s.Name).IsUnique();
        }
    }
}
