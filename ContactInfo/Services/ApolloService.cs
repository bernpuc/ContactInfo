using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContactInfo.Models;
using ContactInfo.Services.Interfaces;

namespace ContactInfo.Services;

// Apollo.io People Enrichment API
// POST https://api.apollo.io/api/v1/people/match
// Docs: https://docs.apollo.io/reference/people-enrichment
// NOTE: people/match requires a paid Apollo plan. Free plans cannot access this endpoint.

public class ApolloService : IContactSource
{
    private const string BaseUrl = "https://api.apollo.io/api/v1/people/match";

    public string Name => "Apollo";

    private readonly HttpClient           _http;
    private readonly IUserSettingsService _settings;

    public ApolloService(HttpClient http, IUserSettingsService settings)
    {
        _http     = http;
        _settings = settings;
    }

    public async Task<SourceResult> LookupAsync(string linkedInUrl)
    {
        var result = new SourceResult { SourceName = Name };

        var apiKey = _settings.ApolloApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            result.Error = "Apollo API key not configured.";
            return result;
        }

        try
        {
            var url = BaseUrl +
                      "?linkedin_url=" + WebUtility.UrlEncode(linkedInUrl.TrimEnd('/')) +
                      "&reveal_personal_emails=true" +
                      "&reveal_phone_number=false";

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key",    apiKey);
            request.Headers.Add("Accept",        "application/json");
            request.Headers.Add("Cache-Control", "no-cache");

            var response = await _http.SendAsync(request);
            var json     = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                result.Error = "Invalid API key.";
                return result;
            }
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                result.Error = "Rate limit exceeded (600 req/hr).";
                return result;
            }
            if (!response.IsSuccessStatusCode)
            {
                var err = JsonSerializer.Deserialize<ApolloErrorResponse>(json, JsonOptions);
                result.Error = err?.Error ?? err?.Message ?? $"HTTP {(int)response.StatusCode}";
                return result;
            }

            var root   = JsonSerializer.Deserialize<ApolloResponse>(json, JsonOptions);
            var person = root?.Person;

            if (person is null)
            {
                result.Error = "Profile not found in Apollo's database.";
                return result;
            }

            result.Name    = person.Name;
            result.Title   = person.Title;
            result.Company = person.Organization?.Name;
            result.Success = true;

            // Emails — prefer contact_emails array, fall back to person.email
            var emailSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var ce in person.Contact?.ContactEmails ?? [])
                if (!string.IsNullOrWhiteSpace(ce.Email) && emailSet.Add(ce.Email))
                    result.Emails.Add(new LabeledValue(ce.Email));

            if (!string.IsNullOrWhiteSpace(person.Email) && emailSet.Add(person.Email))
                result.Emails.Add(new LabeledValue(person.Email));

            // Phones — Apollo does not return a personal/business label
            result.Phones = (person.Contact?.PhoneNumbers ?? [])
                .Where(p => !string.IsNullOrWhiteSpace(p.SanitizedNumber))
                .Select(p => new LabeledValue(p.SanitizedNumber!))
                .ToList();
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        return result;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private class ApolloResponse      { public ApolloPerson?  Person { get; set; } }
    private class ApolloErrorResponse { public string? Error { get; set; } public string? Message { get; set; } }

    private class ApolloPerson
    {
        public string?        Name         { get; set; }
        public string?        Title        { get; set; }
        public string?        Email        { get; set; }
        [JsonPropertyName("email_status")]
        public string?        EmailStatus  { get; set; }
        public ApolloContact? Contact      { get; set; }
        public ApolloOrg?     Organization { get; set; }
    }

    private class ApolloContact
    {
        [JsonPropertyName("contact_emails")]
        public List<ApolloEmail>? ContactEmails { get; set; }
        [JsonPropertyName("phone_numbers")]
        public List<ApolloPhone>? PhoneNumbers  { get; set; }
    }

    private class ApolloEmail
    {
        public string? Email       { get; set; }
        [JsonPropertyName("email_status")]
        public string? EmailStatus { get; set; }
    }

    private class ApolloPhone
    {
        [JsonPropertyName("sanitized_number")]
        public string? SanitizedNumber { get; set; }
    }

    private class ApolloOrg { public string? Name { get; set; } }
}
