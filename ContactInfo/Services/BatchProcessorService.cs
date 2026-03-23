using ContactInfo.Models;
using ContactInfo.Services.Interfaces;

namespace ContactInfo.Services;

public class BatchProcessorService
{
    private readonly IEnumerable<IContactSource> _sources;

    public BatchProcessorService(IEnumerable<IContactSource> sources)
    {
        _sources = sources;
    }

    /// <summary>
    /// Processes each row one at a time (all sources queried in parallel per row).
    /// Calls onRowComplete after each row so the UI can update progressively.
    /// </summary>
    public async Task ProcessAsync(BatchJob job, Func<Task> onRowComplete,
        CancellationToken ct = default)
    {
        var sourceList  = _sources.ToList();
        job.SourceNames = sourceList.Select(s => s.Name).ToList();
        job.IsRunning   = true;

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
            row.Status       = results.Any(r => r.Success) ? RowStatus.Complete : RowStatus.Failed;

            await onRowComplete();
        }

        job.IsRunning = false;
        await onRowComplete();
    }
}
