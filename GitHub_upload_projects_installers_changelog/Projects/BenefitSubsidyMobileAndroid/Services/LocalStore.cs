using System.Text.Json;
using BenefitSubsidyMobileAndroid.Models;

namespace BenefitSubsidyMobileAndroid.Services;

public class LocalStore
{
    private readonly string _cachePath = Path.Combine(FileSystem.AppDataDirectory, "applications_cache.json");
    private readonly string _queuePath = Path.Combine(FileSystem.AppDataDirectory, "sync_queue.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public List<ApplicationRequest> LoadApplications()
    {
        try
        {
            if (!File.Exists(_cachePath)) return new List<ApplicationRequest>();
            var json = File.ReadAllText(_cachePath);
            return JsonSerializer.Deserialize<List<ApplicationRequest>>(json, JsonOptions) ?? new List<ApplicationRequest>();
        }
        catch
        {
            return new List<ApplicationRequest>();
        }
    }

    public void SaveApplications(IEnumerable<ApplicationRequest> applications)
    {
        var json = JsonSerializer.Serialize(applications.Where(x => !x.IsDeleted).ToList(), JsonOptions);
        File.WriteAllText(_cachePath, json);
    }

    public List<SyncOperation> LoadQueue()
    {
        try
        {
            if (!File.Exists(_queuePath)) return new List<SyncOperation>();
            var json = File.ReadAllText(_queuePath);
            return JsonSerializer.Deserialize<List<SyncOperation>>(json, JsonOptions) ?? new List<SyncOperation>();
        }
        catch
        {
            return new List<SyncOperation>();
        }
    }

    public void SaveQueue(IEnumerable<SyncOperation> queue)
    {
        var json = JsonSerializer.Serialize(queue.ToList(), JsonOptions);
        File.WriteAllText(_queuePath, json);
    }

    public void AddToQueue(ApplicationRequest request)
    {
        var queue = LoadQueue();
        queue.RemoveAll(x => x.Request.RequestId == request.RequestId);
        queue.Add(new SyncOperation
        {
            OperationType = "MANAGER_UPDATE",
            Request = request.Clone(),
            CreatedUtc = DateTime.UtcNow
        });
        SaveQueue(queue);
    }

    public int QueueCount => LoadQueue().Count;
}
