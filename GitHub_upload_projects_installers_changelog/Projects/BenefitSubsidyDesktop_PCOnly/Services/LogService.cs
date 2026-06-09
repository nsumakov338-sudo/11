using System.IO;

namespace BenefitSubsidyDesktop.Services;

public static class LogService
{
    public static void Info(string message) => Write("INFO", message);
    public static void Warning(string message) => Write("WARNING", message);
    public static void Error(string message, Exception? ex = null)
    {
        var full = ex == null ? message : $"{message}. {ex.GetType().Name}: {ex.Message}";
        Write("ERROR", full);
    }

    private static void Write(string level, string message)
    {
        try
        {
            AppPaths.EnsureFolders();
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff};{level};{message}{Environment.NewLine}";
            File.AppendAllText(AppPaths.LogFile, line);
        }
        catch
        {
            // Логирование не должно останавливать работу приложения.
        }
    }
}
