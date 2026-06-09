using System.Net.Http;
using System.Text;
using System.Text.Json;
using BenefitSubsidyMobileAndroid.Models;

namespace BenefitSubsidyMobileAndroid.Services;

public class FirebaseDatabaseService
{
    private readonly HttpClient _httpClient = new();
    private readonly string _databaseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public FirebaseDatabaseService(string databaseUrl)
    {
        _databaseUrl = databaseUrl.TrimEnd('/');
    }

    public async Task<string> GetUserRoleAsync(string uid, string idToken)
    {
        var safeUid = Uri.EscapeDataString(uid);
        var url = $"{_databaseUrl}/users/{safeUid}/role.json?auth={Uri.EscapeDataString(idToken)}";
        using var response = await _httpClient.GetAsync(url);
        var text = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception("Не удалось проверить роль пользователя: " + text);

        if (string.IsNullOrWhiteSpace(text) || text == "null")
            return string.Empty;

        return text.Trim().Trim('"');
    }

    public async Task<List<ApplicationRequest>> GetApplicationsAsync(string idToken, string managerUid, string managerEmail)
    {
        var url = $"{_databaseUrl}/applications.json?auth={Uri.EscapeDataString(idToken)}";
        using var response = await _httpClient.GetAsync(url);
        var text = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception("Ошибка загрузки applications из Firebase: " + text);

        if (string.IsNullOrWhiteSpace(text) || text == "null")
            return new List<ApplicationRequest>();

        var data = JsonSerializer.Deserialize<Dictionary<string, FirebaseApplicationDto>>(text, JsonOptions)
                   ?? new Dictionary<string, FirebaseApplicationDto>();

        var list = new List<ApplicationRequest>();
        foreach (var pair in data)
        {
            var dto = pair.Value;
            if (dto.IsDeleted) continue;

            var request = ToApplicationRequest(dto);
            if (string.IsNullOrWhiteSpace(request.RequestId))
                request.RequestId = pair.Key;

            if (IsAssignedToManager(request, managerUid, managerEmail))
                list.Add(request);
        }

        return list.OrderByDescending(x => x.Priority == "Срочный")
                   .ThenByDescending(x => x.SubmittedAt)
                   .ToList();
    }

    public async Task UpdateApplicationAsManagerAsync(ApplicationRequest request, string idToken, string managerUid)
    {
        request.LastModifiedUtc = DateTime.UtcNow;
        request.ModifiedBy = "manager";

        var safeId = Uri.EscapeDataString(request.RequestId);
        var url = $"{_databaseUrl}/applications/{safeId}.json?auth={Uri.EscapeDataString(idToken)}";

        var patch = new
        {
            status = request.Status,
            managerComment = request.ManagerComment,
            lastModified = request.LastModifiedUtc.ToUniversalTime().ToString("O"),
            modifiedBy = "manager"
        };

        await PatchJsonAsync(url, patch, "Ошибка обновления заявки менеджером");
        await AddAuditLogAsync(idToken, request.RequestId, "MANAGER_UPDATE", $"Менеджер изменил статус заявки {request.RequestId} на '{request.Status}'", managerUid, "INFO");
        await AddNotificationAsync(idToken, request, managerUid);
        await UpdateManagerStatsAsync(idToken, managerUid, request.Status);
    }

