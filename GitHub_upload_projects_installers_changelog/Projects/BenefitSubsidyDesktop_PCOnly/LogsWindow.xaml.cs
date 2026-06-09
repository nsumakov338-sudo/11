using System.Diagnostics;
using System.IO;
using System.Windows;
using BenefitSubsidyDesktop.Services;

namespace BenefitSubsidyDesktop;

public partial class LogsWindow : Window
{
    public LogsWindow()
    {
        InitializeComponent();
        RefreshLog();
    }

    private void RefreshLog()
    {
        AppPaths.EnsureFolders();
        if (!File.Exists(AppPaths.LogFile)) File.WriteAllText(AppPaths.LogFile, string.Empty);
        txtLog.Text = File.ReadAllText(AppPaths.LogFile);
        txtPath.Text = AppPaths.LogFile;
        txtLog.ScrollToEnd();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshLog();

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        AppPaths.EnsureFolders();
        Process.Start(new ProcessStartInfo(AppPaths.BaseFolder) { UseShellExecute = true });
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
