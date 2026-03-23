namespace ContactInfo.Services.Interfaces;

public interface IUserSettingsService
{
    string ApolloApiKey { get; }
    void Save(string apolloApiKey);
}
