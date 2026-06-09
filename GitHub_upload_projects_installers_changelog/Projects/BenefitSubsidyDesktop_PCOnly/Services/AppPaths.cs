using System.IO;

namespace BenefitSubsidyDesktop.Services;

public static class AppPaths
{
    public static readonly string BaseFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RostelecomBenefitSubsidyDesktop");

    public static readonly string LocalDatabaseFile = Path.Combine(BaseFolder, "applications_local.json");
    public static readonly string SyncQueueFile = Path.Combine(BaseFolder, "sync_queue.json");
    public static readonly string DemoCloudFile = Path.Combine(BaseFolder, "fake_cloud_database.json");
    public static readonly string LogFile = Path.Combine(BaseFolder, "structured_log.txt");

    public static void EnsureFolders()
    {
        Directory.CreateDirectory(BaseFolder);
    }
}
