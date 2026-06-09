using BenefitSubsidyMobileAndroid.Services;

namespace BenefitSubsidyMobileAndroid.Pages;

public partial class LoginPage : ContentPage
{
    private bool _autoLoginTried;

    public LoginPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        DatabaseUrlEntry.Text = AppSessionService.SavedDatabaseUrl;
        ApiKeyEntry.Text = AppSessionService.SavedApiKey;
        EmailEntry.Text = AppSessionService.SavedEmail;

        if (_autoLoginTried) return;
        _autoLoginTried = true;

        try
        {
            SetBusy(true, "Проверяю сохранённую сессию...");
            var session = await AppSessionService.TryRestoreSessionAsync();
            if (session != null)
            {
                await Navigation.PushAsync(new MainPage(session));
            }
        }
        catch
        {
            StatusLabel.Text = "Автовход не выполнен. Введите данные Firebase и пароль менеджера.";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            SetBusy(true, "Выполняю вход через Firebase Authentication...");
            var session = await AppSessionService.LoginAsync(
                DatabaseUrlEntry.Text ?? string.Empty,
                ApiKeyEntry.Text ?? string.Empty,
                EmailEntry.Text ?? string.Empty,
                PasswordEntry.Text ?? string.Empty);

            PasswordEntry.Text = string.Empty;
            await Navigation.PushAsync(new MainPage(session));
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
            await DisplayAlert("Не удалось войти", ex.Message, "ОК");
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
