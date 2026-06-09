using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BenefitSubsidyMobileAndroid.Models;

public class ApplicationRequest : INotifyPropertyChanged
{
    private string _status = string.Empty;
    private string _managerComment = string.Empty;

    public string RequestId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BenefitCategory { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public string Priority { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.Today;
    public string DocumentNumber { get; set; } = string.Empty;
    public string OperatorExternalComment { get; set; } = string.Empty;

    public string ManagerComment
    {
        get => _managerComment;
        set => SetField(ref _managerComment, value);
    }

    public string AssignedManagerUid { get; set; } = string.Empty;
    public string AssignedManagerName { get; set; } = string.Empty;
    public string CreatedByUid { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
    public string ModifiedBy { get; set; } = "operator";
    public bool IsDeleted { get; set; }

    public string SubmittedDateText => SubmittedAt.ToString("dd.MM.yyyy");
    public string LastModifiedText => LastModifiedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
    public string VisibleComment => string.IsNullOrWhiteSpace(ManagerComment) ? OperatorExternalComment : ManagerComment;
    public bool IsUrgentOrImportant => Priority == "Срочный" || BenefitCategory == "Инвалид" || BenefitCategory == "Многодетная семья";

    public ApplicationRequest Clone()
    {
        return new ApplicationRequest
        {
            RequestId = RequestId,
            FullName = FullName,
            Phone = Phone,
            Address = Address,
            BenefitCategory = BenefitCategory,
            ServiceType = ServiceType,
            Status = Status,
            Priority = Priority,
            SubmittedAt = SubmittedAt,
            DocumentNumber = DocumentNumber,
            OperatorExternalComment = OperatorExternalComment,
            ManagerComment = ManagerComment,
            AssignedManagerUid = AssignedManagerUid,
            AssignedManagerName = AssignedManagerName,
            CreatedByUid = CreatedByUid,
            CreatedAt = CreatedAt,
            LastModifiedUtc = LastModifiedUtc,
            ModifiedBy = ModifiedBy,
            IsDeleted = IsDeleted
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
