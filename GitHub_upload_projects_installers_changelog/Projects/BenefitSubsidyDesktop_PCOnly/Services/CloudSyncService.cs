using BenefitSubsidyDesktop.Models;

namespace BenefitSubsidyDesktop.Services;

public class CloudSyncResult
{
    public int SentCount { get; set; }
    public int PulledCount { get; set; }
    public int ConflictCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CloudSyncService
{
    private readonly DataStore _store;
    private readonly FirebaseConfig _config;
    private readonly FirebaseAuthService _authService = new();
    private FirebaseDatabaseService? _databaseService;
    private string _idToken = string.Empty;
    private string _operatorUid = string.Empty;

    public bool IsOnline { get; set; } = true;
    public bool IsFirebaseMode => _config.IsFirebaseReady;

    public CloudSyncService(DataStore store)
    {
        _store = store;
        _config = FirebaseConfig.Load();

        if (_config.IsFirebaseReady)
            _databaseService = new FirebaseDatabaseService(_config.FirebaseDatabaseUrl, _config);
    }

    public async Task<CloudSyncResult> SyncAsync(List<ApplicationRequest> local)
    {
        if (_config.UseLocalDemoCloud)
            return await Task.Run(() => SyncDemoInternal(local));

        if (!_config.IsFirebaseReady || _databaseService == null)
        {
            return new CloudSyncResult
            {
                Message = "Firebase не настроен. Откройте «Настройки Firebase», заполните URL базы, Web API Key, Email и пароль оператора."
            };
        }

        return await SyncFirebaseAsync(local);
    }

    private async Task<CloudSyncResult> SyncFirebaseAsync(List<ApplicationRequest> local)
    {
        var result = new CloudSyncResult();

        if (!IsOnline)
        {
            result.Message = "Сеть отключена. Изменения сохранены в локальной очереди.";
            return result;
        }

        try
        {
            await EnsureAuthAsync();

            var cloud = await _databaseService!.GetApplicationsAsync(_idToken);
            var cloudMap = cloud.ToDictionary(x => x.RequestId, x => x);
            var queue = _store.LoadQueue();

            foreach (var op in queue.OrderBy(x => x.CreatedUtc))
            {
                var request = op.Request;

                if (op.OperationType == "DELETE")
                {
                    if (!cloudMap.TryGetValue(request.RequestId, out var cloudExisting) || request.LastModifiedUtc >= cloudExisting.LastModifiedUtc)
                    {
                        await _databaseService.DeleteApplicationAsync(request, _idToken, _operatorUid);
                        cloudMap.Remove(request.RequestId);
                        result.SentCount++;
                    }
                    else
                    {
                        result.ConflictCount++;
                    }

                    continue;
                }

                if (cloudMap.TryGetValue(request.RequestId, out var currentCloud))
                {
                    if (request.LastModifiedUtc >= currentCloud.LastModifiedUtc)
                    {
                        await _databaseService.SaveApplicationAsync(request, _idToken, _operatorUid);
                        cloudMap[request.RequestId] = _store.Clone(request);
                        result.SentCount++;
                    }
                    else
                    {
                        result.ConflictCount++;
                    }
                }
                else
                {
                    await _databaseService.SaveApplicationAsync(request, _idToken, _operatorUid);
                    cloudMap[request.RequestId] = _store.Clone(request);
                    result.SentCount++;
                }
            }

            _store.SaveQueue(Array.Empty<SyncOperation>());

            var freshCloud = await _databaseService.GetApplicationsAsync(_idToken);
            MergeCloudIntoLocal(local, freshCloud, result);

            _store.SaveLocal(local.Where(x => !x.Deleted).OrderByDescending(x => x.SubmittedAt));

            result.Message = result.ConflictCount > 0
                ? $"Синхронизация с Firebase выполнена. Конфликты LWW: {result.ConflictCount}. Использована самая новая версия. Отправлено: {result.SentCount}, получено: {result.PulledCount}."
                : $"Синхронизация с Firebase выполнена успешно. Отправлено: {result.SentCount}, получено: {result.PulledCount}.";

            LogService.Info($"Firebase sync: отправлено={result.SentCount}, получено={result.PulledCount}, конфликтов={result.ConflictCount}");
            return result;
        }
        catch (Exception ex)
        {
            LogService.Error("Ошибка Firebase-синхронизации", ex);
            result.Message = "Ошибка Firebase-синхронизации. Проверьте URL базы, Web API Key, Email/Password, UID в users и Rules. Изменения останутся в локальной очереди.";
            return result;
        }
    }

    private async Task EnsureAuthAsync()
    {
        if (!string.IsNullOrWhiteSpace(_idToken) && !string.IsNullOrWhiteSpace(_operatorUid))
            return;

        var authResult = await _authService.SignInAsync(
            _config.FirebaseApiKey,
            _config.FirebaseOperatorEmail,
            _config.FirebaseOperatorPassword);

        _idToken = authResult.IdToken;
        _operatorUid = authResult.LocalId;
        LogService.Info($"Авторизация Firebase выполнена: {authResult.Email}, UID={authResult.LocalId}");
    }

    private CloudSyncResult SyncDemoInternal(List<ApplicationRequest> local)
    {
        var result = new CloudSyncResult();

        if (!IsOnline)
        {
            result.Message = "Сеть отключена. Изменения сохранены в локальной очереди.";
            return result;
        }

        try
        {
            var cloud = _store.LoadCloudDemo();
            var cloudMap = cloud.ToDictionary(x => x.RequestId, x => x);
            var queue = _store.LoadQueue();

            foreach (var op in queue.OrderBy(x => x.CreatedUtc))
            {
                var request = op.Request;

                if (op.OperationType == "DELETE")
                {
                    if (cloudMap.TryGetValue(request.RequestId, out var existing))
                    {
                        if (request.LastModifiedUtc >= existing.LastModifiedUtc)
                        {
                            cloudMap.Remove(request.RequestId);
                            result.SentCount++;
                        }
                        else
                        {
                            result.ConflictCount++;
                        }
                    }
                    continue;
                }

                if (cloudMap.TryGetValue(request.RequestId, out var currentCloud))
                {
                    if (request.LastModifiedUtc >= currentCloud.LastModifiedUtc)
                    {
                        cloudMap[request.RequestId] = request;
                        result.SentCount++;
                    }
                    else
                    {
                        result.ConflictCount++;
                    }
                }
                else
                {
                    cloudMap[request.RequestId] = request;
                    result.SentCount++;
                }
            }

            _store.SaveCloudDemo(cloudMap.Values.OrderByDescending(x => x.SubmittedAt));
            _store.SaveQueue(Array.Empty<SyncOperation>());

            MergeCloudIntoLocal(local, cloudMap.Values.ToList(), result);

            _store.SaveLocal(local.OrderByDescending(x => x.SubmittedAt));
            result.Message = result.ConflictCount > 0
                ? $"Демо-синхронизация выполнена. Конфликты LWW: {result.ConflictCount}."
                : "Демо-синхронизация выполнена успешно.";
            LogService.Info($"Demo sync: отправлено={result.SentCount}, получено={result.PulledCount}, конфликтов={result.ConflictCount}");
            return result;
        }
        catch (Exception ex)
        {
            LogService.Error("Ошибка демонстрационной синхронизации", ex);
            result.Message = "Ошибка демонстрационной синхронизации. Изменения останутся в очереди.";
            return result;
        }
    }

    private static void MergeCloudIntoLocal(List<ApplicationRequest> local, List<ApplicationRequest> cloud, CloudSyncResult result)
    {
        var localMap = local.ToDictionary(x => x.RequestId, x => x);

        foreach (var cloudItem in cloud.Where(x => !x.Deleted))
        {
            if (!localMap.TryGetValue(cloudItem.RequestId, out var localItem))
            {
                local.Add(cloudItem);
                result.PulledCount++;
                continue;
            }

            if (cloudItem.LastModifiedUtc > localItem.LastModifiedUtc)
            {
                CopyInto(localItem, cloudItem);
                result.PulledCount++;
            }
        }
    }

    private static void CopyInto(ApplicationRequest target, ApplicationRequest source)
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
}
