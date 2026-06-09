using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenefitSubsidyDesktop.Models;

namespace BenefitSubsidyDesktop.Services;

public class FirebaseDatabaseService
{
    private readonly HttpClient _httpClient = new();
    private readonly string _databaseUrl;
    private readonly FirebaseConfig _config;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public FirebaseDatabaseService(string databaseUrl, FirebaseConfig config)
    {
        _databaseUrl = databaseUrl.TrimEnd('/');
        _config = config;
    }

    public async Task<List<ApplicationRequest>> GetApplicationsAsync(string idToken)
    {
        var publicUrl = $"{_databaseUrl}/applications.json?auth={Uri.EscapeDataString(idToken)}";
        using var publicResponse = await _httpClient.GetAsync(publicUrl);
        var publicText = await publicResponse.Content.ReadAsStringAsync();

        if (!publicResponse.IsSuccessStatusCode)
            throw new Exception("Ошибка загрузки applications из Firebase: " + publicText);

        var publicData = string.IsNullOrWhiteSpace(publicText) || publicText == "null"
            ? new Dictionary<string, FirebaseApplicationDto>()
            : JsonSerializer.Deserialize<Dictionary<string, FirebaseApplicationDto>>(publicText, JsonOptions) ?? new Dictionary<string, FirebaseApplicationDto>();

        var privateData = await TryLoadPrivateAsync(idToken);
        var result = new List<ApplicationRequest>();

        foreach (var pair in publicData)
        {
            var dto = pair.Value;
            if (dto.IsDeleted) continue;

            privateData.TryGetValue(pair.Key, out var privateDto);
            var request = ToApplicationRequest(dto, privateDto);
            if (!string.IsNullOrWhiteSpace(request.RequestId))
                result.Add(request);
        }

        return result;
    }

    public async Task SaveApplicationAsync(ApplicationRequest request, string idToken, string operatorUid)
    {
        request.LastModifiedUtc = DateTime.UtcNow;
        request.LastModifiedBy = "operator";

        var safeId = Uri.EscapeDataString(request.RequestId);
        var publicUrl = $"{_databaseUrl}/applications/{safeId}.json?auth={Uri.EscapeDataString(idToken)}";
        var privateUrl = $"{_databaseUrl}/applicationPrivate/{safeId}.json?auth={Uri.EscapeDataString(idToken)}";

        var publicDto = ToFirebaseApplicationDto(request, operatorUid);
        var privateDto = ToFirebasePrivateDto(request, operatorUid);

        await PutJsonAsync(publicUrl, publicDto, "Ошибка сохранения applications в Firebase");
        await PutJsonAsync(privateUrl, privateDto, "Ошибка сохранения applicationPrivate в Firebase");
        await AddAuditLogAsync(idToken, request.RequestId, "UPSERT_APPLICATION", $"Оператор сохранил заявку {request.RequestId}", operatorUid, "INFO");

        if (request.Priority == "Срочный" || request.BenefitCategory == "Инвалид" || request.BenefitCategory == "Многодетная семья")
            await AddNotificationAsync(idToken, request);
    }

    public async Task DeleteApplicationAsync(ApplicationRequest request, string idToken, string operatorUid)
    {
        request.Deleted = true;
        request.LastModifiedUtc = DateTime.UtcNow;
        request.LastModifiedBy = "operator";

        var safeId = Uri.EscapeDataString(request.RequestId);
        var publicUrl = $"{_databaseUrl}/applications/{safeId}.json?auth={Uri.EscapeDataString(idToken)}";
        var publicDto = ToFirebaseApplicationDto(request, operatorUid);
        await PutJsonAsync(publicUrl, publicDto, "Ошибка пометки заявки как удаленной в Firebase");
        await AddAuditLogAsync(idToken, request.RequestId, "DELETE_APPLICATION", $"Оператор удалил заявку {request.RequestId}", operatorUid, "WARNING");
    }

    private async Task<Dictionary<string, FirebasePrivateDto>> TryLoadPrivateAsync(string idToken)
    {
        var privateUrl = $"{_databaseUrl}/applicationPrivate.json?auth={Uri.EscapeDataString(idToken)}";
        using var response = await _httpClient.GetAsync(privateUrl);
        var text = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            LogService.Warning("Не удалось загрузить applicationPrivate из Firebase: " + text);
            return new Dictionary<string, FirebasePrivateDto>();
        }

        if (string.IsNullOrWhiteSpace(text) || text == "null")
            return new Dictionary<string, FirebasePrivateDto>();

