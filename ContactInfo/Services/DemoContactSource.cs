using ContactInfo.Models;
using ContactInfo.Services.Interfaces;
using System.Text.RegularExpressions;

namespace ContactInfo.Services;

/// <summary>
/// Generates deterministic fake contact data from any LinkedIn URL.
/// Register two instances (DemoA / DemoB) so the multi-source ranking and
/// green-highlight logic can be demonstrated without real API keys.
///
/// Each URL is assigned a scenario (0–7) by hashing the username.
/// Append "-sN" to a LinkedIn username to force scenario N (used by the sample import).
///
///   0 — full multi-source      : email + phone on both sources → both green
///   1 — email multi-source     : email matches across sources, phone only on A
///   2 — phone multi-source     : phone matches across sources, emails differ (not green)
///   3 — all single-source      : work email + phone on A; personal email on B; nothing green
///   4 — email only             : one email, single source; no phone
///   5 — phone only             : one phone, single source; no email
///   6 — no results             : profile found but no contact info on either source
///   7 — multiple emails        : A has work email + personal email + phone; B has nothing
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
        var scenario              = GetScenario(username, hash);

        result.Name    = $"{Cap(firstName)} {Cap(lastName)}";
        result.Title   = SampleTitle(hash);
        result.Company = $"{Cap(lastName)} Group";

        var workEmail     = $"{firstName}.{lastName}@{lastName}group.com";
        var personalEmail = $"{firstName}{lastName[0]}@gmail.com";
        var phone1        = GeneratePhone(hash);
        var phone2        = GeneratePhone(Math.Abs(hash * 31 + 999983));

        switch (scenario)
        {
            case 0: // full multi-source: same email + same phone on both → both green
                result.Emails.Add(new LabeledValue(workEmail, "work"));
                result.Phones.Add(new LabeledValue(phone1, "mobile"));
                break;

            case 1: // email multi-source: email matches, phone only on A
                result.Emails.Add(new LabeledValue(workEmail, "work"));
                if (_variant == 1)
                    result.Phones.Add(new LabeledValue(phone1, "mobile"));
                else
                    result.Emails.Add(new LabeledValue(personalEmail, "personal"));
                break;

            case 2: // phone multi-source: phone matches, emails differ
                result.Phones.Add(new LabeledValue(phone1, "mobile"));
                if (_variant == 1)
                    result.Emails.Add(new LabeledValue(workEmail, "work"));
                else
                    result.Emails.Add(new LabeledValue(personalEmail, "personal"));
                break;

            case 3: // all single-source: work email + phone on A; personal email on B
                if (_variant == 1)
                {
                    result.Emails.Add(new LabeledValue(workEmail, "work"));
                    result.Phones.Add(new LabeledValue(phone1, "mobile"));
                }
                else
                {
                    result.Emails.Add(new LabeledValue(personalEmail, "personal"));
                }
                break;

            case 4: // email only, single source
                if (_variant == 1)
                    result.Emails.Add(new LabeledValue(workEmail, "work"));
                break;

            case 5: // phone only, single source
                if (_variant == 1)
                    result.Phones.Add(new LabeledValue(phone1, "mobile"));
                break;

            case 6: // no results — profile found but no contact info
                break;

            case 7: // multiple emails + phone, single source
                if (_variant == 1)
                {
                    result.Emails.Add(new LabeledValue(workEmail, "work"));
                    result.Emails.Add(new LabeledValue(personalEmail, "personal"));
                    result.Phones.Add(new LabeledValue(phone1, "mobile"));
                    result.Phones.Add(new LabeledValue(phone2, "direct"));
                }
                break;
        }

        return Task.FromResult(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the scenario index. A "-sN" suffix on the username forces scenario N,
    /// allowing sample import files to guarantee full coverage. Otherwise uses hash % 8.
    /// </summary>
    private static int GetScenario(string username, int hash)
    {
        var m = Regex.Match(username, @"-s(\d)$");
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n) && n < 8)
            return n;
        return hash % 8;
    }

    private static string ExtractUsername(string url)
    {
        var m = Regex.Match(url, @"linkedin\.com/in/([^/?#]+)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.ToLower() : "demo-user";
    }

    private static (string first, string last) ParseName(string username)
    {
        // Strip trailing scenario suffix (-sN) and numeric ID segment (e.g. "john-doe-32a4")
        var clean = Regex.Replace(username, @"-s\d$", "");
        clean     = Regex.Replace(clean,   @"-[a-f0-9]{4,}$", "");
        var parts = clean.Split('-', StringSplitOptions.RemoveEmptyEntries);
        var first = parts.Length > 0 ? AlphaOnly(parts[0])  : "demo";
        var last  = parts.Length > 1 ? AlphaOnly(parts[^1]) : "user";
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
