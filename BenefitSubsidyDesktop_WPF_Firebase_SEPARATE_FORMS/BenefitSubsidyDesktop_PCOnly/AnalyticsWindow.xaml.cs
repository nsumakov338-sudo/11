using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using BenefitSubsidyDesktop.Services;

namespace BenefitSubsidyDesktop;

public partial class AnalyticsWindow : Window
{
    private readonly AppController _controller;
    private bool _initializing = true;

    public AnalyticsWindow(AppController controller)
    {
        InitializeComponent();
        _controller = controller;
        cmbPeriod.ItemsSource = new[] { "День", "Неделя", "Месяц" };
        cmbPeriod.SelectedItem = "Месяц";
        cmbGroup.ItemsSource = new[] { "Категория льготы", "Статус", "Тип услуги" };
        cmbGroup.SelectedItem = "Категория льготы";
        _controller.DataChanged += Controller_DataChanged;
        SizeChanged += (_, _) => DrawAnalytics();
        _initializing = false;
        DrawAnalytics();
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initializing) DrawAnalytics();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => DrawAnalytics();
    private void Controller_DataChanged(object? sender, EventArgs e) => DrawAnalytics();

    private void DrawAnalytics()
    {
        if (analyticsCanvas == null) return;
        analyticsCanvas.Children.Clear();

        var period = cmbPeriod.SelectedItem?.ToString() ?? "Месяц";
        var group = cmbGroup.SelectedItem?.ToString() ?? "Категория льготы";
        var start = period switch
        {
            "День" => DateTime.Today,
            "Неделя" => DateTime.Today.AddDays(-7),
            _ => DateTime.Today.AddMonths(-1)
        };

        var data = _controller.Requests
            .Where(x => !x.Deleted && x.SubmittedAt.Date >= start.Date && x.SubmittedAt.Date <= DateTime.Today)
            .GroupBy(x => group switch
            {
                "Статус" => x.Status,
                "Тип услуги" => x.ServiceType,
                _ => x.BenefitCategory
            })
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        txtSummary.Text = $"Период: {period}. Группировка: {group}. Всего записей на графике: {data.Sum(x => x.Count)}.";

        if (data.Count == 0)
        {
            AddCanvasText("Нет данных за выбранный период", 24, 24, 18, Brushes.Gray, 420);
            return;
        }

        var width = Math.Max(650, analyticsCanvas.ActualWidth > 0 ? analyticsCanvas.ActualWidth : 760);
        var barMaxWidth = width - 260;
        var max = Math.Max(1, data.Max(x => x.Count));
        var y = 28.0;
        var colors = new[] { "#7952FF", "#00AEEF", "#27AE60", "#FFB020", "#E84855", "#68738A" };

        for (var i = 0; i < data.Count; i++)
        {
            var item = data[i];
            var barWidth = Math.Max(28, barMaxWidth * item.Count / max);
            AddCanvasText(item.Name, 18, y + 7, 14, (Brush)FindResource("TextBrush"), 180);
            var rect = new Rectangle
            {
                Width = barWidth,
                Height = 30,
                RadiusX = 10,
                RadiusY = 10,
                Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(colors[i % colors.Length])!
            };
            Canvas.SetLeft(rect, 210);
            Canvas.SetTop(rect, y);
            analyticsCanvas.Children.Add(rect);
            AddCanvasText(item.Count.ToString(CultureInfo.InvariantCulture), 222 + barWidth, y + 5, 14, (Brush)FindResource("TextBrush"), 80);
            y += 50;
        }
    }

    private void AddCanvasText(string text, double left, double top, double fontSize, Brush brush, double width)
    {
        var tb = new TextBlock { Text = text, FontSize = fontSize, Foreground = brush, TextWrapping = TextWrapping.Wrap, Width = width };
        Canvas.SetLeft(tb, left);
        Canvas.SetTop(tb, top);
        analyticsCanvas.Children.Add(tb);
    }

    protected override void OnClosed(EventArgs e)
    {
        _controller.DataChanged -= Controller_DataChanged;
        base.OnClosed(e);
    }
}
