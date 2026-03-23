using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContactInfo.Models;
using ContactInfo.Services.Interfaces;

namespace ContactInfo.Services;

// SignalHire API v1 — Candidate Search
// Step 1: POST https://www.signalhire.com/api/v1/candidate/search
//   Header: apikey: YOUR_KEY
//   Body:   { "items": ["linkedin_url"] }
//   Response 202: { "requestId": "..." }
// Step 2: Poll GET https://www.signalhire.com/api/v1/request/{requestId}
//   Until status is "done", "notFound", or "error"
//   Returns: candidates[].contacts[] with type ("email"/"phone"), value, label

public class SignalHireService : IContactSource
{
    private const string SearchUrl = "https://www.signalhire.com/api/v1/candidate/search";
    private const string RequestUrl = "https://www.signalhire.com/api/v1/request/";

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

        try
        {
            // Step 1 — submit search
            var requestId = await SubmitSearch(apiKey, linkedInUrl, result);
            if (requestId is null) return result;

            // Step 2 — poll for completion
            var candidate = await PollForResult(apiKey, requestId, result);
            if (candidate is null) return result;

            result.Success = true;
            result.Name    = candidate.FullName;
            result.Title   = candidate.Headline;

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

    private async Task<string?> SubmitSearch(string apiKey, string linkedInUrl, SourceResult result)
    {
        var body    = JsonSerializer.Serialize(new { items = new[] { linkedInUrl } });
        var request = new HttpRequestMessage(HttpMethod.Post, SearchUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("apikey", apiKey);

        var response = await _http.SendAsync(request);

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
        if (response.StatusCode != HttpStatusCode.Accepted && !response.IsSuccessStatusCode)
        {
            result.Error = $"HTTP {(int)response.StatusCode}";
            return null;
        }

        var json     = await response.Content.ReadAsStringAsync();
        var parsed   = JsonSerializer.Deserialize<SignalHireSearchResponse>(json, JsonOptions);
        var requestId = parsed?.RequestId;

        if (string.IsNullOrWhiteSpace(requestId))
        {
            result.Error = "No requestId returned by SignalHire.";
            return null;
        }

        return requestId;
    }

    private async Task<SignalHireCandidate?> PollForResult(string apiKey, string requestId, SourceResult result)
    {
        const int maxAttempts = 15;
        const int delayMs     = 2000;

        for (int i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(delayMs);

            var request = new HttpRequestMessage(HttpMethod.Get, RequestUrl + requestId);
            request.Headers.Add("apikey", apiKey);

            var response = await _http.SendAsync(request);
            var json     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                result.Error = $"HTTP {(int)response.StatusCode} while polling.";
                return null;
            }

            var parsed = JsonSerializer.Deserialize<SignalHireRequestResponse>(json, JsonOptions);

            if (parsed?.Status == "notFound")
            {
                result.Error = "Profile not found in SignalHire's database.";
                return null;
            }
            if (parsed?.Status == "error")
            {
                result.Error = "SignalHire returned an error processing this profile.";
                return null;
            }
            if (parsed?.Status == "done")
            {
                return parsed.Candidates?.FirstOrDefault();
            }
        }

        result.Error = "Lookup timed out waiting for SignalHire to complete.";
        return null;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private class SignalHireSearchResponse
    {
        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }
    }

    private class SignalHireRequestResponse
    {
        public string?                    Status     { get; set; }
        public List<SignalHireCandidate>? Candidates { get; set; }
    }

    private class SignalHireCandidate
    {
        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }
        public string? Headline { get; set; }
        public List<SignalHireContact>? Contacts { get; set; }
    }

    private class SignalHireContact
    {
        public string? Type  { get; set; }
        public string? Value { get; set; }
        public string? Label { get; set; }
    }
}
