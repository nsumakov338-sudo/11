using BenefitSubsidyMobileAndroid.Pages;

namespace BenefitSubsidyMobileAndroid;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new NavigationPage(new LoginPage())
        {
            BarBackgroundColor = Color.FromArgb("#0B5FFF"),
            BarTextColor = Colors.White
        };
    }
}
