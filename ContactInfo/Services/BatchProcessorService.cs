using ContactInfo.Models;
using ContactInfo.Services.Interfaces;

namespace ContactInfo.Services;

public class BatchProcessorService
{
    private readonly IEnumerable<IContactSource> _sources;
    private readonly IUserSettingsService        _settings;

    public BatchProcessorService(IEnumerable<IContactSource> sources, IUserSettingsService settings)
    {
        _sources  = sources;
        _settings = settings;
    }

    /// <summary>
    /// Processes each row one at a time (all sources queried in parallel per row).
    /// Calls onRowComplete after each row so the UI can update progressively.
    /// </summary>
    public async Task ProcessAsync(BatchJob job, Func<Task> onRowComplete,
        CancellationToken ct = default)
    {
        var sourceList  = ActiveSources().ToList();
        job.SourceNames = sourceList.Select(s => s.Name).ToList();
        job.IsRunning  = true;

        if (sourceList.Count == 0)
        {
            foreach (var row in job.Rows)
            {
                row.Status = RowStatus.Failed;
                row.Error  = _settings.DemoMode
                    ? "No demo sources registered."
                    : "No API sources configured. Add an API key in Settings.";
            }
            job.IsRunning = false;
            await onRowComplete();
            return;
        }

        foreach (var row in job.Rows)
        {
            if (ct.IsCancellationRequested) break;

            row.Status = RowStatus.Processing;
            await onRowComplete();

            // All sources queried in parallel for this one URL
            var tasks   = sourceList.Select(s => s.LookupAsync(row.LinkedInUrl));
            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                row.SourceResults[result.SourceName] = result;

                // Use first successful result for profile info
                if (row.Name is null && result.Name is not null)
                {
                    row.Name    = result.Name;
                    row.Title   = result.Title;
                    row.Company = result.Company;
                }
            }

            var (emails, phones) = ContactRankerService.Rank(results);
            row.RankedEmails = emails;
            row.RankedPhones = phones;

            if (results.Any(r => r.Success))
            {
                row.Status = RowStatus.Complete;
            }
            else
            {
                row.Status = RowStatus.Failed;
                row.Error  = string.Join("; ", results
                    .Where(r => r.Error is not null)
                    .Select(r => $"{r.SourceName}: {r.Error}"));
            }

            await onRowComplete();
        }

        job.IsRunning = false;
        await onRowComplete();
    }

    private IEnumerable<IContactSource> ActiveSources()
    {
        var sources = _settings.DemoMode
            ? _sources.Where(s => s is DemoContactSource)
            : _sources.Where(s => s is not DemoContactSource);
        return sources.Where(s => _settings.IsSourceEnabled(s.Name));
    }
}
