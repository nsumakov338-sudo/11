using BenefitSubsidyMobileAndroid.Models;
using BenefitSubsidyMobileAndroid.Services;

namespace BenefitSubsidyMobileAndroid.Pages;

public partial class DetailPage : ContentPage
{
    private readonly ApplicationRequest _request;
    private readonly SyncService _syncService;
    private readonly Func<Task> _onSaved;
    private readonly List<string> _statuses = new() { "Принято", "На проверке", "Одобрено", "Отказано", "Выполнено" };

    public DetailPage(ApplicationRequest request, SyncService syncService, Func<Task> onSaved)
    {
        InitializeComponent();
        _request = request;
        _syncService = syncService;
        _onSaved = onSaved;
        BindingContext = _request;
        StatusPicker.ItemsSource = _statuses;
        StatusPicker.SelectedItem = _request.Status;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            SetBusy(true, "Сохраняю изменение...");

            var status = StatusPicker.SelectedItem as string ?? string.Empty;
            var comment = ManagerCommentEditor.Text ?? string.Empty;

            if (!_statuses.Contains(status))
                throw new Exception("Выберите статус из справочника.");

            if (comment.Length > 200)
                throw new Exception("Комментарий менеджера не должен превышать 200 символов.");

            _request.Status = status;
            _request.ManagerComment = comment.Trim();

            await _syncService.SaveManagerUpdateAsync(_request);
            StatusLabel.Text = "Изменение сохранено. Если интернет отключён, оно отправится после восстановления связи.";
            await NotificationService.ShowToastAsync("Изменение заявки сохранено");
            await _onSaved();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
            await DisplayAlert("Ошибка сохранения", ex.Message, "ОК");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool isBusy, string message = "")
    {
        BusyIndicator.IsVisible = isBusy;
        BusyIndicator.IsRunning = isBusy;
        StatusLabel.Text = message;
    }
}
