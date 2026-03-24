using System.Text.Json;
using ContactInfo.Models;
using ContactInfo.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ContactInfo.Services;

public class UserSettingsService : IUserSettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ContactInfo", "user-settings.json");

    private UserSettingsData _data;

    public UserSettingsService(IOptions<AppSettings> fallback)
    {
        _data = Load(fallback.Value);
    }

    public string ApolloApiKey          => _data.ApolloApiKey;
    public string RocketReachApiKey     => _data.RocketReachApiKey;
    public string SignalHireApiKey       => _data.SignalHireApiKey;
    public string WebhookCallbackUrl     => _data.WebhookCallbackUrl;
    public string WebhookRelayPollUrl    => _data.WebhookRelayPollUrl;
    public bool   DemoMode              => _data.DemoMode;
    public bool   ApolloEnabled      => _data.ApolloEnabled;
    public bool   RocketReachEnabled => _data.RocketReachEnabled;
    public bool   SignalHireEnabled  => _data.SignalHireEnabled;

    public bool IsSourceEnabled(string sourceName) => sourceName switch
    {
        "Apollo"      => _data.ApolloEnabled,
        "RocketReach" => _data.RocketReachEnabled,
        "SignalHire"  => _data.SignalHireEnabled,
        _             => true
    };

    public void Save(string apolloApiKey, string rocketReachApiKey, string signalHireApiKey,
                     string webhookCallbackUrl, string webhookRelayPollUrl,
                     bool demoMode, bool apolloEnabled, bool rocketReachEnabled, bool signalHireEnabled)
    {
        _data = new UserSettingsData
        {
            ApolloApiKey       = apolloApiKey,
            RocketReachApiKey  = rocketReachApiKey,
            SignalHireApiKey   = signalHireApiKey,
            WebhookCallbackUrl = webhookCallbackUrl,
            WebhookRelayPollUrl = webhookRelayPollUrl,
            DemoMode               = demoMode,
            ApolloEnabled          = apolloEnabled,
            RocketReachEnabled     = rocketReachEnabled,
            SignalHireEnabled      = signalHireEnabled
        };
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath,
            JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static UserSettingsData Load(AppSettings fallback)
    {
        if (File.Exists(SettingsPath))
        {
            try
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<UserSettingsData>(json) ?? FromFallback(fallback);
            }
            catch { /* fall through */ }
        }
        return FromFallback(fallback);
    }

    private static UserSettingsData FromFallback(AppSettings s) => new()
    {
        ApolloApiKey      = s.ApolloApiKey,
        RocketReachApiKey = s.RocketReachApiKey,
        SignalHireApiKey  = s.SignalHireApiKey
    };

    private class UserSettingsData
    {
        public string ApolloApiKey          { get; set; } = "";
        public string RocketReachApiKey     { get; set; } = "";
        public string SignalHireApiKey       { get; set; } = "";
        public string WebhookCallbackUrl     { get; set; } = "";
        public string WebhookRelayPollUrl    { get; set; } = "";
        public bool   DemoMode              { get; set; } = true;
        public bool   ApolloEnabled         { get; set; } = true;
        public bool   RocketReachEnabled    { get; set; } = true;
        public bool   SignalHireEnabled     { get; set; } = true;
    }
}
