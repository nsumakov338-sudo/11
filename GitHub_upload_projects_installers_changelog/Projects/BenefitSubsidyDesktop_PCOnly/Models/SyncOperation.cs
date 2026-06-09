namespace BenefitSubsidyDesktop.Models;

public class SyncOperation
{
    public string OperationType { get; set; } = "UPSERT";
    public ApplicationRequest Request { get; set; } = new();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
