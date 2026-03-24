using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ContactInfo.Models;
using ContactInfo.Services.Interfaces;

namespace ContactInfo.Services;

// SignalHire API v1 — Candidate Search
// Step 1: POST https://www.signalhire.com/api/v1/candidate/search
//   Header:       apikey: YOUR_KEY
//   Content-Type: application/x-www-form-urlencoded (required by API even though body is JSON)
//   Body:         { "items": ["linkedin_url"], "callbackUrl": "https://..." }
//   Response 201: { "requestId": 123456 }
//
// Step 2: SignalHire POSTs results to callbackUrl. The poll endpoint returns HTML, not JSON.
//   Results are retrieved via a webhook relay (e.g. webhook.site).
//   Relay poll URL format: https://webhook.site/token/{uuid}/requests

public class SignalHireService : IContactSource
{
    private const string SearchUrl = "https://www.signalhire.com/api/v1/candidate/search";

    public string Name => "SignalHire";

    private readonly HttpClient           _http;
    private readonly IUserSettingsService _settings;

    public SignalHireService(HttpClient http, IUserSettingsService settings)
    {
        _http     = http;
        _settings = settings;
    }

    public async Task<SourceResult> LookupAsync(string linkedInUrl)
    {
        var result = new SourceResult { SourceName = Name };

        var apiKey = _settings.SignalHireApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            result.Error = "SignalHire API key not configured.";
            return result;
        }
        if (string.IsNullOrWhiteSpace(_settings.WebhookCallbackUrl))
        {
            result.Error = "SignalHire requires a Webhook Callback URL — set it in Settings.";
            return result;
        }

        var relayPollUrl = ResolveRelayPollUrl();
        if (relayPollUrl is null)
        {
            result.Error = "Webhook Relay Poll URL not configured — set it in Settings.";
            return result;
        }

        try
        {
            var submittedAt = DateTimeOffset.UtcNow;
            var requestId   = await SubmitSearch(apiKey, linkedInUrl, result);
            if (requestId == 0) return result;

            var candidate = await PollRelay(relayPollUrl, linkedInUrl, submittedAt, result);
            if (candidate is null) return result;

            result.Success = true;
            result.Name    = candidate.FullName;
            result.Title   = candidate.HeadLine;
            result.Company = candidate.Experience?
                .FirstOrDefault(e => e.Current == true)?.Company;

            foreach (var contact in candidate.Contacts ?? [])
            {
                if (string.IsNullOrWhiteSpace(contact.Value)) continue;
                if (contact.Type == "email") result.Emails.Add(contact.Value);
                else if (contact.Type == "phone") result.Phones.Add(contact.Value);
            }
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        return result;
    }

    private static string StripScheme(string? url) =>
        Regex.Replace(url?.TrimEnd('/').ToLowerInvariant() ?? "", @"^https?://", "");

    // If the callbackUrl is a webhook.site URL, derive the relay poll URL automatically.
    // Otherwise fall back to the explicitly configured relay poll URL.
    private string? ResolveRelayPollUrl()
    {
        var callbackUrl = _settings.WebhookCallbackUrl;
        var m = Regex.Match(callbackUrl, @"webhook\.site/([0-9a-f\-]{36})", RegexOptions.IgnoreCase);
        if (m.Success)
            return $"https://webhook.site/token/{m.Groups[1].Value}/requests";

        return string.IsNullOrWhiteSpace(_settings.WebhookRelayPollUrl)
            ? null
            : _settings.WebhookRelayPollUrl;
    }

    private async Task<int> SubmitSearch(string apiKey, string linkedInUrl, SourceResult result)
    {
        var payload = new { items = new[] { linkedInUrl }, callbackUrl = _settings.WebhookCallbackUrl };
        var body    = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, SearchUrl)
        {
            // SignalHire requires x-www-form-urlencoded content type even though body is JSON
            Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded")
        };
        request.Headers.Add("apikey", apiKey);

