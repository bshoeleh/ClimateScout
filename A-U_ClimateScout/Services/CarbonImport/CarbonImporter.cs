using System.Text.Json;
using A_U_ClimateScout.Data;
using A_U_ClimateScout.Models;
using Microsoft.EntityFrameworkCore;

namespace A_U_ClimateScout.Services.CarbonImport
{
    // Imports one carbon CSV as one batch (plan §6): parse → match regions → compare with the current values
    // for the same source. New values are added; a changed value is added and the old row is marked as superseded
    // by this batch (never deleted, so the batch can be rolled back in Phase 6); unchanged values are left alone.
    // Everything is saved in one transaction, so a failure leaves the database as it was. A dry run saves nothing.
    // Used by "tool import carbon" now, and by the Admin upload screen in Phase 6.
    public class CarbonImporter(ApplicationDbContext db)
    {
        public async Task<CarbonImportSummary> ImportAsync(string fileName, TextReader reader, bool dryRun, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var parsed = CarbonCsvParser.Parse(reader, now.Year);
            var profile = parsed.Profile;
            var source = await db.CarbonDataSources.SingleOrDefaultAsync(s => s.Name == profile.SourceName, cancellationToken)
                ?? throw new CarbonImportException($"The data source \"{profile.SourceName}\" doesn't exist yet. Run \"tool init reference\" first.");

            var regions = await db.CarbonRegions.AsNoTracking()
                .Select(r => new CarbonRegionInfo(r.Id, r.Code, r.Name, r.RegionType, r.ParentRegion != null ? r.ParentRegion.Code : null))
                .ToListAsync(cancellationToken);
            var aliases = await db.CarbonRegionAliases.AsNoTracking()
                .Select(a => new CarbonAliasInfo(a.RegionId, a.Alias, a.Source))
                .ToListAsync(cancellationToken);
            var match = CarbonRegionMatcher.Match(profile, parsed.Rows, regions, aliases);

            // Current values from this source, tracked so the changed ones can be marked as superseded.
            var current = await db.CarbonIntensities
                .Where(i => i.SourceId == source.Id && i.SupersededByBatchId == null)
                .ToDictionaryAsync(i => (i.RegionId, i.Year), cancellationToken);

            var added = new List<CarbonMatchedRow>();
            var replaced = new List<CarbonIntensity>();
            var unchanged = 0;
            foreach (var row in match.Matched)
            {
                // Stored to 3 decimals, so compare at that precision.
                var value = Math.Round(row.Row.Value, 3);
                if (!current.TryGetValue((row.RegionId, row.Row.Year), out var existing))
                {
                    added.Add(row);
                }
                else if (existing.ValueGPerKWh != value)
                {
                    added.Add(row);
                    replaced.Add(existing);
                }
                else
                {
                    unchanged++;
                }
            }

            var problems = parsed.Problems.Concat(match.Problems).OrderBy(p => p.Line).ToList();
            var summary = new CarbonImportSummary(profile.Name, source.Name,
                NewRows: added.Count - replaced.Count, ChangedRows: replaced.Count, UnchangedRows: unchanged,
                Problems: problems, BatchId: null);
            if (dryRun)
            {
                return summary;
            }

            var batch = new CarbonImportBatch
            {
                FileName = fileName,
                SourceId = source.Id,
                Profile = profile.Name,
                Status = CarbonImportStatus.Committed,
                UploadedAt = now,
                CommittedAt = now,
                TotalRows = parsed.Rows.Count + parsed.Problems.Count,
                NewRows = summary.NewRows,
                ChangedRows = summary.ChangedRows,
                UnchangedRows = summary.UnchangedRows,
                UnmatchedRows = match.Problems.Count,
                InvalidRows = parsed.Problems.Count,
                ErrorsJson = problems.Count > 0 ? JsonSerializer.Serialize(problems) : null,
            };

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            db.CarbonImportBatches.Add(batch);
            await db.SaveChangesAsync(cancellationToken);

            // Retire the old values first: the database allows only one current value per region, source and year.
            foreach (var old in replaced)
            {
                old.SupersededByBatchId = batch.Id;
            }
            await db.SaveChangesAsync(cancellationToken);

            db.CarbonIntensities.AddRange(added.Select(row => new CarbonIntensity
            {
                RegionId = row.RegionId,
                SourceId = source.Id,
                Year = row.Row.Year,
                ValueGPerKWh = Math.Round(row.Row.Value, 3),
                ImportBatchId = batch.Id,
            }));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return summary with { BatchId = batch.Id };
        }
    }

    // What an import did (or, for a dry run, would do). BatchId is null for a dry run.
    public record CarbonImportSummary(string Profile, string Source, int NewRows, int ChangedRows, int UnchangedRows,
        IReadOnlyList<CarbonCsvProblem> Problems, int? BatchId);
}
