using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class EquivalencyFactorConfiguration : IEntityTypeConfiguration<EquivalencyFactor>
    {
        public void Configure(EntityTypeBuilder<EquivalencyFactor> builder)
        {
            builder.HasKey(f => f.Key);

            builder.ToTable(t => t.HasCheckConstraint("CK_EquivalencyFactors_Value", "[TonsCo2ePerUnit] > 0"));

            builder.Property(f => f.Key).HasMaxLength(50);
            builder.Property(f => f.Label).HasMaxLength(FieldLengths.Name);
            builder.Property(f => f.TonsCo2ePerUnit).HasPrecision(12, 8);
            builder.Property(f => f.SourceUrl).HasMaxLength(FieldLengths.Url);
        }
    }
}
