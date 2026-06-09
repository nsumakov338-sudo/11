using System.IO;
using System.Text.Json;

namespace BenefitSubsidyDesktop.Services;

public class FirebaseConfig
{
    public string AppName { get; set; } = "Ростелеком — льготные подключения и субсидии";
    public bool UseLocalDemoCloud { get; set; } = true;
    public int SyncIntervalSeconds { get; set; } = 8;
    public string FirebaseDatabaseUrl { get; set; } = string.Empty;
    public string FirebaseApiKey { get; set; } = string.Empty;
    public string FirebaseOperatorEmail { get; set; } = "operator@test.ru";
    public string FirebaseOperatorPassword { get; set; } = "123456";
    public string FirebaseManagerUid { get; set; } = string.Empty;
    public string FirebaseManagerName { get; set; } = "Менеджер Ростелеком";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string SettingsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

    public bool IsFirebaseReady =>
        !UseLocalDemoCloud &&
        !string.IsNullOrWhiteSpace(FirebaseDatabaseUrl) &&
        !string.IsNullOrWhiteSpace(FirebaseApiKey) &&
        !string.IsNullOrWhiteSpace(FirebaseOperatorEmail) &&
        !string.IsNullOrWhiteSpace(FirebaseOperatorPassword);

    public static FirebaseConfig Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                var empty = new FirebaseConfig();
                empty.Save();
                return empty;
            }

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<FirebaseConfig>(json, JsonOptions) ?? new FirebaseConfig();
        }
        catch (Exception ex)
        {
            LogService.Error("Ошибка чтения appsettings.json", ex);
            return new FirebaseConfig();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            LogService.Error("Ошибка сохранения appsettings.json", ex);
            throw;
        }
    }
}
