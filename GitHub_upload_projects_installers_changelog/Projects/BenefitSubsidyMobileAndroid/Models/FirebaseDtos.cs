using System.Text.Json.Serialization;

namespace BenefitSubsidyMobileAndroid.Models;

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
    public string Role { get; set; } = "manager";

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;
}

public class FirebaseNotificationDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "manager_update";

    [JsonPropertyName("applicationId")]
    public string ApplicationId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("targetRole")]
    public string TargetRole { get; set; } = "operator";

    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;
}

public class FirebaseManagerStatsDto
{
    [JsonPropertyName("processed")]
    public int Processed { get; set; }

    [JsonPropertyName("approved")]
    public int Approved { get; set; }

    [JsonPropertyName("rejected")]
    public int Rejected { get; set; }

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;
}
