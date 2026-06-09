using System.Windows;
using System.Windows.Controls;
using BenefitSubsidyDesktop.Models;
using BenefitSubsidyDesktop.Services;

namespace BenefitSubsidyDesktop;

public partial class ApplicationDetailsWindow : Window
{
    private readonly AppController _controller;
    private string _requestId;

    public ApplicationDetailsWindow(AppController controller, ApplicationRequest request)
    {
        InitializeComponent();
        _controller = controller;
        _requestId = request.RequestId;
        _controller.DataChanged += Controller_DataChanged;
        RefreshDetails();
    }

    private ApplicationRequest? Current => _controller.Requests.FirstOrDefault(x => x.RequestId == _requestId);

    private void RefreshDetails()
    {
        var request = Current;
        if (request == null)
        {
            txtTitle.Text = "Заявка не найдена";
            txtDetails.Text = "Запись была удалена или ещё не синхронизирована.";
            return;
        }

        txtTitle.Text = $"Заявка {request.RequestId}";
        txtDetails.Text =
            $"Идентификатор: {request.RequestId}\n" +
            $"ФИО заявителя: {request.FullName}\n" +
            $"Контактный телефон: {request.Phone}\n" +
            $"Адрес подключения: {request.Address}\n\n" +
            $"Категория льготы: {request.BenefitCategory}\n" +
            $"Тип услуги: {request.ServiceType}\n" +
            $"Текущий статус: {request.Status}\n" +
            $"Приоритет: {request.Priority}\n" +
            $"Дата подачи: {request.SubmittedAt:dd.MM.yyyy}\n" +
            $"Номер подтверждающего документа: {request.DocumentNumber}\n" +
            $"Назначенный менеджер: {request.AssignedManager}\n\n" +
            $"Внешний комментарий для менеджера:\n{request.ExternalComment}\n\n" +
            $"Внутренний комментарий оператора:\n{request.InternalComment}\n\n" +
            $"Служебная метка, скрытая от менеджера: {request.SpecialFlag}\n" +
            $"Последнее изменение: {request.LastModifiedText}\n" +
            $"Кем изменено: {request.LastModifiedBy}";
    }

    private void Controller_DataChanged(object? sender, EventArgs e) => RefreshDetails();

    private void QuickStatus_Click(object sender, RoutedEventArgs e)
    {
        var request = Current;
        if (request == null) return;
        if (sender is not Button button || button.Content is not string status) return;

        var clone = _controller.Clone(request);
        clone.Status = status;
        var errors = _controller.SaveRequest(clone);
        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, errors), "Проверьте заявку", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show($"Статус изменён на: {status}", "Статус", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        var request = Current;
        if (request == null) return;
        var window = new ApplicationFormWindow(_controller, request) { Owner = this };
        window.ShowDialog();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _controller.DataChanged -= Controller_DataChanged;
        base.OnClosed(e);
    }
}
