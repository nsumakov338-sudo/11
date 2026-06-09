using System.Windows;
using BenefitSubsidyDesktop.Services;
using Microsoft.Win32;

namespace BenefitSubsidyDesktop;

public partial class ExportWindow : Window
{
    private readonly AppController _controller;

    public ExportWindow(AppController controller)
    {
        InitializeComponent();
        _controller = controller;
        cmbExportGroup.ItemsSource = new[] { "Статус", "Категория льготы", "Тип услуги" };
        cmbExportGroup.SelectedIndex = 0;
        dpReportFrom.SelectedDate = DateTime.Today.AddDays(-30);
        dpReportTo.SelectedDate = DateTime.Today;
        txtSummary.Text = $"В базе доступно заявлений: {_controller.CountAll}.";
    }

    private string Group => cmbExportGroup.SelectedItem?.ToString() ?? "Статус";

    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить Excel-отчет",
            Filter = "Excel-файл (*.xls)|*.xls",
            FileName = $"Заявления_льготы_{DateTime.Now:yyyyMMdd_HHmm}.xls"
        };
        if (dialog.ShowDialog() == true)
        {
            ExportService.ExportExcelLikeHtml(dialog.FileName, _controller.Requests, Group, "Полный список заявлений");
            MessageBox.Show("Excel-отчет сохранён.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ExportWord_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить Word-отчет",
            Filter = "Word-документ (*.doc)|*.doc",
            FileName = $"Заявления_льготы_{DateTime.Now:yyyyMMdd_HHmm}.doc"
        };
        if (dialog.ShowDialog() == true)
        {
            ExportService.ExportWordLikeHtml(dialog.FileName, _controller.Requests, Group, "Полный список заявлений");
            MessageBox.Show("Word-отчет сохранён.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ExportPeriod_Click(object sender, RoutedEventArgs e)
    {
        var from = dpReportFrom.SelectedDate?.Date ?? DateTime.Today.AddMonths(-1);
        var to = dpReportTo.SelectedDate?.Date ?? DateTime.Today;
        if (from > to)
        {
            MessageBox.Show("Дата начала периода не может быть позже даты окончания.", "Период", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var data = _controller.Requests.Where(x => x.SubmittedAt.Date >= from && x.SubmittedAt.Date <= to).ToList();
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить отчет за период",
            Filter = "Excel-файл (*.xls)|*.xls|Word-документ (*.doc)|*.doc",
            FileName = $"Отчет_за_период_{from:yyyyMMdd}_{to:yyyyMMdd}.xls"
        };
        if (dialog.ShowDialog() == true)
        {
            var title = $"Отчет по заявлениям за период {from:dd.MM.yyyy} — {to:dd.MM.yyyy}";
            if (dialog.FileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase))
                ExportService.ExportWordLikeHtml(dialog.FileName, data, Group, title);
            else
                ExportService.ExportExcelLikeHtml(dialog.FileName, data, Group, title);

            MessageBox.Show("Отчет за период сохранён.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
