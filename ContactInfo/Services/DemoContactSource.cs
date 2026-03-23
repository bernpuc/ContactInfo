using ContactInfo.Models;
using ContactInfo.Services.Interfaces;
using System.Text.RegularExpressions;

namespace ContactInfo.Services;

/// <summary>
/// Generates deterministic fake contact data from any LinkedIn URL.
/// Register two instances (DemoA / DemoB) so the multi-source ranking and
/// green-highlight logic can be demonstrated without real API keys.
///
/// Overlap strategy:
///   DemoA — work email  + phone
///   DemoB — work email (same → triggers multi-source green) + personal gmail
/// </summary>
public class DemoContactSource : IContactSource
{
    private readonly int _variant; // 1 = DemoA, 2 = DemoB

    public string Name { get; }

    public DemoContactSource(string name, int variant)
    {
        Name     = name;
        _variant = variant;
    }

    public Task<SourceResult> LookupAsync(string linkedInUrl)
    {
        var result = new SourceResult { SourceName = Name, Success = true };

        var username              = ExtractUsername(linkedInUrl);
        var (firstName, lastName) = ParseName(username);
        var hash                  = ComputeHash(username);

        result.Name    = $"{Cap(firstName)} {Cap(lastName)}";
        result.Title   = SampleTitle(hash);
        result.Company = $"{Cap(lastName)} Group";

        var workEmail     = $"{firstName}.{lastName}@{lastName}group.com";
        var personalEmail = $"{firstName}{lastName[0]}@gmail.com";
        var phone         = GeneratePhone(hash);

        if (_variant == 1)
        {
            // DemoA: work email + phone
            result.Emails.Add(workEmail);
            result.Phones.Add(phone);
        }
        else
        {
            // DemoB: same work email (creates multi-source match) + personal email
            result.Emails.Add(workEmail);
            result.Emails.Add(personalEmail);
        }

        return Task.FromResult(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string ExtractUsername(string url)
    {
        var m = Regex.Match(url, @"linkedin\.com/in/([^/?#]+)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.ToLower() : "demo-user";
    }

    private static (string first, string last) ParseName(string username)
    {
        // Strip trailing numeric ID segment (e.g. "john-doe-32a4")
        var clean  = Regex.Replace(username, @"-[a-f0-9]{4,}$", "");
        var parts  = clean.Split('-', StringSplitOptions.RemoveEmptyEntries);
        var first  = parts.Length > 0 ? AlphaOnly(parts[0])  : "demo";
        var last   = parts.Length > 1 ? AlphaOnly(parts[^1]) : "user";
        return (first.Length > 0 ? first : "demo",
                last.Length  > 0 ? last  : "user");
    }

    private static string AlphaOnly(string s) =>
        new(s.Where(char.IsLetter).ToArray());

    private static string Cap(string s) =>
        s.Length == 0 ? s : char.ToUpper(s[0]) + s[1..];

    private static int ComputeHash(string s)
    {
        unchecked
        {
            int h = 17;
            foreach (var c in s) h = h * 31 + c;
            return Math.Abs(h);
        }
    }

    private static string GeneratePhone(int hash)
    {
        // Produce a US-style number: +1 (NXX) NXX-XXXX
        // Area code 200-999, avoid 555 etc.
        var area = 200 + (hash % 800);
        var mid  = 200 + ((hash / 800) % 800);
        var last = (hash / 640000) % 10000;
        return $"+1{area:D3}{mid:D3}{last:D4}";
    }

    private static readonly string[] Titles =
    [
        "Software Engineer", "Product Manager", "Marketing Director",
        "Sales Executive", "Operations Manager", "Data Analyst",
        "UX Designer", "Business Development Manager", "CTO", "CEO"
    ];

    private static string SampleTitle(int hash) => Titles[hash % Titles.Length];
}
