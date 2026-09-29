using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace A_U_ClimateScout.Data.Configurations
{
    public class StrategyConflictConfiguration : IEntityTypeConfiguration<StrategyConflict>
    {
        public void Configure(EntityTypeBuilder<StrategyConflict> builder)
        {
            builder.HasKey(c => new { c.StrategyId, c.ConflictsWithStrategyId });

            builder.ToTable(t => t.HasCheckConstraint("CK_StrategyConflicts_NotSelf",
                "[StrategyId] <> [ConflictsWithStrategyId]"));

            // Deleting a strategy removes the rows where it is StrategyId. SQL Server doesn't allow a
            // second cascade path into this table, so the reverse rows (where it is ConflictsWithStrategyId)
            // are removed by StrategyConflictService before the strategy is deleted.
            builder.HasOne(c => c.Strategy).WithMany(s => s.Conflicts)
                .HasForeignKey(c => c.StrategyId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(c => c.ConflictsWith).WithMany()
                .HasForeignKey(c => c.ConflictsWithStrategyId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
