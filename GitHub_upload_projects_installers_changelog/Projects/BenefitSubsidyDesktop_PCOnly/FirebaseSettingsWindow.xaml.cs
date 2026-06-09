using System.Windows;
using BenefitSubsidyDesktop.Services;

namespace BenefitSubsidyDesktop;

public partial class FirebaseSettingsWindow : Window
{
    private readonly FirebaseConfig _config;

    public FirebaseSettingsWindow()
    {
        InitializeComponent();
        _config = FirebaseConfig.Load();
        LoadToForm();
    }

    private void LoadToForm()
    {
        chkUseFirebase.IsChecked = !_config.UseLocalDemoCloud;
        txtDatabaseUrl.Text = _config.FirebaseDatabaseUrl;
        txtApiKey.Text = _config.FirebaseApiKey;
        txtOperatorEmail.Text = string.IsNullOrWhiteSpace(_config.FirebaseOperatorEmail) ? "operator@test.ru" : _config.FirebaseOperatorEmail;
        pwdOperatorPassword.Password = string.IsNullOrWhiteSpace(_config.FirebaseOperatorPassword) ? "123456" : _config.FirebaseOperatorPassword;
        txtManagerUid.Text = _config.FirebaseManagerUid;
        txtManagerName.Text = string.IsNullOrWhiteSpace(_config.FirebaseManagerName) ? "Менеджер Ростелеком" : _config.FirebaseManagerName;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _config.UseLocalDemoCloud = chkUseFirebase.IsChecked != true;
        _config.FirebaseDatabaseUrl = txtDatabaseUrl.Text.Trim();
        _config.FirebaseApiKey = txtApiKey.Text.Trim();
        _config.FirebaseOperatorEmail = txtOperatorEmail.Text.Trim();
        _config.FirebaseOperatorPassword = pwdOperatorPassword.Password;
        _config.FirebaseManagerUid = txtManagerUid.Text.Trim();
        _config.FirebaseManagerName = txtManagerName.Text.Trim();

        if (!_config.UseLocalDemoCloud)
        {
            if (string.IsNullOrWhiteSpace(_config.FirebaseDatabaseUrl) ||
                string.IsNullOrWhiteSpace(_config.FirebaseApiKey) ||
                string.IsNullOrWhiteSpace(_config.FirebaseOperatorEmail) ||
                string.IsNullOrWhiteSpace(_config.FirebaseOperatorPassword))
            {
                MessageBox.Show("Для режима Firebase нужно заполнить URL базы, Web API Key, Email и пароль оператора.", "Не хватает данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        _config.Save();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
