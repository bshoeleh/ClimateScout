using System.Xml.Linq;
using A_U_ClimateScout.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace A_U_ClimateScout.Controllers
{
    // robots.txt and sitemap.xml for search engines (plan Phase 4, SEO). Both are built from the request's own address,
    // so they are right on localhost and on climatescout.arcadis.com.
    public class SeoController(ApplicationDbContext db, IMemoryCache cache) : Controller
    {
        // Public pages that are not a zone or a strategy.
        private static readonly string[] FixedPaths =
            ["/", "/design-strategy", "/carbon", "/carbon-comparison", "/sponsors", "/about", "/contact"];

        // Cleared by Admin when a zone or strategy is shown or hidden.
        public const string SitemapCacheKey = "sitemap:paths";

        private string Origin => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        [HttpGet("robots.txt")]
        public ContentResult Robots() => Content(
            $"""
            User-agent: *
            Disallow: /admin
            Disallow: /Identity
            Disallow: /api/
            Disallow: /map/tiles
            Sitemap: {Origin}/sitemap.xml

            """,
            "text/plain");

        [HttpGet("sitemap.xml")]
        public async Task<ContentResult> Sitemap(CancellationToken cancellationToken)
        {
            // The list of paths is cached an hour; the address in front of them comes from each request.
            var paths = await cache.GetOrCreateAsync(SitemapCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                var zones = await db.ClimateZones.AsNoTracking().Where(z => z.IsActive)
                    .OrderBy(z => z.Slug).Select(z => "/zone/" + z.Slug).ToListAsync(cancellationToken);
                var strategies = await db.DesignStrategies.AsNoTracking().Where(s => s.IsActive)
                    .OrderBy(s => s.Slug).Select(s => "/design-strategy/" + s.Slug).ToListAsync(cancellationToken);
                return FixedPaths.Concat(zones).Concat(strategies).ToList();
            });

            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var document = new XDocument(new XDeclaration("1.0", "utf-8", null),
                new XElement(ns + "urlset",
                    paths!.Select(path => new XElement(ns + "url", new XElement(ns + "loc", Origin + path)))));
            return Content(document.Declaration + Environment.NewLine + document, "application/xml");
        }
    }
}