        var response = await _http.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            result.Error = "Invalid API key.";
            return 0;
        }
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            result.Error = "Rate limit exceeded.";
            return 0;
        }
        if (!response.IsSuccessStatusCode)
        {
            var body2 = await response.Content.ReadAsStringAsync();
            var err   = JsonSerializer.Deserialize<SignalHireErrorResponse>(body2, JsonOptions);
            result.Error = err?.Error ?? err?.Message ?? $"HTTP {(int)response.StatusCode}";
            return 0;
        }

        var json   = await response.Content.ReadAsStringAsync();
        var parsed = JsonSerializer.Deserialize<SignalHireSearchResponse>(json, JsonOptions);

        if (parsed?.RequestId == 0)
        {
            result.Error = "No requestId returned by SignalHire.";
            return 0;
        }

        return parsed!.RequestId;
    }

    // Poll the webhook relay for a response matching our LinkedIn URL,
    // only considering entries created after we submitted the request.
    private async Task<SignalHireCandidate?> PollRelay(
        string relayPollUrl, string linkedInUrl,
        DateTimeOffset submittedAt, SourceResult result)
    {
        const int maxAttempts = 20;
        const int delayMs     = 3000;

        var normalizedUrl  = StripScheme(linkedInUrl);
        var pollUrl        = relayPollUrl.TrimEnd('/') + "?sorting=newest&per_page=50";
        string? lastParseError = null;
        string? lastItemSeen   = null;

        for (int i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(delayMs);

            var response = await _http.GetAsync(pollUrl);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                result.Error = "Webhook relay URL has expired or is invalid — update it in Settings.";
                return null;
            }
            if (!response.IsSuccessStatusCode) continue;

            var json   = await response.Content.ReadAsStringAsync();
            var relay  = JsonSerializer.Deserialize<WebhookSiteResponse>(json, JsonOptions);

            foreach (var entry in relay?.Data ?? [])
            {
                if (string.IsNullOrWhiteSpace(entry.Content)) continue;
                // Skip entries older than when we submitted this request
                // Use a generous 5-minute window to account for timezone/parse edge cases
                if (entry.CreatedAtParsed < submittedAt.AddMinutes(-5)) continue;

                List<SignalHireCallbackItem>? items;
                try { items = JsonSerializer.Deserialize<List<SignalHireCallbackItem>>(entry.Content, JsonOptions); }
                catch (Exception ex) { lastParseError = ex.Message; continue; }

                foreach (var it in items ?? [])
                    lastItemSeen = it.Item;

                var match = items?.FirstOrDefault(x =>
                    StripScheme(x.Item) == normalizedUrl &&
                    x.Status == "success");

                if (match?.Candidate is not null)
                    return match.Candidate;

                // Explicit not-found signal from SignalHire
                var notFound = items?.FirstOrDefault(x =>
                    StripScheme(x.Item) == normalizedUrl &&
                    x.Status != "success");
                if (notFound is not null)
                {
                    result.Error = "Profile not found in SignalHire's database.";
                    return null;
                }
            }
        }

        if (lastParseError is not null)
            result.Error = $"Timed out — content parse error: {lastParseError}";
        else if (lastItemSeen is not null)
            result.Error = $"Timed out — callback received but URL did not match. Got: '{lastItemSeen}', expected: '{normalizedUrl}'";
        else
            result.Error = "Timed out — no SignalHire callback received at relay.";
        return null;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── SignalHire models ────────────────────────────────────────────────────

    private class SignalHireSearchResponse
    {
        [JsonPropertyName("requestId")]
        public int RequestId { get; set; }
    }

    private class SignalHireErrorResponse
    {
        public string? Error   { get; set; }
        public string? Message { get; set; }
    }

    // Callback body: array of these items
    private class SignalHireCallbackItem
    {
        public string?              Status    { get; set; }
        public string?              Item      { get; set; }
        public SignalHireCandidate? Candidate { get; set; }
    }

    private class SignalHireCandidate
    {
        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }
        [JsonPropertyName("headLine")]
        public string? HeadLine { get; set; }
        public List<SignalHireContact>?    Contacts   { get; set; }
        public List<SignalHireExperience>? Experience { get; set; }
    }

    private class SignalHireContact
    {
        public string? Type    { get; set; }
        public string? Value   { get; set; }
        [JsonPropertyName("subType")]
        public string? SubType { get; set; }
    }

    private class SignalHireExperience
    {
        public bool?   Current  { get; set; }
        public string? Company  { get; set; }
        public string? Position { get; set; }
    }

    // ── Webhook relay models ─────────────────────────────────────────────────

    private class WebhookSiteResponse
    {
        public List<WebhookSiteEntry>? Data { get; set; }
    }

    private class WebhookSiteEntry
    {
        public string? Content { get; set; }
        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        public DateTimeOffset CreatedAtParsed =>
            DateTimeOffset.TryParse(CreatedAt, out var dt) ? dt :
            DateTime.TryParseExact(CreatedAt, "yyyy-MM-dd HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal, out var dt2)
            ? new DateTimeOffset(dt2, TimeSpan.Zero)
            : DateTimeOffset.MaxValue; // unknown time → don't skip it
    }
}
