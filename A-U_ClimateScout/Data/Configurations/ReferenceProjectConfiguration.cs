using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class ReferenceProjectConfiguration : IEntityTypeConfiguration<ReferenceProject>
    {
        public void Configure(EntityTypeBuilder<ReferenceProject> builder)
        {
            builder.Property(p => p.Name).HasMaxLength(FieldLengths.Name);
            builder.Property(p => p.Location).HasMaxLength(FieldLengths.Name);
            builder.Property(p => p.Sector).HasMaxLength(FieldLengths.Name);
            builder.Property(p => p.Url).HasMaxLength(FieldLengths.Url);

            builder.HasOne(p => p.Strategy).WithMany(s => s.ReferenceProjects)
                .HasForeignKey(p => p.StrategyId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(p => p.ImageAsset).WithMany()
                .HasForeignKey(p => p.ImageAssetId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
