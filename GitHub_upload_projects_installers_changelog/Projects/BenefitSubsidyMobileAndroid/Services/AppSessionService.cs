namespace BenefitSubsidyMobileAndroid.Services;

public class AppSession
{
    public string DatabaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public string Uid { get; set; } = string.Empty;
}

public static class AppSessionService
{
    private const string DatabaseUrlKey = "firebase_database_url";
    private const string ApiKeyKey = "firebase_api_key";
    private const string EmailKey = "manager_email";
    private const string LastActiveKey = "last_active_utc";
    private const string PasswordKey = "manager_password";
    private const string TokenKey = "firebase_id_token";
    private const string UidKey = "firebase_uid";

    public static string SavedDatabaseUrl
    {
        get => Preferences.Default.Get(DatabaseUrlKey, string.Empty);
        set => Preferences.Default.Set(DatabaseUrlKey, value?.Trim() ?? string.Empty);
    }

    public static string SavedApiKey
    {
        get => Preferences.Default.Get(ApiKeyKey, string.Empty);
        set => Preferences.Default.Set(ApiKeyKey, value?.Trim() ?? string.Empty);
    }

    public static string SavedEmail
    {
        get => Preferences.Default.Get(EmailKey, "manager@test.ru");
        set => Preferences.Default.Set(EmailKey, value?.Trim() ?? string.Empty);
    }

    public static async Task<AppSession> LoginAsync(string databaseUrl, string apiKey, string email, string password)
    {
        databaseUrl = databaseUrl.Trim().TrimEnd('/');
        apiKey = apiKey.Trim();
        email = email.Trim();

        if (string.IsNullOrWhiteSpace(databaseUrl)) throw new Exception("Введите Firebase Database URL.");
        if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("Введите Web API Key.");
        if (string.IsNullOrWhiteSpace(email)) throw new Exception("Введите Email менеджера.");
        if (string.IsNullOrWhiteSpace(password)) throw new Exception("Введите пароль менеджера.");

        var auth = new FirebaseAuthService();
        var authResult = await auth.SignInAsync(apiKey, email, password);

        var database = new FirebaseDatabaseService(databaseUrl);
        var role = await database.GetUserRoleAsync(authResult.LocalId, authResult.IdToken);

        if (!role.Equals("manager", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("У пользователя нет роли manager. В Firebase Realtime Database добавьте users/" + authResult.LocalId + "/role = manager.");
        }

        SavedDatabaseUrl = databaseUrl;
        SavedApiKey = apiKey;
        SavedEmail = email;
        Preferences.Default.Set(LastActiveKey, DateTime.UtcNow.ToString("O"));

        await SecureStorage.Default.SetAsync(PasswordKey, password);
        await SecureStorage.Default.SetAsync(TokenKey, authResult.IdToken);
        await SecureStorage.Default.SetAsync(UidKey, authResult.LocalId);

        return new AppSession
        {
            DatabaseUrl = databaseUrl,
            ApiKey = apiKey,
            Email = email,
            IdToken = authResult.IdToken,
            Uid = authResult.LocalId
        };
    }

    public static async Task<AppSession?> TryRestoreSessionAsync()
    {
        var lastActiveText = Preferences.Default.Get(LastActiveKey, string.Empty);
        if (!DateTime.TryParse(lastActiveText, out var lastActiveUtc))
            return null;

        if (DateTime.UtcNow - lastActiveUtc.ToUniversalTime() > TimeSpan.FromHours(24))
        {
            await LogoutAsync();
            return null;
        }

        var databaseUrl = SavedDatabaseUrl;
        var apiKey = SavedApiKey;
        var email = SavedEmail;
        var password = await SecureStorage.Default.GetAsync(PasswordKey);

        if (string.IsNullOrWhiteSpace(databaseUrl) || string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        // idToken живёт недолго, поэтому при автологине выполняем повторный вход по сохранённым данным.
        return await LoginAsync(databaseUrl, apiKey, email, password);
    }

    public static void Touch()
    {
        Preferences.Default.Set(LastActiveKey, DateTime.UtcNow.ToString("O"));
    }

    public static async Task LogoutAsync()
    {
        Preferences.Default.Remove(LastActiveKey);
        SecureStorage.Default.Remove(TokenKey);
        SecureStorage.Default.Remove(UidKey);
        SecureStorage.Default.Remove(PasswordKey);
        await Task.CompletedTask;
    }
}
