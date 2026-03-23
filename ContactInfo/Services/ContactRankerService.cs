using ContactInfo.Models;

namespace ContactInfo.Services;

public static class ContactRankerService
{
    public static (List<RankedContact> emails, List<RankedContact> phones) Rank(
        IEnumerable<SourceResult> results)
    {
        var list = results.ToList();
        return (
            RankValues(list.SelectMany(r => r.Emails.Select(e => (r.SourceName, e)))),
            RankValues(list.SelectMany(r => r.Phones.Select(p => (r.SourceName, p))))
        );
    }

    private static List<RankedContact> RankValues(
        IEnumerable<(string source, string value)> items)
    {
        return items
            .GroupBy(x => x.value, StringComparer.OrdinalIgnoreCase)
            .Select(g => new RankedContact
            {
                Value          = g.First().value,
                Score          = g.Select(x => x.source).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                FoundOnSources = g.Select(x => x.source).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            })
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Value)
            .ToList();
    }
}
