using A_U_ClimateScout.Identity;
using A_U_ClimateScout.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Data
{
    // EF Core database context. Includes the ASP.NET Core Identity tables (users, roles, claims, logins, tokens)
    // keyed on ApplicationUser, plus our own tables. Column rules live in Data/Configurations.
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        // Media
        public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

        // Climate zones and design strategies
        public DbSet<ClimateZoneGroup> ClimateZoneGroups => Set<ClimateZoneGroup>();
        public DbSet<ClimateZone> ClimateZones => Set<ClimateZone>();
        public DbSet<Diagram> Diagrams => Set<Diagram>();
        public DbSet<DesignStrategy> DesignStrategies => Set<DesignStrategy>();
        public DbSet<ClimateZoneStrategy> ClimateZoneStrategies => Set<ClimateZoneStrategy>();
        public DbSet<StrategyConflict> StrategyConflicts => Set<StrategyConflict>();
        public DbSet<ReferenceProject> ReferenceProjects => Set<ReferenceProject>();

        // Grid carbon intensity
        public DbSet<CarbonDataSource> CarbonDataSources => Set<CarbonDataSource>();
        public DbSet<CarbonRegion> CarbonRegions => Set<CarbonRegion>();
        public DbSet<CarbonRegionAlias> CarbonRegionAliases => Set<CarbonRegionAlias>();
        public DbSet<CarbonImportBatch> CarbonImportBatches => Set<CarbonImportBatch>();
        public DbSet<CarbonIntensity> CarbonIntensities => Set<CarbonIntensity>();

        // Site content
        public DbSet<Sponsor> Sponsors => Set<Sponsor>();
        public DbSet<ContentBlock> ContentBlocks => Set<ContentBlock>();
        public DbSet<EquivalencyFactor> EquivalencyFactors => Set<EquivalencyFactor>();
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

        // Audit
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Identity's own tables first, then every IEntityTypeConfiguration<T> in this project.
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
