using A_U_ClimateScout.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Data
{
    // EF Core database context. Includes the ASP.NET Core Identity tables (users, roles, claims, logins, tokens)
    // keyed on ApplicationUser. Our own tables (DbSet<...>) are added in Phase 2 when we design the models.
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
    }
}
