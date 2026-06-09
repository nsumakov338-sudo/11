using System.IO;
using System.Text.Json;
using BenefitSubsidyDesktop.Models;

namespace BenefitSubsidyDesktop.Services;

public class DataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public DataStore()
    {
        AppPaths.EnsureFolders();
    }

    public List<ApplicationRequest> LoadLocal()
    {
        try
        {
            if (!File.Exists(AppPaths.LocalDatabaseFile))
                return CreateSeedData();

            var json = File.ReadAllText(AppPaths.LocalDatabaseFile);
            var data = JsonSerializer.Deserialize<List<ApplicationRequest>>(json, JsonOptions) ?? new List<ApplicationRequest>();
            return data.Where(x => !x.Deleted).ToList();
        }
        catch (Exception ex)
        {
            LogService.Error("Ошибка чтения локальной базы", ex);
            return CreateSeedData();
        }
    }

    public void SaveLocal(IEnumerable<ApplicationRequest> requests)
    {
        try
        {
            var list = requests.ToList();
            var json = JsonSerializer.Serialize(list, JsonOptions);
            File.WriteAllText(AppPaths.LocalDatabaseFile, json);
        }
        catch (Exception ex)
        {
            LogService.Error("Ошибка сохранения локальной базы", ex);
        }
    }

    public List<SyncOperation> LoadQueue()
    {
        try
        {
            if (!File.Exists(AppPaths.SyncQueueFile)) return new List<SyncOperation>();
            var json = File.ReadAllText(AppPaths.SyncQueueFile);
            return JsonSerializer.Deserialize<List<SyncOperation>>(json, JsonOptions) ?? new List<SyncOperation>();
        }
        catch (Exception ex)
        {
            LogService.Error("Ошибка чтения очереди синхронизации", ex);
            return new List<SyncOperation>();
        }
    }

    public void SaveQueue(IEnumerable<SyncOperation> queue)
    {
        try
        {
            File.WriteAllText(AppPaths.SyncQueueFile, JsonSerializer.Serialize(queue.ToList(), JsonOptions));
        }
        catch (Exception ex)
        {
            LogService.Error("Ошибка сохранения очереди синхронизации", ex);
        }
    }

    public void Enqueue(string operationType, ApplicationRequest request)
    {
        var queue = LoadQueue();
        queue.Add(new SyncOperation
        {
            OperationType = operationType,
            Request = Clone(request),
            CreatedUtc = DateTime.UtcNow
        });
        SaveQueue(queue);
    }

    public List<ApplicationRequest> LoadCloudDemo()
    {
        try
        {
            if (!File.Exists(AppPaths.DemoCloudFile)) return new List<ApplicationRequest>();
            var json = File.ReadAllText(AppPaths.DemoCloudFile);
            return JsonSerializer.Deserialize<List<ApplicationRequest>>(json, JsonOptions) ?? new List<ApplicationRequest>();
        }
        catch (Exception ex)
        {
            LogService.Error("Ошибка чтения демонстрационной облачной базы", ex);
            return new List<ApplicationRequest>();
        }
    }

    public void SaveCloudDemo(IEnumerable<ApplicationRequest> cloud)
    {
        try
        {
            File.WriteAllText(AppPaths.DemoCloudFile, JsonSerializer.Serialize(cloud.ToList(), JsonOptions));
        }
        catch (Exception ex)
        {
            LogService.Error("Ошибка сохранения демонстрационной облачной базы", ex);
        }
    }

    public ApplicationRequest Clone(ApplicationRequest request)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        return JsonSerializer.Deserialize<ApplicationRequest>(json, JsonOptions) ?? new ApplicationRequest();
    }

    private List<ApplicationRequest> CreateSeedData()
    {
        var today = DateTime.Today;
        var data = new List<ApplicationRequest>
        {
            new()
            {
                RequestId = $"ЛГ-{today:yyMMdd}-001",
                FullName = "Иванов Сергей Петрович",
                Phone = "+79001234567",
                Address = "г. Москва, ул. Ленина, д. 10, кв. 45",
                BenefitCategory = "Пенсионер",
                ServiceType = "Интернет",
                Status = "Принято",
                Priority = "Обычный",
                SubmittedAt = today,
                DocumentNumber = "ПЕН-45871",
                InternalComment = "Проверить корректность пенсионного удостоверения.",
                ExternalComment = "Клиент ожидает звонка до 18:00",
                SpecialFlag = "Ожидает подтверждения документа",
                AssignedManager = "manager",
                LastModifiedUtc = DateTime.UtcNow.AddMinutes(-25)
            },
            new()
            {
                RequestId = $"ЛГ-{today:yyMMdd}-002",
                FullName = "Смирнова Анна Викторовна",
                Phone = "+79161234567",
                Address = "г. Тула, пр-т Победы, д. 4",
                BenefitCategory = "Инвалид",
                ServiceType = "Комплексный пакет",
                Status = "На проверке",
                Priority = "Срочный",
                SubmittedAt = today.AddDays(-1),
                DocumentNumber = "МСЭ-2026-11",
                InternalComment = "Срочная заявка, проверить пакет документов в первую очередь.",
                ExternalComment = "Требуется копия справки МСЭ",
                SpecialFlag = "Требует согласования с юридическим отделом",
                AssignedManager = "manager",
                LastModifiedUtc = DateTime.UtcNow.AddMinutes(-15)
            },
            new()
            {
                RequestId = $"ЛГ-{today:yyMMdd}-003",
                FullName = "Кузнецова Мария Олеговна",
                Phone = "+79261234567",
                Address = "г. Рязань, ул. Садовая, д. 12",
                BenefitCategory = "Многодетная семья",
                ServiceType = "ТВ",
                Status = "Одобрено",
                Priority = "Срочный",
                SubmittedAt = today.AddDays(-3),
                DocumentNumber = "МДС-778899",
                InternalComment = "Документы подтверждены.",
                ExternalComment = "Документы проверены, заявка одобрена",
                SpecialFlag = "Нет",
                AssignedManager = "manager_petrov",
                LastModifiedUtc = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        SaveLocal(data);
        SaveCloudDemo(data);
        LogService.Info("Создана стартовая локальная база с демо-записями");
        return data;
    }
}