    private static bool IsAssignedToManager(ApplicationRequest request, string managerUid, string managerEmail)
    {
        if (string.IsNullOrWhiteSpace(request.AssignedManagerUid)) return true;
        if (request.AssignedManagerUid == managerUid) return true;
        if (request.AssignedManagerUid.Equals("manager", StringComparison.OrdinalIgnoreCase)) return true;
        if (request.AssignedManagerName.Contains("менеджер", StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.IsNullOrWhiteSpace(managerEmail) && request.AssignedManagerName.Contains(managerEmail, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private async Task PatchJsonAsync<T>(string url, T value, string errorPrefix)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
        {
            Content = content
        };
        using var response = await _httpClient.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(errorPrefix + ": " + text);
    }

    private async Task PutJsonAsync<T>(string url, T value, string errorPrefix)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PutAsync(url, content);
        var text = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(errorPrefix + ": " + text);
    }

    private async Task AddAuditLogAsync(string idToken, string applicationId, string action, string message, string uid, string level)
    {
        try
        {
            var key = $"log_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}";
            var url = $"{_databaseUrl}/auditLogs/{key}.json?auth={Uri.EscapeDataString(idToken)}";
            var dto = new FirebaseAuditLogDto
            {
                Level = level,
                Action = action,
                ApplicationId = applicationId,
                UserUid = uid,
                Role = "manager",
                Message = message,
                CreatedAt = DateTime.UtcNow.ToString("O")
            };
            await PutJsonAsync(url, dto, "Ошибка записи auditLogs");
        }
        catch
        {
            // Логи в облако не должны ломать основную операцию изменения статуса.
        }
    }

    private async Task AddNotificationAsync(string idToken, ApplicationRequest request, string managerUid)
    {
        try
        {
            var key = $"notif_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}";
            var url = $"{_databaseUrl}/notifications/{key}.json?auth={Uri.EscapeDataString(idToken)}";
            var dto = new FirebaseNotificationDto
            {
                Type = "manager_status_update",
                ApplicationId = request.RequestId,
                Title = "Статус заявки изменён",
                Text = $"Менеджер обновил заявку {request.RequestId}: {request.Status}",
                TargetRole = "operator",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.ToString("O")
            };
            await PutJsonAsync(url, dto, "Ошибка записи notifications");
        }
        catch
        {
        }
    }

    private async Task UpdateManagerStatsAsync(string idToken, string managerUid, string status)
    {
        try
        {
            var day = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var safeUid = Uri.EscapeDataString(managerUid);
            var url = $"{_databaseUrl}/managerStats/{safeUid}/{day}.json?auth={Uri.EscapeDataString(idToken)}";

            using var getResponse = await _httpClient.GetAsync(url);
            var text = await getResponse.Content.ReadAsStringAsync();
            var stats = string.IsNullOrWhiteSpace(text) || text == "null"
                ? new FirebaseManagerStatsDto()
                : JsonSerializer.Deserialize<FirebaseManagerStatsDto>(text, JsonOptions) ?? new FirebaseManagerStatsDto();

            stats.Processed++;
            if (status == "Одобрено" || status == "Выполнено") stats.Approved++;
            if (status == "Отказано") stats.Rejected++;
            stats.UpdatedAt = DateTime.UtcNow.ToString("O");

            await PutJsonAsync(url, stats, "Ошибка обновления managerStats");
        }
        catch
        {
        }
    }

    private static ApplicationRequest ToApplicationRequest(FirebaseApplicationDto dto)
    {
        return new ApplicationRequest
        {
            RequestId = dto.Id,
            FullName = dto.FullName,
            Phone = dto.Phone,
            Address = dto.Address,
            BenefitCategory = dto.BenefitCategory,
            ServiceType = dto.ServiceType,
            Status = dto.Status,
            Priority = dto.Priority,
            SubmittedAt = ParseDate(dto.DateCreated, DateTime.Today),
            DocumentNumber = dto.DocumentNumber,
            OperatorExternalComment = dto.OperatorExternalComment,
            ManagerComment = dto.ManagerComment,
            AssignedManagerUid = dto.AssignedManagerUid,
            AssignedManagerName = dto.AssignedManagerName,
            CreatedByUid = dto.CreatedByUid,
            CreatedAt = dto.CreatedAt,
            LastModifiedUtc = ParseDate(dto.LastModified, DateTime.UtcNow).ToUniversalTime(),
            ModifiedBy = dto.ModifiedBy,
            IsDeleted = dto.IsDeleted
        };
    }

    private static DateTime ParseDate(string? value, DateTime fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        return DateTime.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
