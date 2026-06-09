namespace BenefitSubsidyMobileAndroid;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // Используются системные шрифты Android, внешние файлы шрифтов не нужны.
            });

        return builder.Build();
    }
}
