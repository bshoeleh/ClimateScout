using System.CommandLine;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Models;
using Microsoft.AspNetCore.StaticFiles;
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
            };
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

                var mediaByWordPressId = await ImportMediaAsync(db, environment, logger, isDryRun, cancellationToken);

                if (!isDryRun)
                {
                    await db.SaveChangesAsync(cancellationToken);
                }

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

        // Columns: WordPressId, OldPath, Folder, StoragePath, Title, AltText, Width, Height.
        // TextFieldParser handles quoted values with commas, e.g. "Gateway, The".
        private static List<MediaMapRow> ReadMediaMap(string contentRootPath)
        {
            using var parser = new TextFieldParser(Path.Combine(contentRootPath, "Data", "Import", "media-map.csv"))
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
            };
            parser.SetDelimiters(",");
            parser.ReadFields();   // header

            var rows = new List<MediaMapRow>();
            while (parser.ReadFields() is { } fields)
            {
                rows.Add(new MediaMapRow(
                    int.Parse(fields[0]),
                    fields[3],
                    string.IsNullOrWhiteSpace(fields[5]) ? null : fields[5],
                    int.TryParse(fields[6], out var width) ? width : null,
                    int.TryParse(fields[7], out var height) ? height : null));
            }
            return rows;
        }

        private record MediaMapRow(int WordPressId, string StoragePath, string? AltText, int? Width, int? Height);
    }
}
