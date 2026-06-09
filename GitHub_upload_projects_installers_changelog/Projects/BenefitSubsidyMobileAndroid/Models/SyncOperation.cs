namespace BenefitSubsidyMobileAndroid.Models;

public class SyncOperation
{
    public string OperationType { get; set; } = "MANAGER_UPDATE";
    public ApplicationRequest Request { get; set; } = new();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
