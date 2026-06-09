using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using BenefitSubsidyDesktop.Models;
using BenefitSubsidyDesktop.Services;

namespace BenefitSubsidyDesktop;

public partial class ApplicationsWindow : Window
{
    private readonly AppController _controller;
    private readonly ICollectionView _view;
    private bool _initializing = true;

    public ApplicationsWindow(AppController controller)
    {
        InitializeComponent();
        _controller = controller;
        InitializeFilters();
        _view = CollectionViewSource.GetDefaultView(_controller.Requests);
        _view.Filter = FilterRequest;
        dgRequests.ItemsSource = _view;
        _controller.DataChanged += Controller_DataChanged;
        _initializing = false;
        RefreshView();
    }

    private void InitializeFilters()
    {
        FillFilter(cmbFilterCategory, ValidationService.Categories);
        FillFilter(cmbFilterStatus, ValidationService.Statuses);
        FillFilter(cmbFilterService, ValidationService.Services);
        FillFilter(cmbFilterPriority, ValidationService.Priorities);
    }

    private static void FillFilter(ComboBox comboBox, IEnumerable<string> values)
    {
        comboBox.Items.Clear();
        comboBox.Items.Add("Все");
        foreach (var value in values) comboBox.Items.Add(value);
        comboBox.SelectedIndex = 0;
    }

    private bool FilterRequest(object obj)
    {
        if (obj is not ApplicationRequest request || request.Deleted) return false;

        var search = (txtSearch.Text ?? string.Empty).Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var haystack = string.Join(" ", request.RequestId, request.FullName, request.Phone, request.Address,
                request.BenefitCategory, request.ServiceType, request.Status, request.Priority, request.DocumentNumber,
                request.ExternalComment, request.InternalComment, request.SpecialFlag, request.AssignedManager).ToLowerInvariant();
            if (!haystack.Contains(search)) return false;
        }

        if (!FilterComboMatches(cmbFilterCategory, request.BenefitCategory)) return false;
        if (!FilterComboMatches(cmbFilterStatus, request.Status)) return false;
        if (!FilterComboMatches(cmbFilterService, request.ServiceType)) return false;
        if (!FilterComboMatches(cmbFilterPriority, request.Priority)) return false;

        return true;
    }

    private static bool FilterComboMatches(ComboBox comboBox, string value)
    {
        var selected = comboBox.SelectedItem?.ToString();
        return string.IsNullOrWhiteSpace(selected) || selected == "Все" || selected == value;
    }

    private void FilterChanged(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        RefreshView();
    }

    private void RefreshView()
    {
        _view.Refresh();
        txtStatus.Text = $"Найдено: {_view.Cast<object>().Count().ToString(CultureInfo.InvariantCulture)} | Всего в базе: {_controller.CountAll}";
    }

    private void Controller_DataChanged(object? sender, EventArgs e) => RefreshView();

    private ApplicationRequest? SelectedRequest => dgRequests.SelectedItem as ApplicationRequest;

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var window = new ApplicationFormWindow(_controller, null) { Owner = this };
        window.ShowDialog();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRequest == null)
        {
            MessageBox.Show("Сначала выберите заявку в таблице.", "Редактирование", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new ApplicationFormWindow(_controller, SelectedRequest) { Owner = this };
        window.ShowDialog();
    }

    private void Details_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRequest == null)
        {
            MessageBox.Show("Сначала выберите заявку в таблице.", "Подробно", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        new ApplicationDetailsWindow(_controller, SelectedRequest) { Owner = this }.Show();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRequest == null)
        {
            MessageBox.Show("Сначала выберите заявку в таблице.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var answer = MessageBox.Show($"Удалить заявку {SelectedRequest.RequestId}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Yes)
            _controller.DeleteRequest(SelectedRequest);
    }

    private void DgRequests_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SelectedRequest != null)
            new ApplicationDetailsWindow(_controller, SelectedRequest) { Owner = this }.Show();
    }

    private void ResetFilters_Click(object sender, RoutedEventArgs e)
    {
        txtSearch.Text = string.Empty;
        cmbFilterCategory.SelectedIndex = 0;
        cmbFilterStatus.SelectedIndex = 0;
        cmbFilterService.SelectedIndex = 0;
        cmbFilterPriority.SelectedIndex = 0;
        RefreshView();
    }

    private async void Sync_Click(object sender, RoutedEventArgs e)
    {
        var result = await _controller.TrySyncAsync();
        MessageBox.Show(result.Message, "Синхронизация", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _controller.DataChanged -= Controller_DataChanged;
        base.OnClosed(e);
    }
}