        return JsonSerializer.Deserialize<Dictionary<string, FirebasePrivateDto>>(text, JsonOptions) ?? new Dictionary<string, FirebasePrivateDto>();
    }

    private async Task PutJsonAsync<T>(string url, T value, string errorPrefix)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PutAsync(url, content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(errorPrefix + ": " + responseText);
    }

    private async Task AddAuditLogAsync(string idToken, string applicationId, string action, string message, string operatorUid, string level)
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
                UserUid = operatorUid,
                Role = "operator",
                Message = message,
                CreatedAt = DateTime.UtcNow.ToString("O")
            };
            await PutJsonAsync(url, dto, "Ошибка записи auditLogs");
        }
        catch (Exception ex)
        {
            LogService.Warning("Не удалось записать облачный auditLogs: " + ex.Message);
        }
    }

    private async Task AddNotificationAsync(string idToken, ApplicationRequest request)
    {
        try
        {
            var key = $"notif_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}";
            var url = $"{_databaseUrl}/notifications/{key}.json?auth={Uri.EscapeDataString(idToken)}";
            var dto = new FirebaseNotificationDto
            {
                Type = request.Priority == "Срочный" ? "urgent_application" : "important_category_application",
                ApplicationId = request.RequestId,
                Title = request.Priority == "Срочный" ? "Срочная заявка" : "Важная категория льготы",
                Text = $"Поступила заявка {request.RequestId}: {request.FullName}, {request.BenefitCategory}",
                TargetRole = "manager",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.ToString("O")
            };
            await PutJsonAsync(url, dto, "Ошибка записи notifications");
        }
        catch (Exception ex)
        {
            LogService.Warning("Не удалось записать облачное уведомление: " + ex.Message);
        }
    }

    private FirebaseApplicationDto ToFirebaseApplicationDto(ApplicationRequest request, string operatorUid)
    {
        return new FirebaseApplicationDto
        {
            Id = request.RequestId,
            FullName = request.FullName,
            Phone = request.Phone,
            Address = request.Address,
            BenefitCategory = request.BenefitCategory,
            ServiceType = request.ServiceType,
            Status = request.Status,
            Priority = request.Priority,
            DateCreated = request.SubmittedAt.ToString("yyyy-MM-dd"),
            DocumentNumber = request.DocumentNumber,
            OperatorExternalComment = request.ExternalComment,
            ManagerComment = string.Empty,
            AssignedManagerUid = string.IsNullOrWhiteSpace(_config.FirebaseManagerUid) ? request.AssignedManager : _config.FirebaseManagerUid,
            AssignedManagerName = string.IsNullOrWhiteSpace(_config.FirebaseManagerName) ? request.AssignedManager : _config.FirebaseManagerName,
            CreatedByUid = operatorUid,
            CreatedAt = request.SubmittedAt.ToUniversalTime().ToString("O"),
            LastModified = request.LastModifiedUtc.ToUniversalTime().ToString("O"),
            ModifiedBy = request.LastModifiedBy,
            IsDeleted = request.Deleted
        };
    }

    private static FirebasePrivateDto ToFirebasePrivateDto(ApplicationRequest request, string operatorUid)
    {
        return new FirebasePrivateDto
        {
            InternalComment = request.InternalComment,
            SpecialFlag = request.SpecialFlag == "Нет" ? string.Empty : request.SpecialFlag,
            LegalApprovalRequired = request.SpecialFlag == "Требует согласования с юридическим отделом",
            WaitingDocumentConfirmation = request.SpecialFlag == "Ожидает подтверждения документа",
            UpdatedAt = request.LastModifiedUtc.ToUniversalTime().ToString("O"),
            UpdatedByUid = operatorUid
        };
    }

    private static ApplicationRequest ToApplicationRequest(FirebaseApplicationDto dto, FirebasePrivateDto? privateDto)
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
            InternalComment = privateDto?.InternalComment ?? string.Empty,
            ExternalComment = string.IsNullOrWhiteSpace(dto.ManagerComment) ? dto.OperatorExternalComment : dto.ManagerComment,
            SpecialFlag = string.IsNullOrWhiteSpace(privateDto?.SpecialFlag) ? "Нет" : privateDto!.SpecialFlag,
            AssignedManager = string.IsNullOrWhiteSpace(dto.AssignedManagerName) ? dto.AssignedManagerUid : dto.AssignedManagerName,
            LastModifiedUtc = ParseDate(dto.LastModified, DateTime.UtcNow).ToUniversalTime(),
            LastModifiedBy = dto.ModifiedBy,
            Deleted = dto.IsDeleted
        };
    }

    private static DateTime ParseDate(string? value, DateTime fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        return DateTime.TryParse(value, out var parsed) ? parsed : fallback;
    }
}

public class FirebaseApplicationDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("benefitCategory")]
    public string BenefitCategory { get; set; } = string.Empty;

    [JsonPropertyName("serviceType")]
    public string ServiceType { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("dateCreated")]
    public string DateCreated { get; set; } = string.Empty;

    [JsonPropertyName("documentNumber")]
    public string DocumentNumber { get; set; } = string.Empty;

    [JsonPropertyName("operatorExternalComment")]
    public string OperatorExternalComment { get; set; } = string.Empty;

    [JsonPropertyName("managerComment")]
    public string ManagerComment { get; set; } = string.Empty;

    [JsonPropertyName("assignedManagerUid")]
    public string AssignedManagerUid { get; set; } = string.Empty;

    [JsonPropertyName("assignedManagerName")]
    public string AssignedManagerName { get; set; } = string.Empty;

    [JsonPropertyName("createdByUid")]
    public string CreatedByUid { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("lastModified")]
    public string LastModified { get; set; } = string.Empty;

    [JsonPropertyName("modifiedBy")]
    public string ModifiedBy { get; set; } = "operator";

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }
}

public class FirebasePrivateDto
{
    [JsonPropertyName("internalComment")]
    public string InternalComment { get; set; } = string.Empty;

    [JsonPropertyName("specialFlag")]
    public string SpecialFlag { get; set; } = "Нет";

    [JsonPropertyName("legalApprovalRequired")]
    public bool LegalApprovalRequired { get; set; }

    [JsonPropertyName("waitingDocumentConfirmation")]
    public bool WaitingDocumentConfirmation { get; set; }

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;

    [JsonPropertyName("updatedByUid")]
    public string UpdatedByUid { get; set; } = string.Empty;
}

public class FirebaseAuditLogDto
{
    [JsonPropertyName("level")]
    public string Level { get; set; } = "INFO";

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("applicationId")]
    public string ApplicationId { get; set; } = string.Empty;

    [JsonPropertyName("userUid")]
    public string UserUid { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = "operator";

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;
}

public class FirebaseNotificationDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("applicationId")]
    public string ApplicationId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("targetRole")]
    public string TargetRole { get; set; } = "manager";

    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;
}
