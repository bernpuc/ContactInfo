namespace ContactInfo.Services.Interfaces;

public interface IUserSettingsService
{
    string ApolloApiKey            { get; }
    string RocketReachApiKey       { get; }
    string SignalHireApiKey        { get; }
    string WebhookCallbackUrl      { get; }
    string WebhookRelayPollUrl     { get; }
    bool   DemoMode                { get; }
    bool   ApolloEnabled           { get; }
    bool   RocketReachEnabled      { get; }
    bool   SignalHireEnabled       { get; }
    bool   IsSourceEnabled(string sourceName);
    void Save(string apolloApiKey, string rocketReachApiKey, string signalHireApiKey,
              string webhookCallbackUrl, string webhookRelayPollUrl,
              bool demoMode, bool apolloEnabled, bool rocketReachEnabled, bool signalHireEnabled);
}
