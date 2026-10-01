using System.CommandLine;
using System.Globalization;
using System.Text.RegularExpressions;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Models;
using A_U_ClimateScout.Services;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace A_U_ClimateScout.Tools
{
    // "tool import …": bring data in from outside sources (plan §3.3).
    public static class ImportCommands
    {
        public static Command Create(IServiceProvider services)
        {
            return new Command("import", "Import data from outside sources.")
            {
                CreateWordPress(services),
                CreateGeo(services),
            };
        }

        // tool import geo [--dry-run]
        // Carbon regions (countries, US states, Canadian provinces, aggregates) and the other names the carbon CSVs
        // use for them, from Data/Import/carbon-regions.csv and carbon-region-aliases.csv (plan Phase 3).
        // The map outlines are in wwwroot/geo/carbon-regions.geojson, keyed by the same codes. Existing rows are skipped.
        private static Command CreateGeo(IServiceProvider services)
        {
            var dryRun = new Option<bool>("--dry-run") { Description = "Report what would be created without saving." };
            var command = new Command("geo", "Create carbon regions and their aliases.") { dryRun };
            command.SetAction(async (parseResult, cancellationToken) =>
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
                var isDryRun = parseResult.GetValue(dryRun);

                // Canadian values come from the Canada Energy Regulator, everything else from Ember.
                var sources = await db.CarbonDataSources.ToDictionaryAsync(s => s.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);
                if (!sources.TryGetValue("Ember", out var ember) || !sources.TryGetValue("Canada Energy Regulator", out var canada))
                {
                    logger.LogError("The carbon data sources don't exist yet. Run \"tool init reference\" first.");
                    return 1;
                }

                // Columns: Code, Name, Type, Parent, Continent, ShowOnMap, DataSources. Countries come first,
                // so a state's or province's parent (US, CA) is always created before it.
                var regions = await db.CarbonRegions.ToDictionaryAsync(r => r.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
                var regionRows = ReadCsv(environment.ContentRootPath, "carbon-regions.csv");
                var regionsCreated = 0;
                foreach (var fields in regionRows)
                {
                    if (regions.ContainsKey(fields[0]))
                    {
                        continue;
                    }

                    var type = Enum.Parse<CarbonRegionType>(fields[2]);
                    var region = new CarbonRegion
                    {
                        Code = fields[0],
                        Name = fields[1],
                        RegionType = type,
                        ParentRegion = fields[3] == "" ? null : regions[fields[3]],
                        Continent = EmptyToNull(fields[4]),
                        ShowOnMap = fields[5] == "yes",
                        PreferredSource = type == CarbonRegionType.Province ? canada : ember,
                    };
                    if (!isDryRun)
                    {
                        db.CarbonRegions.Add(region);
                    }
                    regions[region.Code] = region;
                    regionsCreated++;
                }

                // Columns: Code, Alias, Source.
                var existingAliases = (await db.CarbonRegionAliases.Select(a => a.Alias).ToListAsync(cancellationToken))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var aliasRows = ReadCsv(environment.ContentRootPath, "carbon-region-aliases.csv");
                var aliasesCreated = 0;
                foreach (var fields in aliasRows.Where(f => !existingAliases.Contains(f[1])))
                {
                    if (!isDryRun)
                    {
                        db.CarbonRegionAliases.Add(new CarbonRegionAlias { Region = regions[fields[0]], Alias = fields[1], Source = fields[2] });
                    }
                    aliasesCreated++;
                }

                if (!isDryRun)
                {
                    await db.SaveChangesAsync(cancellationToken);
                }

                var verb = isDryRun ? "would create" : "created";
                logger.LogInformation("Carbon regions: {Verb} {Created}, skipped {Skipped} (already present).",
                    verb, regionsCreated, regionRows.Count - regionsCreated);
                logger.LogInformation("Region aliases: {Verb} {Created}, skipped {Skipped} (already present).",
                    verb, aliasesCreated, aliasRows.Count - aliasesCreated);
                return 0;
            });
            return command;
        }

        // tool import wordpress --sqlite <path> [--dry-run]
        // Imports the old WordPress site (plan Phase 3), one part per method, media first. Each part adds only
        // what is missing, so it can be run on each database (development now, production at launch).
        // Everything is saved at the end, so a failure part-way saves nothing.
        private static Command CreateWordPress(IServiceProvider services)
        {
            var sqlite = new Option<FileInfo>("--sqlite") { Description = "The old site's database (climatescout-*.sqlite).", Required = true };
            var dryRun = new Option<bool>("--dry-run") { Description = "Report what would be created without saving." };
            var command = new Command("wordpress", "Import media, zones, strategies and content from the old WordPress site.") { sqlite, dryRun };
            command.SetAction(async (parseResult, cancellationToken) =>
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
                var isDryRun = parseResult.GetValue(dryRun);

                var file = parseResult.GetValue(sqlite)!;
                if (!file.Exists)
                {
                    logger.LogError("Database file not found: {Path}", file.FullName);
                    return 1;
                }

                await using var wordPress = new SqliteConnection($"Data Source={file.FullName};Mode=ReadOnly");
                await wordPress.OpenAsync(cancellationToken);

                var mediaByWordPressId = await ImportMediaAsync(db, environment, logger, isDryRun, cancellationToken);
                var zonesByCode = await ImportZonesAsync(db, wordPress, logger, isDryRun, cancellationToken);
                await ImportStrategiesAsync(db, wordPress, logger, zonesByCode, mediaByWordPressId, isDryRun, cancellationToken);
                await ImportContentAsync(db, wordPress, logger, isDryRun, cancellationToken);
                await ImportDiagramsAsync(db, environment, logger, isDryRun, cancellationToken);

                if (!isDryRun)
                {
                    await db.SaveChangesAsync(cancellationToken);
                }

                await CheckDiagramLayersAsync(db, environment, logger, cancellationToken);
                return 0;
            });
            return command;
        }

        // MediaAsset rows for the images already in wwwroot/img/media, from Data/Import/media-map.csv (plan §10).
        // Several old attachments can share one file (identical duplicates), so rows are grouped by StoragePath.
        // Returns old attachment ID → MediaAsset, which later parts use to link strategy and project images.
        private static async Task<Dictionary<int, MediaAsset>> ImportMediaAsync(ApplicationDbContext db,
            IWebHostEnvironment environment, ILogger logger, bool isDryRun, CancellationToken cancellationToken)
        {
            var rows = ReadMediaMap(environment.ContentRootPath);
            var existing = await db.MediaAssets.ToDictionaryAsync(m => m.StoragePath, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var contentTypes = new FileExtensionContentTypeProvider();
            var mediaByWordPressId = new Dictionary<int, MediaAsset>();
            var created = 0;

            var files = rows.GroupBy(r => r.StoragePath, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var file in files)
            {
                if (!existing.TryGetValue(file.Key, out var asset))
                {
                    var info = new FileInfo(Path.Combine(environment.WebRootPath, "img", "media", file.Key));
                    if (!info.Exists)
                    {
                        throw new FileNotFoundException($"media-map.csv lists {file.Key}, but the file is missing.", info.FullName);
                    }

                    var first = file.First();
                    asset = new MediaAsset
                    {
                        FileName = info.Name,
                        ContentType = contentTypes.TryGetContentType(info.Name, out var type) ? type : "application/octet-stream",
                        StoragePath = file.Key,
                        SizeBytes = info.Length,
                        AltText = first.AltText,
                        Width = first.Width,
                        Height = first.Height,
                        UploadedAt = DateTimeOffset.UtcNow,
                    };
                    if (!isDryRun)
                    {
                        db.MediaAssets.Add(asset);
                    }
                    created++;
                }

                foreach (var row in file)
                {
                    mediaByWordPressId[row.WordPressId] = asset;
                }
            }

            logger.LogInformation("Media: {Rows} old attachments, {Files} files; {Verb} {Created}, skipped {Skipped} (already present).",
                rows.Count, files.Count, isDryRun ? "would create" : "created", created, files.Count - created);
            return mediaByWordPressId;
        }

        // ClimateZone rows for the 31 Köppen sub-zones (child terms of the old "climate-zone" taxonomy).
        // Groups (A–E) and diagrams already exist from "tool init reference"; zones are linked to them
        // by the group's code and the diagram's slug. Existing zones (same Köppen code) are skipped.
        // Returns Köppen code → zone (existing and new), which the strategies use for their zone links.
        private static async Task<Dictionary<string, ClimateZone>> ImportZonesAsync(ApplicationDbContext db, SqliteConnection wordPress,
            ILogger logger, bool isDryRun, CancellationToken cancellationToken)
        {
            var groups = await db.ClimateZoneGroups.ToDictionaryAsync(g => g.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var diagrams = await db.Diagrams.ToDictionaryAsync(d => d.Slug, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var zonesByCode = await db.ClimateZones.ToDictionaryAsync(z => z.KoppenCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

            using var command = wordPress.CreateCommand();
            command.CommandText = $"""
                select t.name, t.slug,
                       {TermMeta("t.term_id", "climate-code")}, {TermMeta("t.term_id", "body")},
                       {TermMeta("t.term_id", "heading-color")}, {TermMeta("t.term_id", "map_id")},
                       {TermMeta("t.term_id", "diagram")}, {TermMeta("tt.parent", "climate-code")}
                from wp_terms t
                join wp_term_taxonomy tt on tt.term_id = t.term_id
                where tt.taxonomy = 'climate-zone' and tt.parent <> 0
                order by t.name
                """;

            var total = 0;
            var created = 0;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                total++;
                var code = reader.GetString(2);
                if (zonesByCode.ContainsKey(code))
                {
                    continue;
                }

                var name = reader.GetString(0);
                var diagramSlug = reader.IsDBNull(6) ? "" : reader.GetString(6);
                if (diagramSlug == "")
                {
                    // Am Tropical Monsoon has none on the old site; imported as is, fixed by hand (plan §11, owner's final fixes).
                    logger.LogWarning("Zone {Code} has no diagram on the old site; set it in Admin.", code);
                }

                var mapId = int.TryParse(reader.IsDBNull(5) ? "" : reader.GetString(5), out var id) ? id : (int?)null;
                if (mapId is null)
                {
                    // As has none: the Köppen map's 30 classes don't include it, so it has a page but no map area.
                    logger.LogInformation("Zone {Code} has no map ID (not a class on the Köppen map).", code);
                }

                var zone = new ClimateZone
                {
                    KoppenCode = code,
                    Name = name[(name.IndexOf('_') + 1)..],            // "Cfa_Humid Subtropical" → "Humid Subtropical"
                    Slug = reader.GetString(1).Replace('_', '-'),       // our slugs use hyphens (plan §10)
                    DescriptionHtml = ToHtml(reader.GetString(3)),
                    Color = reader.GetString(4).ToUpperInvariant(),     // #c7ff4f → #C7FF4F
                    MapId = mapId,
                    Diagram = diagramSlug == "" ? null : diagrams[diagramSlug],
                    Group = groups[reader.GetString(7)],
                    SortOrder = total,                                  // alphabetical by code, as on the old site
                };
                if (!isDryRun)
                {
                    db.ClimateZones.Add(zone);
                }
                zonesByCode[code] = zone;
                created++;
            }

            logger.LogInformation("Zones: {Verb} {Created}, skipped {Skipped} (already present).",
                isDryRun ? "would create" : "created", created, total - created);
            return zonesByCode;
        }

        // DesignStrategy rows for the 27 published strategies, with their zone links, reference projects and
        // conflicts (plan Phase 3). A strategy that already exists (same slug) is left as it is, including its
        // links and projects; conflicts are added only where the pair is missing.
        private static async Task ImportStrategiesAsync(ApplicationDbContext db, SqliteConnection wordPress, ILogger logger,
            Dictionary<string, ClimateZone> zonesByCode, Dictionary<int, MediaAsset> mediaByWordPressId,
            bool isDryRun, CancellationToken cancellationToken)
        {
            var existing = await db.DesignStrategies.ToDictionaryAsync(s => s.Slug, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var existingConflicts = (await db.StrategyConflicts.ToListAsync(cancellationToken))
                .Select(c => (c.StrategyId, c.ConflictsWithStrategyId)).ToHashSet();

            var rows = await ReadRowsAsync(wordPress, $"""
                select p.ID, p.post_title, p.post_name, p.post_content, p.post_excerpt,
                       {PostMeta("2030_url")}, {PostMeta("_thumbnail_id")}, {PostMeta("diagram_image")}, {PostMeta("conflicts")}
                from wp_posts p
                where p.post_type = 'climate-strategy' and p.post_status = 'publish'
                order by p.post_title
                """, cancellationToken);

            // Reference projects are stored as numbered fields: reference_projects (count), reference_projects_0_name, …
            var projectFields = (await ReadRowsAsync(wordPress,
                    "select post_id, meta_key, meta_value from wp_postmeta where meta_key like 'reference_projects%'", cancellationToken))
                .ToDictionary(r => (int.Parse(r[0]!), r[1]!), r => r[2]);

            // Links to sub-zones only; the few links to the top-level groups (A–E) are redundant and left out.
            var zoneCodesByStrategy = (await ReadRowsAsync(wordPress, $"""
                    select r.object_id, {TermMeta("tt.term_id", "climate-code")}
                    from wp_term_relationships r
                    join wp_term_taxonomy tt on tt.term_taxonomy_id = r.term_taxonomy_id
                    where tt.taxonomy = 'climate-zone' and tt.parent <> 0
                    """, cancellationToken))
                .ToLookup(r => int.Parse(r[0]!), r => r[1]!);

            MediaAsset? Media(string? id) => int.TryParse(id, out var key) ? mediaByWordPressId.GetValueOrDefault(key) : null;

            var strategies = new Dictionary<int, DesignStrategy>();   // old post ID → strategy
            var conflictPairs = new List<(int StrategyId, int ConflictsWithStrategyId)>();
            var created = 0;
            var links = 0;
            var projects = 0;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var wordPressId = int.Parse(row[0]!);
                var slug = row[2]!;
                conflictPairs.AddRange(ParseConflictIds(row[8]).Select(other => (wordPressId, other)));

                if (existing.TryGetValue(slug, out var strategy))
                {
                    strategies[wordPressId] = strategy;
                    continue;
                }

                var palette2030Url = EmptyToNull(row[5]);
                if (palette2030Url is null || !palette2030Url.Contains(slug, StringComparison.OrdinalIgnoreCase))
                {
                    // Clerestories and Skylights has none; East-West Shading points to earth-sheltering (plan §11, owner's final fixes).
                    logger.LogWarning("Strategy {Slug}: 2030 Palette link is missing or looks wrong ({Url}); check it in Admin.",
                        slug, palette2030Url ?? "none");
                }

                strategy = new DesignStrategy
                {
                    Name = row[1]!,
                    Slug = slug,
                    BodyHtml = ToHtml(row[3]),
                    Summary = EmptyToNull(row[4]),
                    Palette2030Url = palette2030Url,
                    ImageAsset = Media(row[6]),
                    DiagramImageAsset = Media(row[7]),
                    SortOrder = i + 1,                       // alphabetical: the old site had no manual order
                };

                foreach (var code in zoneCodesByStrategy[wordPressId])
                {
                    strategy.Zones.Add(new ClimateZoneStrategy { Zone = zonesByCode[code], SortOrder = strategy.SortOrder });
                    links++;
                }

                var projectCount = int.TryParse(projectFields.GetValueOrDefault((wordPressId, "reference_projects")), out var count) ? count : 0;
                for (var p = 0; p < projectCount; p++)
                {
                    string? Field(string name) => EmptyToNull(projectFields.GetValueOrDefault((wordPressId, $"reference_projects_{p}_{name}")));
                    if (Field("url")?.Contains("callisonrtkl.com", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        // Kept as is; they redirect to a generic Arcadis page (plan §11, owner's final fixes).
                        logger.LogWarning("Strategy {Slug}, project {Project}: link points to callisonrtkl.com ({Url}); replace it with the arcadis.com project page.",
                            slug, Field("name"), Field("url"));
                    }

                    strategy.ReferenceProjects.Add(new ReferenceProject
                    {
                        Name = Field("name")!,
                        Location = Field("description"),     // holds a place name on the old site (plan §10)
                        Url = Field("url"),
                        ImageAsset = Media(Field("image")),
                        SortOrder = p + 1,
                    });
                    projects++;
                }

                if (!isDryRun)
                {
                    db.DesignStrategies.Add(strategy);
                }
                strategies[wordPressId] = strategy;
                created++;
            }

            // Conflicts: skip entries pointing at old IDs that aren't strategies (7 and 8 are WordPress settings
            // records), then make every pair two-way with the tested rules (plan §10).
            var stale = conflictPairs.Where(c => !strategies.ContainsKey(c.ConflictsWithStrategyId)).ToList();
            foreach (var (from, to) in stale)
            {
                logger.LogInformation("Strategy {Slug}: skipped conflict with old ID {Id}, which is not a strategy.", strategies[from].Slug, to);
            }

            var conflictsAdded = 0;
            foreach (var (a, b) in StrategyConflictRules.MakeSymmetric(conflictPairs.Except(stale)))
            {
                var (strategy, other) = (strategies[a], strategies[b]);
                if (existingConflicts.Contains((strategy.Id, other.Id)))   // new strategies have Id 0, so they never match
                {
                    continue;
                }

                if (!isDryRun)
                {
                    db.StrategyConflicts.Add(new StrategyConflict { Strategy = strategy, ConflictsWith = other });
                }
                conflictsAdded++;
            }

            var verb = isDryRun ? "would create" : "created";
            logger.LogInformation("Strategies: {Verb} {Created}, skipped {Skipped} (already present); {Links} zone links, {Projects} reference projects, {Conflicts} conflict rows.",
                verb, created, rows.Count - created, links, projects, conflictsAdded);
        }

        // ContentBlock rows for the editable page text: the About page, the carbon map note and the carbon
        // comparison page with its calculator fields. The old Home page (test cards only) is not imported.
        // CallisonRTKL becomes Arcadis; existing blocks (same key) are skipped.
        private static async Task ImportContentAsync(ApplicationDbContext db, SqliteConnection wordPress,
            ILogger logger, bool isDryRun, CancellationToken cancellationToken)
        {
            var existing = (await db.ContentBlocks.Select(c => c.Key).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var pages = (await ReadRowsAsync(wordPress, $"""
                    select p.post_name, p.post_content, {PostMeta("calculator_intro")}, {PostMeta("calculator_result_intro")},
                           {PostMeta("calculator_result_after")}, {PostMeta("disclaimers")}
                    from wp_posts p
                    where p.post_type = 'page' and p.post_status = 'publish'
                    """, cancellationToken))
                .ToDictionary(r => r[0]!, StringComparer.OrdinalIgnoreCase);
            var comparison = pages["carbon-comparison"];

            (string Key, string Title, string? Text)[] blocks =
            [
                ("about.body", "About page", pages["about"][1]),
                ("carbon.map.note", "Carbon map – note under the map", pages["carbon"][1]),
                ("carbon.comparison.learn-more", "Carbon comparison – Learn more", comparison[1]),
                ("carbon.calculator.intro", "Carbon calculator – introduction", comparison[2]),
                ("carbon.calculator.result-intro", "Carbon calculator – text above the result", comparison[3]),
                ("carbon.calculator.result-after", "Carbon calculator – text after the result", comparison[4]),
                ("carbon.calculator.disclaimers", "Carbon calculator – disclaimers", comparison[5]),
            ];

            var created = 0;
            foreach (var (key, title, text) in blocks)
            {
                if (existing.Contains(key))
                {
                    continue;
                }

                var html = (ToHtml(text) ?? "")
                    .Replace("CallisonRTKL’s", "Arcadis’")
                    .Replace("CallisonRTKL", "Arcadis");
                if (OldBrand.IsMatch(html))
                {
                    logger.LogWarning("Content {Key} still mentions the old company name; reword it in Admin.", key);
                }

                if (!isDryRun)
                {
                    db.ContentBlocks.Add(new ContentBlock { Key = key, Title = title, Html = html, UpdatedAt = DateTimeOffset.UtcNow });
                }
                created++;
            }

            logger.LogInformation("Content blocks: {Verb} {Created}, skipped {Skipped} (already present).",
                isDryRun ? "would create" : "created", created, blocks.Length - created);
        }

        // Links each of the 4 diagrams (created by "tool init reference") to its SVG in wwwroot/img/media/diagrams/{slug}.svg,
        // copied from the old theme. Diagrams that already have an SVG are left alone.
        private static async Task ImportDiagramsAsync(ApplicationDbContext db, IWebHostEnvironment environment,
            ILogger logger, bool isDryRun, CancellationToken cancellationToken)
        {
            var diagrams = await db.Diagrams.Where(d => d.SvgAssetId == null).ToListAsync(cancellationToken);
            foreach (var diagram in diagrams)
            {
                var storagePath = $"diagrams/{diagram.Slug}.svg";
                var info = new FileInfo(Path.Combine(environment.WebRootPath, "img", "media", storagePath));
                if (!info.Exists)
                {
                    throw new FileNotFoundException($"Diagram {diagram.Slug} has no SVG file.", info.FullName);
                }

                var asset = await db.MediaAssets.FirstOrDefaultAsync(m => m.StoragePath == storagePath, cancellationToken)
                    ?? new MediaAsset
                    {
                        FileName = info.Name,
                        ContentType = "image/svg+xml",
                        StoragePath = storagePath,
                        SizeBytes = info.Length,
                        AltText = $"{diagram.Name} building diagram",
                        UploadedAt = DateTimeOffset.UtcNow,
                    };
                if (!isDryRun)
                {
                    diagram.SvgAsset = asset;
                }
            }

            logger.LogInformation("Diagrams: {Verb} {Linked} SVGs, skipped {Skipped} (already linked).",
                isDryRun ? "would link" : "linked", diagrams.Count, await db.Diagrams.CountAsync(cancellationToken) - diagrams.Count);
        }

        // Checks every diagram SVG has a ds-{slug} layer for each strategy used by the zones showing that diagram;
        // the zone page toggles these layers. Runs on the saved data, so it reflects later edits in Admin too.
        // Missing layers need new artwork (plan §11, owner's final fixes).
        private static async Task CheckDiagramLayersAsync(ApplicationDbContext db, IWebHostEnvironment environment,
            ILogger logger, CancellationToken cancellationToken)
        {
            var diagrams = await db.Diagrams.Include(d => d.SvgAsset).Where(d => d.SvgAsset != null).ToListAsync(cancellationToken);
            var used = await db.ClimateZoneStrategies
                .Where(l => l.Zone.DiagramId != null)
                .Select(l => new { DiagramId = l.Zone.DiagramId!.Value, l.Strategy.Slug, l.Zone.KoppenCode })
                .ToListAsync(cancellationToken);

            var missingCount = 0;
            foreach (var diagram in diagrams)
            {
                var svg = await File.ReadAllTextAsync(Path.Combine(environment.WebRootPath, "img", "media", diagram.SvgAsset!.StoragePath), cancellationToken);
                var layers = DiagramLayer.Matches(svg).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var missing in used.Where(u => u.DiagramId == diagram.Id && !layers.Contains(u.Slug)).GroupBy(u => u.Slug))
                {
                    logger.LogWarning("Diagram {Diagram} has no layer for strategy {Slug} (used by zones {Zones}); it needs artwork.",
                        diagram.Slug, missing.Key, string.Join(", ", missing.Select(m => m.KoppenCode).Order()));
                    missingCount++;
                }
            }

            logger.LogInformation("Diagram layers: checked {Diagrams} diagrams, {Missing} missing layers.", diagrams.Count, missingCount);
        }

        private static readonly Regex DiagramLayer = new("id=\"ds-([^\"]+)\"");

        // Old WordPress text has no <p> tags: WordPress added them when showing the page, one paragraph per line.
        // Wraps each plain-text line in <p>, leaves lines that start with a block tag (<h2>, <ul>, <li> …) as they are,
        // and replaces the outdated <acronym> tag with <abbr>.
        private static string? ToHtml(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var lines = text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var html = string.Join("\n", lines.Select(line => BlockTag.IsMatch(line) ? line : $"<p>{line}</p>"));
            return html.Replace("<acronym", "<abbr").Replace("</acronym>", "</abbr>");
        }

        private static readonly Regex BlockTag =
            new(@"^</?(h[1-6]|p|div|ul|ol|li|table|thead|tbody|tr|td|th|blockquote|figure|hr|img)\b", RegexOptions.IgnoreCase);

        private static readonly Regex OldBrand = new(@"callison|crtkl", RegexOptions.IgnoreCase);

        // SQL for one WordPress term field, e.g. the "climate-code" of the zone or of its parent group.
        private static string TermMeta(string termId, string key) =>
            $"(select meta_value from wp_termmeta where term_id = {termId} and meta_key = '{key}')";

        // SQL for one WordPress post field of p (the strategy), e.g. its 2030_url.
        private static string PostMeta(string key) =>
            $"(select meta_value from wp_postmeta where post_id = p.ID and meta_key = '{key}')";

        // Runs a query on the old database and returns every row as text (null for empty database values).
        private static async Task<List<string?[]>> ReadRowsAsync(SqliteConnection wordPress, string sql, CancellationToken cancellationToken)
        {
            using var command = wordPress.CreateCommand();
            command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<string?[]>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new string?[reader.FieldCount];
                for (var i = 0; i < row.Length; i++)
                {
                    row[i] = reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture);
                }
                rows.Add(row);
            }
            return rows;
        }

        // The old "conflicts" field is a PHP-serialized list of post IDs: a:2:{i:0;s:2:"84";i:1;s:2:"59";} → 84, 59.
        private static IEnumerable<int> ParseConflictIds(string? serialized) =>
            Regex.Matches(serialized ?? "", "s:\\d+:\"(\\d+)\"").Select(m => int.Parse(m.Groups[1].Value));

        private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

        // Columns: WordPressId, OldPath, Folder, StoragePath, Title, AltText, Width, Height.
        // TextFieldParser handles quoted values with commas, e.g. "Gateway, The".
        private static List<MediaMapRow> ReadMediaMap(string contentRootPath) =>
            ReadCsv(contentRootPath, "media-map.csv")
                .Select(fields => new MediaMapRow(
                    int.Parse(fields[0]),
                    fields[3],
                    string.IsNullOrWhiteSpace(fields[5]) ? null : fields[5],
                    int.TryParse(fields[6], out var width) ? width : null,
                    int.TryParse(fields[7], out var height) ? height : null))
                .ToList();

        // Reads a CSV from Data/Import (header skipped). TextFieldParser handles quoted values with commas.
        private static List<string[]> ReadCsv(string contentRootPath, string fileName)
        {
            using var parser = new TextFieldParser(Path.Combine(contentRootPath, "Data", "Import", fileName))
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
            };
            parser.SetDelimiters(",");
            parser.ReadFields();   // header

            var rows = new List<string[]>();
            while (parser.ReadFields() is { } fields)
            {
                rows.Add(fields);
            }
            return rows;
        }

        private record MediaMapRow(int WordPressId, string StoragePath, string? AltText, int? Width, int? Height);
    }
}
