using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BenefitSubsidyDesktop.Services;

public class FirebaseAuthService
{
    private readonly HttpClient _httpClient = new();

    public async Task<FirebaseAuthResult> SignInAsync(string apiKey, string email, string password)
    {
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";
        var body = new
        {
            email,
            password,
            returnSecureToken = true
        };

        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(url, content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception("Ошибка авторизации Firebase: " + responseText);

        var result = JsonSerializer.Deserialize<FirebaseAuthResult>(responseText);
        if (result == null || string.IsNullOrWhiteSpace(result.IdToken))
            throw new Exception("Firebase не вернул idToken. Проверьте Email/Password Authentication.");

        return result;
    }
}

public class FirebaseAuthResult
{
    [JsonPropertyName("idToken")]
    public string IdToken { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("expiresIn")]
    public string ExpiresIn { get; set; } = string.Empty;

    [JsonPropertyName("localId")]
    public string LocalId { get; set; } = string.Empty;
}
