using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using A_U_ClimateScout.Controllers;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Identity;
using A_U_ClimateScout.Options;
using A_U_ClimateScout.Services;
using A_U_ClimateScout.Services.CarbonImport;
using A_U_ClimateScout.Tools;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Production settings come from environment variables prefixed with CS__ (ClimateScout),
// e.g. CS__ConnectionStrings__DefaultConnection. The prefix is removed and "__" becomes ":".
// Added last so these override appsettings.json and User Secrets.
builder.Configuration.AddEnvironmentVariables(prefix: "CS__");

// Logging: Serilog replaces the built-in loggers. What gets logged and where
// (console + daily rolling files) is configured in the "Serilog" section of appsettings.json.
builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        // Generate SQL that SQL Server 2019 (compatibility level 150) understands. Raise this once the
        // production server's version is confirmed (plan §10).
        sql.UseCompatibilityLevel(150)));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddUserManager<AppUserManager>()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>()   // after AddRoles, which registers its own
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Who may use the Admin area. Views and controllers check the policy, never role names,
// so Entra ID sign-in can be added later without touching them (plan §9).
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.AdminArea, policy => policy.RequireRole(Roles.Admin, Roles.Editor));

// JSON (API and the data embedded in pages) writes enums as their names, e.g. "KwhPerSquareMetre", not 1.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddMemoryCache();

// Map tiles are fetched by our server (MapController), never directly by the browser (plan §5).
builder.Services.Configure<MapsOptions>(builder.Configuration.GetSection(MapsOptions.SectionName));
builder.Services.AddHttpClient(MapsOptions.HttpClientName, (services, client) =>
{
    var maps = services.GetRequiredService<IOptions<MapsOptions>>().Value;
    client.DefaultRequestHeaders.UserAgent.ParseAdd($"ClimateScout/1.0 ({maps.ContactEmail})");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddSingleton<Geocoder>();
builder.Services.AddScoped<CarbonImporter>();
builder.Services.AddScoped<CarbonValues>();
builder.Services.AddScoped<Sponsors>();

// Contact form: at most 5 messages per visitor (IP address) per 15 minutes; more get the "too many requests" page.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(ContactController.RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(15) }));
});

var app = builder.Build();

// Data commands (plan §3.3): "tool …" runs one command against the same configuration and database
// as the site, then exits without starting the web server.
if (args is ["tool", ..])
{
    return await ToolRunner.RunAsync(app.Services, args[1..]);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/error/500");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Error responses without a body (404, 403 …) re-run the pipeline at /error/{code}
// so visitors see a styled page; the original status code is kept.
app.UseStatusCodePagesWithReExecute("/error/{0}");

app.UseHttpsRedirection();

// One summary log line per request: method, path, status code, duration.
app.UseSerilogRequestLogging();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthorization();

// Users with a one-time password (tool init admin) must choose their own before using the site.
app.UseMustChangePassword();

app.MapStaticAssets();

// Areas first, so /admin reaches the Admin area's Dashboard controller.
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

await app.RunAsync();
return 0;
