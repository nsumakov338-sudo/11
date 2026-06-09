using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using BenefitSubsidyDesktop.Services;

namespace BenefitSubsidyDesktop;

public partial class MainWindow : Window
{
    private readonly AppController _controller = new();
    private readonly DispatcherTimer _timer = new();

    public MainWindow()
    {
        InitializeComponent();
        _controller.DataChanged += (_, _) => UpdateDashboard();
        ConfigureTimer();
        UpdateDashboard();
    }

    private void ConfigureTimer()
    {
        _timer.Interval = TimeSpan.FromSeconds(Math.Max(5, FirebaseConfig.Load().SyncIntervalSeconds));
        _timer.Tick += async (_, _) => await _controller.TrySyncAsync();
        _timer.Start();
    }

    private void OpenApplications_Click(object sender, RoutedEventArgs e)
    {
        new ApplicationsWindow(_controller) { Owner = this }.Show();
    }

    private void OpenNewApplication_Click(object sender, RoutedEventArgs e)
    {
        var window = new ApplicationFormWindow(_controller, null) { Owner = this };
        window.ShowDialog();
    }

    private void OpenAnalytics_Click(object sender, RoutedEventArgs e)
    {
        new AnalyticsWindow(_controller) { Owner = this }.Show();
    }

    private void OpenExport_Click(object sender, RoutedEventArgs e)
    {
        new ExportWindow(_controller) { Owner = this }.ShowDialog();
    }

    private void OpenLogs_Click(object sender, RoutedEventArgs e)
    {
        new LogsWindow() { Owner = this }.Show();
    }

    private void OpenFirebaseSettings_Click(object sender, RoutedEventArgs e)
    {
        var window = new FirebaseSettingsWindow { Owner = this };
        if (window.ShowDialog() == true)
        {
            _controller.ReloadFirebaseSettings();
            MessageBox.Show("Настройки Firebase сохранены. Нажмите «Синхронизировать», чтобы проверить подключение.", "Firebase", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void ToggleCloud_Click(object sender, RoutedEventArgs e)
    {
        var result = await _controller.ToggleOnlineAndSyncAsync();
        MessageBox.Show(result.Message, "Сеть", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void Sync_Click(object sender, RoutedEventArgs e)
    {
        var result = await _controller.TrySyncAsync();
        MessageBox.Show(result.Message, "Синхронизация", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void UpdateDashboard()
    {
        txtStatAll.Text = _controller.CountAll.ToString(CultureInfo.InvariantCulture);
        txtStatCheck.Text = _controller.CountChecking.ToString(CultureInfo.InvariantCulture);
        txtStatUrgent.Text = _controller.CountUrgent.ToString(CultureInfo.InvariantCulture);
        txtStatApproved.Text = _controller.CountApproved.ToString(CultureInfo.InvariantCulture);
        txtStatQueue.Text = _controller.QueueCount.ToString(CultureInfo.InvariantCulture);
        txtQueueStatus.Text = $"Очередь: {_controller.QueueCount}";

        var mode = _controller.IsFirebaseMode ? "Firebase" : "демо";
        if (_controller.IsOnline)
        {
            ellCloud.Fill = (Brush)FindResource("SuccessBrush");
            txtCloudStatus.Text = $"Онлайн ({mode})";
            btnToggleCloud.Content = "Отключить сеть";
        }
        else
        {
            ellCloud.Fill = (Brush)FindResource("DangerBrush");
            txtCloudStatus.Text = $"Оффлайн ({mode})";
            btnToggleCloud.Content = "Включить сеть";
        }
    }
}
