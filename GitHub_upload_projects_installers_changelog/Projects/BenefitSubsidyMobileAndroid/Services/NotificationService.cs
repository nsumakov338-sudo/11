namespace BenefitSubsidyMobileAndroid.Services;

public static class NotificationService
{
    public static Task ShowToastAsync(string text)
    {
#if ANDROID
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Android.Widget.Toast.MakeText(Android.App.Application.Context, text, Android.Widget.ToastLength.Long)?.Show();
        });
#else
        MainThread.BeginInvokeOnMainThread(async () => await Application.Current!.MainPage!.DisplayAlert("Уведомление", text, "ОК"));
#endif
        return Task.CompletedTask;
    }
}
