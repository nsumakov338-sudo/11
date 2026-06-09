using BenefitSubsidyMobileAndroid.Models;
using Microsoft.Maui.Networking;

namespace BenefitSubsidyMobileAndroid.Services;

public class SyncResult
{
    public List<ApplicationRequest> Applications { get; set; } = new();
    public int SentCount { get; set; }
    public int PulledCount { get; set; }
    public int QueueCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
}

public class SyncService
{
    private readonly AppSession _session;
    private readonly LocalStore _store = new();
    private readonly FirebaseDatabaseService _database;

    public SyncService(AppSession session)
    {
        _session = session;
        _database = new FirebaseDatabaseService(session.DatabaseUrl);
    }

    public List<ApplicationRequest> LoadLocal() => _store.LoadApplications();

    public async Task<SyncResult> SyncAsync(List<ApplicationRequest>? currentLocal = null)
    {
        AppSessionService.Touch();

        var result = new SyncResult
        {
            IsOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet
        };

        var local = currentLocal ?? _store.LoadApplications();

        if (!result.IsOnline)
        {
            result.Applications = local;
            result.QueueCount = _store.QueueCount;
            result.Message = "Оффлайн: отображаются данные из локального кэша.";
            return result;
        }

        var queue = _store.LoadQueue();
        foreach (var op in queue.OrderBy(x => x.CreatedUtc))
        {
            await _database.UpdateApplicationAsManagerAsync(op.Request, _session.IdToken, _session.Uid);
            result.SentCount++;
        }
        _store.SaveQueue(Array.Empty<SyncOperation>());

        var cloud = await _database.GetApplicationsAsync(_session.IdToken, _session.Uid, _session.Email);
        var cloudMap = cloud.ToDictionary(x => x.RequestId, x => x);
        var localMap = local.ToDictionary(x => x.RequestId, x => x);

        foreach (var cloudItem in cloudMap.Values)
        {
            if (!localMap.TryGetValue(cloudItem.RequestId, out var localItem))
            {
                local.Add(cloudItem.Clone());
                result.PulledCount++;
                continue;
            }

            if (cloudItem.LastModifiedUtc >= localItem.LastModifiedUtc)
            {
                CopyInto(localItem, cloudItem);
                result.PulledCount++;
            }
        }

        local.RemoveAll(x => !cloudMap.ContainsKey(x.RequestId) || x.IsDeleted);
        _store.SaveApplications(local);

        result.Applications = local.OrderByDescending(x => x.Priority == "Срочный")
                                   .ThenByDescending(x => x.SubmittedAt)
                                   .ToList();
        result.QueueCount = 0;
        result.Message = $"Синхронизация выполнена. Получено: {result.PulledCount}, отправлено: {result.SentCount}.";
        return result;
    }

    public async Task SaveManagerUpdateAsync(ApplicationRequest request)
    {
        AppSessionService.Touch();

        request.LastModifiedUtc = DateTime.UtcNow;
        request.ModifiedBy = "manager";

        var local = _store.LoadApplications();
        var existing = local.FirstOrDefault(x => x.RequestId == request.RequestId);
        if (existing == null)
        {
            local.Add(request.Clone());
        }
        else
        {
            CopyInto(existing, request);
        }
        _store.SaveApplications(local);

        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            try
            {
                await _database.UpdateApplicationAsManagerAsync(request, _session.IdToken, _session.Uid);
                return;
            }
            catch
            {
                // При временной ошибке сети изменение не теряется, а уходит в очередь.
            }
        }

        _store.AddToQueue(request);
    }

    private static void CopyInto(ApplicationRequest target, ApplicationRequest source)
    {
        target.RequestId = source.RequestId;
        target.FullName = source.FullName;
        target.Phone = source.Phone;
        target.Address = source.Address;
        target.BenefitCategory = source.BenefitCategory;
        target.ServiceType = source.ServiceType;
        target.Status = source.Status;
        target.Priority = source.Priority;
        target.SubmittedAt = source.SubmittedAt;
        target.DocumentNumber = source.DocumentNumber;
        target.OperatorExternalComment = source.OperatorExternalComment;
        target.ManagerComment = source.ManagerComment;
        target.AssignedManagerUid = source.AssignedManagerUid;
        target.AssignedManagerName = source.AssignedManagerName;
        target.CreatedByUid = source.CreatedByUid;
        target.CreatedAt = source.CreatedAt;
        target.LastModifiedUtc = source.LastModifiedUtc;
        target.ModifiedBy = source.ModifiedBy;
        target.IsDeleted = source.IsDeleted;
    }
}
