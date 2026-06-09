using System.Windows;
using BenefitSubsidyDesktop.Models;
using BenefitSubsidyDesktop.Services;

namespace BenefitSubsidyDesktop;

public partial class ApplicationFormWindow : Window
{
    private readonly AppController _controller;
    private readonly bool _isEdit;

    public ApplicationFormWindow(AppController controller, ApplicationRequest? source)
    {
        InitializeComponent();
        _controller = controller;
        _isEdit = source != null;
        InitializeDictionaries();
        FillForm(source == null ? _controller.CreateNewRequest() : _controller.Clone(source));
        txtTitle.Text = _isEdit ? "Редактирование заявки" : "Новая заявка";
    }

    private void InitializeDictionaries()
    {
        cmbCategory.ItemsSource = ValidationService.Categories;
        cmbService.ItemsSource = ValidationService.Services;
        cmbStatus.ItemsSource = ValidationService.Statuses;
        cmbPriority.ItemsSource = ValidationService.Priorities;
        cmbSpecialFlag.ItemsSource = ValidationService.SpecialFlags;
        cmbManager.ItemsSource = ValidationService.Managers;
    }

    private void FillForm(ApplicationRequest request)
    {
        txtRequestId.Text = request.RequestId;
        txtFullName.Text = request.FullName;
        txtPhone.Text = string.IsNullOrWhiteSpace(request.Phone) ? "+7" : request.Phone;
        txtAddress.Text = request.Address;
        cmbCategory.SelectedItem = request.BenefitCategory;
        cmbService.SelectedItem = request.ServiceType;
        cmbStatus.SelectedItem = request.Status;
        cmbPriority.SelectedItem = request.Priority;
        dpSubmittedAt.SelectedDate = request.SubmittedAt;
        txtDocument.Text = request.DocumentNumber;
        txtInternalComment.Text = request.InternalComment;
        txtExternalComment.Text = request.ExternalComment;
        cmbSpecialFlag.SelectedItem = request.SpecialFlag;
        cmbManager.SelectedItem = request.AssignedManager;
    }

    private ApplicationRequest CollectFromForm()
    {
        return new ApplicationRequest
        {
            RequestId = txtRequestId.Text.Trim(),
            FullName = txtFullName.Text.Trim(),
            Phone = txtPhone.Text.Trim(),
            Address = txtAddress.Text.Trim(),
            BenefitCategory = cmbCategory.SelectedItem?.ToString() ?? string.Empty,
            ServiceType = cmbService.SelectedItem?.ToString() ?? string.Empty,
            Status = cmbStatus.SelectedItem?.ToString() ?? string.Empty,
            Priority = cmbPriority.SelectedItem?.ToString() ?? string.Empty,
            SubmittedAt = dpSubmittedAt.SelectedDate?.Date ?? DateTime.Today,
            DocumentNumber = txtDocument.Text.Trim(),
            InternalComment = txtInternalComment.Text.Trim(),
            ExternalComment = txtExternalComment.Text.Trim(),
            SpecialFlag = cmbSpecialFlag.SelectedItem?.ToString() ?? "Нет",
            AssignedManager = cmbManager.SelectedItem?.ToString() ?? "manager",
            LastModifiedBy = "operator",
            LastModifiedUtc = DateTime.UtcNow
        };
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var request = CollectFromForm();
        var errors = _controller.SaveRequest(request);
        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, errors), "Проверьте ввод", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show(_isEdit ? "Заявка обновлена." : "Заявка добавлена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
