using System.Collections.ObjectModel;
using BenefitSubsidyMobileAndroid.Models;
using BenefitSubsidyMobileAndroid.Services;

namespace BenefitSubsidyMobileAndroid.Pages;

public partial class MainPage : ContentPage
{
    private readonly AppSession _session;
    private readonly SyncService _syncService;
    private readonly IDispatcherTimer _timer;
    private List<ApplicationRequest> _all = new();
    private readonly HashSet<string> _notifiedIds = new();
    private bool _isSyncing;

    public ObservableCollection<ApplicationRequest> Applications { get; } = new();

    public MainPage(AppSession session)
    {
        InitializeComponent();
        BindingContext = this;
        _session = session;
        _syncService = new SyncService(session);

        InitPickers();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(8);
        _timer.Tick += async (_, _) => await SyncNowAsync(false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _all = _syncService.LoadLocal();
        ApplyFilters();
        UpdateStats();
        await SyncNowAsync(true);
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer.Stop();
    }

    private void InitPickers()
    {
        CategoryPicker.ItemsSource = new List<string> { "Все", "Пенсионер", "Инвалид", "Многодетная семья", "Ветеран", "Малоимущий" };
        StatusPicker.ItemsSource = new List<string> { "Все", "Принято", "На проверке", "Одобрено", "Отказано", "Выполнено" };
        ServiceTypePicker.ItemsSource = new List<string> { "Все", "Интернет", "ТВ", "Телефония", "Комплексный пакет" };
        SortPicker.ItemsSource = new List<string> { "Приоритет и дата", "Сначала новые", "Сначала старые", "По статусу" };
        CategoryPicker.SelectedIndex = 0;
        StatusPicker.SelectedIndex = 0;
        ServiceTypePicker.SelectedIndex = 0;
        SortPicker.SelectedIndex = 0;
    }

    private async Task SyncNowAsync(bool showErrors)
    {
        if (_isSyncing) return;
        _isSyncing = true;

        try
        {
            ConnectionLabel.Text = "Синхронизация с Firebase...";
            var result = await _syncService.SyncAsync(_all);
            _all = result.Applications;
            ApplyFilters();
            UpdateStats();
            ConnectionLabel.Text = result.IsOnline
                ? result.Message
                : result.Message + $" Очередь: {result.QueueCount}.";

            await ShowImportantToastsAsync();
        }
        catch (Exception ex)
        {
            ConnectionLabel.Text = "Ошибка синхронизации: " + ex.Message;
            if (showErrors)
                await DisplayAlert("Ошибка Firebase", ex.Message, "ОК");
        }
        finally
        {
            _isSyncing = false;
            RefreshView.IsRefreshing = false;
        }
    }

    private void ApplyFilters()
    {
        var query = (SearchBar.Text ?? string.Empty).Trim().ToLowerInvariant();
        var category = CategoryPicker.SelectedItem as string ?? "Все";
        var status = StatusPicker.SelectedItem as string ?? "Все";
        var serviceType = ServiceTypePicker.SelectedItem as string ?? "Все";
        var sort = SortPicker.SelectedItem as string ?? "Приоритет и дата";

        IEnumerable<ApplicationRequest> data = _all.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            data = data.Where(x =>
                x.FullName.ToLowerInvariant().Contains(query) ||
                x.Address.ToLowerInvariant().Contains(query) ||
                x.VisibleComment.ToLowerInvariant().Contains(query));
        }

        if (category != "Все") data = data.Where(x => x.BenefitCategory == category);
        if (status != "Все") data = data.Where(x => x.Status == status);
        if (serviceType != "Все") data = data.Where(x => x.ServiceType == serviceType);

        data = sort switch
        {
            "Сначала новые" => data.OrderByDescending(x => x.SubmittedAt),
            "Сначала старые" => data.OrderBy(x => x.SubmittedAt),
            "По статусу" => data.OrderBy(x => x.Status).ThenByDescending(x => x.SubmittedAt),
            _ => data.OrderByDescending(x => x.Priority == "Срочный").ThenByDescending(x => x.SubmittedAt)
        };

        Applications.Clear();
        foreach (var item in data)
            Applications.Add(item);
    }

    private void UpdateStats()
    {
        TotalLabel.Text = _all.Count(x => !x.IsDeleted).ToString();
        ApprovedLabel.Text = _all.Count(x => x.Status == "Одобрено" || x.Status == "Выполнено").ToString();
        RejectedLabel.Text = _all.Count(x => x.Status == "Отказано").ToString();
    }

    private async Task ShowImportantToastsAsync()
    {
        var important = _all.Where(x => x.IsUrgentOrImportant)
                            .OrderByDescending(x => x.LastModifiedUtc)
                            .Take(5)
                            .ToList();

        foreach (var item in important)
        {
            if (_notifiedIds.Contains(item.RequestId)) continue;
            _notifiedIds.Add(item.RequestId);
            await NotificationService.ShowToastAsync($"Важная заявка: {item.RequestId} — {item.Priority}, {item.BenefitCategory}");
        }
    }

    private void OnFilterChanged(object sender, EventArgs e)
    {
        ApplyFilters();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await SyncNowAsync(true);
    }

    private async void OnPullToRefresh(object sender, EventArgs e)
    {
        await SyncNowAsync(true);
    }

    private async void OnApplicationTapped(object sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not ApplicationRequest request)
            return;

        await Navigation.PushAsync(new DetailPage(request.Clone(), _syncService, async () =>
        {
            await SyncNowAsync(false);
        }));
    }

    protected override bool OnBackButtonPressed()
    {
        Dispatcher.Dispatch(async () =>
        {
            var exit = await DisplayAlert("Выход", "Выйти из кабинета менеджера?", "Да", "Нет");
            if (exit)
            {
                await AppSessionService.LogoutAsync();
                await Navigation.PopToRootAsync();
            }
        });
        return true;
    }
}
