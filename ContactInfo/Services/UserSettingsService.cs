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

    public string ApolloApiKey => _data.ApolloApiKey;

    public void Save(string apolloApiKey)
    {
        _data = new UserSettingsData { ApolloApiKey = apolloApiKey };
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
        ApolloApiKey = s.ApolloApiKey
    };

    private class UserSettingsData
    {
        public string ApolloApiKey { get; set; } = "";
    }
}
