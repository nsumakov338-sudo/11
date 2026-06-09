using System.IO;
using System.Net;
using System.Text;
using BenefitSubsidyDesktop.Models;

namespace BenefitSubsidyDesktop.Services;

public static class ExportService
{
    public static void ExportExcelLikeHtml(string path, IEnumerable<ApplicationRequest> requests, string groupBy, string title)
    {
        var html = BuildHtml(requests, groupBy, title, true);
        File.WriteAllText(path, html, Encoding.UTF8);
        LogService.Info($"Экспорт Excel: {path}");
    }

    public static void ExportWordLikeHtml(string path, IEnumerable<ApplicationRequest> requests, string groupBy, string title)
    {
        var html = BuildHtml(requests, groupBy, title, false);
        File.WriteAllText(path, html, Encoding.UTF8);
        LogService.Info($"Экспорт Word: {path}");
    }

    private static string BuildHtml(IEnumerable<ApplicationRequest> source, string groupBy, string title, bool excelMode)
    {
        var requests = source.OrderBy(x => GetGroupValue(x, groupBy)).ThenByDescending(x => x.SubmittedAt).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;color:#172033} h1{color:#5C35E8} h2{background:#EEF2FF;padding:10px;border-radius:8px;color:#4A35B7} table{border-collapse:collapse;width:100%;table-layout:auto;margin-bottom:22px} th{background:#7952FF;color:white;padding:8px;border:1px solid #d9def0} td{padding:7px;border:1px solid #d9def0;vertical-align:top} tr:nth-child(even){background:#f6f8ff}.urgent{background:#FFF1D6;font-weight:600}.meta{color:#68738A;margin-bottom:20px}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h1>{E(title)}</h1>");
        sb.AppendLine($"<div class='meta'>Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}. Всего заявлений: {requests.Count}. Группировка: {E(groupBy)}.</div>");

        foreach (var group in requests.GroupBy(x => GetGroupValue(x, groupBy)))
        {
            sb.AppendLine($"<h2>{E(group.Key)} — {group.Count()}</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Идентификатор</th><th>ФИО</th><th>Телефон</th><th>Адрес</th><th>Категория</th><th>Услуга</th><th>Статус</th><th>Приоритет</th><th>Дата</th><th>Документ</th><th>Внешний комментарий</th><th>Внутренний комментарий</th><th>Служебная метка</th></tr>");
            foreach (var item in group)
            {
                var rowClass = item.Priority == "Срочный" ? " class='urgent'" : string.Empty;
                sb.AppendLine($"<tr{rowClass}><td>{E(item.RequestId)}</td><td>{E(item.FullName)}</td><td>{E(item.Phone)}</td><td>{E(item.Address)}</td><td>{E(item.BenefitCategory)}</td><td>{E(item.ServiceType)}</td><td>{E(item.Status)}</td><td>{E(item.Priority)}</td><td>{item.SubmittedAt:dd.MM.yyyy}</td><td>{E(item.DocumentNumber)}</td><td>{E(item.ExternalComment)}</td><td>{E(item.InternalComment)}</td><td>{E(item.SpecialFlag)}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        if (excelMode)
            sb.AppendLine("<div class='meta'>Файл сохранён в HTML-формате с расширением .xls. Excel открывает его как таблицу со стилями и автоподбором ширины.</div>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string GetGroupValue(ApplicationRequest request, string groupBy)
    {
        return groupBy switch
        {
            "Категория льготы" => request.BenefitCategory,
            "Тип услуги" => request.ServiceType,
            _ => request.Status
        };
    }

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
