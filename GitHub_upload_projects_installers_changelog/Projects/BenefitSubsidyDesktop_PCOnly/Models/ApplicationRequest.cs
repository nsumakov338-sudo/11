namespace BenefitSubsidyDesktop.Models;

public class ApplicationRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BenefitCategory { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.Today;
    public string DocumentNumber { get; set; } = string.Empty;
    public string InternalComment { get; set; } = string.Empty;
    public string ExternalComment { get; set; } = string.Empty;
    public string SpecialFlag { get; set; } = "Нет";
    public string AssignedManager { get; set; } = "manager";
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
    public string LastModifiedBy { get; set; } = "operator";
    public bool Deleted { get; set; }

    public string SubmittedDateText => SubmittedAt.ToString("dd.MM.yyyy");
    public string LastModifiedText => LastModifiedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
}
