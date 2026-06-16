using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<DesignStrategy> DesignStrategies { get; set; }
        public DbSet<ClimateScoutClimateZone> ClimateScoutClimateZones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed DesignStrategies
            var designStrategies = new List<DesignStrategy>
            {
                // ... (as above)
            };
            modelBuilder.Entity<DesignStrategy>().HasData(designStrategies);

            // Add similar seeding for ClimateScoutClimateZone if needed
        }
    }
}