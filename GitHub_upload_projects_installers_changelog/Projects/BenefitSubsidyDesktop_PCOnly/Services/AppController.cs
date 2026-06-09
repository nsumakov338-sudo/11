using System.Collections.ObjectModel;
using BenefitSubsidyDesktop.Models;

namespace BenefitSubsidyDesktop.Services;

public class AppController
{
    public ObservableCollection<ApplicationRequest> Requests { get; } = new();
    public DataStore Store { get; } = new();
    public CloudSyncService SyncService { get; private set; }
    public event EventHandler? DataChanged;

    public AppController()
    {
        SyncService = new CloudSyncService(Store);
        LoadLocal();
    }

    public void LoadLocal()
    {
        Requests.Clear();
        foreach (var item in Store.LoadLocal().Where(x => !x.Deleted).OrderByDescending(x => x.SubmittedAt).ThenBy(x => x.FullName))
            Requests.Add(item);
        OnDataChanged();
    }

    public string GenerateRequestId()
    {
        var prefix = $"ЛГ-{DateTime.Today:yyMMdd}-";
        var maxNumber = Requests
            .Where(x => x.RequestId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(x => int.TryParse(x.RequestId.Replace(prefix, string.Empty), out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"{prefix}{maxNumber + 1:000}";
    }

    public ApplicationRequest CreateNewRequest()
    {
        return new ApplicationRequest
        {
            RequestId = GenerateRequestId(),
            Phone = "+7",
            BenefitCategory = ValidationService.Categories.First(),
            ServiceType = ValidationService.Services.First(),
            Status = "Принято",
            Priority = "Обычный",
            SubmittedAt = DateTime.Today,
            SpecialFlag = "Нет",
            AssignedManager = ValidationService.Managers.FirstOrDefault() ?? "manager",
            LastModifiedBy = "operator",
            LastModifiedUtc = DateTime.UtcNow
        };
    }

    public ApplicationRequest Clone(ApplicationRequest request) => Store.Clone(request);

    public List<string> SaveRequest(ApplicationRequest request)
    {
        var errors = ValidationService.Validate(request);
        if (errors.Count > 0)
        {
            LogService.Warning($"Ошибка валидации заявки {request.RequestId}: {string.Join(" | ", errors)}");
            return errors;
        }

        var isNew = Requests.All(x => x.RequestId != request.RequestId);
        var previous = Requests.FirstOrDefault(x => x.RequestId == request.RequestId);
        var oldStatus = previous?.Status;

        request.LastModifiedUtc = DateTime.UtcNow;
        request.LastModifiedBy = "operator";
        request.Deleted = false;

        if (isNew)
        {
            Requests.Add(Store.Clone(request));
            LogService.Info($"Добавление заявки {request.RequestId}");
        }
        else if (previous != null)
        {
            CopyInto(previous, request);
            LogService.Info($"Редактирование заявки {request.RequestId}");
            if (!string.Equals(oldStatus, request.Status, StringComparison.Ordinal))
                LogService.Info($"Изменение статуса заявки {request.RequestId}: {oldStatus} -> {request.Status}");
        }

        Store.SaveLocal(Requests.Where(x => !x.Deleted));
        Store.Enqueue("UPSERT", request);
        OnDataChanged();
        _ = TrySyncAsync();
        return new List<string>();
    }

    public void DeleteRequest(ApplicationRequest request)
    {
        var target = Requests.FirstOrDefault(x => x.RequestId == request.RequestId);
        if (target == null) return;

        target.Deleted = true;
        target.LastModifiedUtc = DateTime.UtcNow;
        target.LastModifiedBy = "operator";
        Requests.Remove(target);
        Store.SaveLocal(Requests.Where(x => !x.Deleted));
        Store.Enqueue("DELETE", target);
        LogService.Warning($"Удаление заявки {target.RequestId}");
        OnDataChanged();
        _ = TrySyncAsync();
    }

    public async Task<CloudSyncResult> TrySyncAsync()
    {
        var local = Requests.Select(Store.Clone).ToList();
        var result = await SyncService.SyncAsync(local);

        Requests.Clear();
        foreach (var item in local.Where(x => !x.Deleted).OrderByDescending(x => x.SubmittedAt).ThenBy(x => x.FullName))
            Requests.Add(item);

        OnDataChanged();
        return result;
    }

    public async Task<CloudSyncResult> ToggleOnlineAndSyncAsync()
    {
        SyncService.IsOnline = !SyncService.IsOnline;
        if (SyncService.IsOnline)
            return await TrySyncAsync();
        OnDataChanged();
        return new CloudSyncResult { Message = "Сеть отключена. Новые изменения будут сохраняться в локальную очередь." };
    }

    public void ReloadFirebaseSettings()
    {
        var online = SyncService.IsOnline;
        SyncService = new CloudSyncService(Store) { IsOnline = online };
        OnDataChanged();
    }

    public int QueueCount => Store.LoadQueue().Count;
    public bool IsOnline => SyncService.IsOnline;
    public bool IsFirebaseMode => SyncService.IsFirebaseMode;

    public int CountAll => Requests.Count(x => !x.Deleted);
    public int CountChecking => Requests.Count(x => !x.Deleted && x.Status == "На проверке");
    public int CountUrgent => Requests.Count(x => !x.Deleted && x.Priority == "Срочный");
    public int CountApproved => Requests.Count(x => !x.Deleted && x.Status == "Одобрено");
    public int CountDone => Requests.Count(x => !x.Deleted && x.Status == "Выполнено");

    public static void CopyInto(ApplicationRequest target, ApplicationRequest source)
    {
        target.FullName = source.FullName;
        target.Phone = source.Phone;
        target.Address = source.Address;
        target.BenefitCategory = source.BenefitCategory;
        target.ServiceType = source.ServiceType;
        target.Status = source.Status;
        target.Priority = source.Priority;
        target.SubmittedAt = source.SubmittedAt;
        target.DocumentNumber = source.DocumentNumber;
        target.InternalComment = source.InternalComment;
        target.ExternalComment = source.ExternalComment;
        target.SpecialFlag = source.SpecialFlag;
        target.AssignedManager = source.AssignedManager;
        target.LastModifiedUtc = source.LastModifiedUtc;
        target.LastModifiedBy = source.LastModifiedBy;
        target.Deleted = source.Deleted;
    }

    private void OnDataChanged() => DataChanged?.Invoke(this, EventArgs.Empty);
}
