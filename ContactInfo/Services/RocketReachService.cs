using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContactInfo.Models;
using ContactInfo.Services.Interfaces;

namespace ContactInfo.Services;

// RocketReach API v2 — Person Lookup
// GET https://api.rocketreach.co/api/v2/person/lookup?linkedin_url=...
// Header: Api-Key: YOUR_KEY
// Docs: https://docs.rocketreach.co/reference/people-lookup-api
// May return status "searching" — poll until "complete" or "failed"

public class RocketReachService : IContactSource
{
    private const string BaseUrl = "https://api.rocketreach.co/api/v2/person/lookup";

    public string Name => "RocketReach";

    private readonly HttpClient           _http;
    private readonly IUserSettingsService _settings;

    public RocketReachService(HttpClient http, IUserSettingsService settings)
    {
        _http     = http;
        _settings = settings;
    }

    public async Task<SourceResult> LookupAsync(string linkedInUrl)
    {
        var result = new SourceResult { SourceName = Name };

        var apiKey = _settings.RocketReachApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            result.Error = "RocketReach API key not configured.";
            return result;
        }

        try
        {
            // Step 1 — submit lookup
            var profile = await PostLookup(apiKey, linkedInUrl, result);
            if (profile is null) return result;

            // Step 2 — poll if still searching
            if (profile.Status == "searching")
            {
                profile = await PollUntilComplete(apiKey, profile.Id, result);
                if (profile is null) return result;
            }

            if (profile.Status == "failed" || profile.Status == "not_found")
            {
                result.Error = "Profile not found in RocketReach's database.";
                return result;
            }

            result.Name    = profile.Name;
            result.Title   = profile.CurrentTitle;
            result.Company = profile.CurrentEmployer;
            result.Success = true;

            foreach (var e in profile.Emails ?? [])
                if (!string.IsNullOrWhiteSpace(e.Email))
                    result.Emails.Add(new LabeledValue(e.Email, e.Type));

            foreach (var p in profile.Phones ?? [])
                if (!string.IsNullOrWhiteSpace(p.Number))
                    result.Phones.Add(new LabeledValue(p.Number, p.Type));
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        return result;
    }

    private async Task<RocketReachProfile?> PostLookup(string apiKey, string linkedInUrl, SourceResult result)
    {
        var url = $"{BaseUrl}?linkedin_url={Uri.EscapeDataString(linkedInUrl)}";
        return await GetWithRetry(apiKey, url, result);
    }

    private async Task<RocketReachProfile?> PollUntilComplete(string apiKey, int id, SourceResult result)
    {
        const int maxAttempts = 10;
        const int delayMs     = 2000;

        for (int i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(delayMs);

            var profile = await GetWithRetry(apiKey, $"{BaseUrl}?id={id}", result);
            if (profile is null) return null;
            if (profile.Status != "searching") return profile;
        }

        result.Error = "Lookup timed out waiting for RocketReach to complete.";
        return null;
    }

    // Retries up to 3 times on 429, with increasing delays (10s, 20s, 30s).
    private async Task<RocketReachProfile?> GetWithRetry(string apiKey, string url, SourceResult result)
    {
        const int maxRetries = 3;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Api-Key", apiKey);

            var response = await _http.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt < maxRetries - 1)
                {
                    await Task.Delay((attempt + 1) * 10_000); // 10s, 20s
                    continue;
                }
                result.Error = "Rate limit exceeded — try again in a minute.";
                return null;
            }

            return await HandleResponse(response, result);
        }

        result.Error = "Rate limit exceeded — try again in a minute.";
        return null;
    }

    private async Task<RocketReachProfile?> HandleResponse(HttpResponseMessage response, SourceResult result)
    {
        var json = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            result.Error = "Invalid API key.";
            return null;
        }
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            result.Error = "Rate limit exceeded.";
            return null;
        }
        if (!response.IsSuccessStatusCode)
        {
            var err = JsonSerializer.Deserialize<RocketReachError>(json, JsonOptions);
            result.Error = err?.Detail ?? err?.Error ?? $"HTTP {(int)response.StatusCode}";
            return null;
        }

        return JsonSerializer.Deserialize<RocketReachProfile>(json, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private class RocketReachProfile
    {
        public int     Id              { get; set; }
        public string? Status          { get; set; }
        public string? Name            { get; set; }
        [JsonPropertyName("current_title")]
        public string? CurrentTitle    { get; set; }
        [JsonPropertyName("current_employer")]
        public string? CurrentEmployer { get; set; }
        public List<RocketReachEmail>? Emails { get; set; }
        public List<RocketReachPhone>? Phones { get; set; }
    }

    private class RocketReachEmail
    {
        public string? Email { get; set; }
        public string? Type  { get; set; }
        [JsonPropertyName("smtp_valid")]
        public string? SmtpValid { get; set; }
    }

    private class RocketReachPhone
    {
        public string? Number { get; set; }
        public string? Type   { get; set; }
    }

    private class RocketReachError
    {
        public string? Detail { get; set; }
        public string? Error  { get; set; }
    }
}
