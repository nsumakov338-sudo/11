using System.Text.RegularExpressions;
using BenefitSubsidyDesktop.Models;

namespace BenefitSubsidyDesktop.Services;

public static partial class ValidationService
{
    public static readonly string[] Categories = { "Пенсионер", "Инвалид", "Многодетная семья", "Ветеран", "Малоимущий" };
    public static readonly string[] Services = { "Интернет", "ТВ", "Телефония", "Комплексный пакет" };
    public static readonly string[] Statuses = { "Принято", "На проверке", "Одобрено", "Отказано", "Выполнено" };
    public static readonly string[] Priorities = { "Обычный", "Срочный" };
    public static readonly string[] SpecialFlags = { "Нет", "Требует согласования с юридическим отделом", "Ожидает подтверждения документа" };
    public static readonly string[] Managers = { "manager", "manager_ivanov", "manager_petrov" };

    public static List<string> Validate(ApplicationRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Length > 100 || !FullNameRegex().IsMatch(request.FullName))
            errors.Add("ФИО: не пустое, до 100 символов, только буквы, пробелы и дефисы.");

        if (string.IsNullOrWhiteSpace(request.Phone) || !PhoneRegex().IsMatch(request.Phone))
            errors.Add("Телефон: формат +7XXXXXXXXXX, ровно 11 цифр вместе с 7.");

        if (string.IsNullOrWhiteSpace(request.Address) || request.Address.Length > 150 || !AddressRegex().IsMatch(request.Address))
            errors.Add("Адрес: не пустой, до 150 символов, буквы/цифры/пробелы/знаки препинания.");

        if (!Categories.Contains(request.BenefitCategory))
            errors.Add("Категория льготы должна быть выбрана из справочника.");

        if (!Services.Contains(request.ServiceType))
            errors.Add("Тип услуги должен быть выбран из справочника.");

        if (!Statuses.Contains(request.Status))
            errors.Add("Статус должен быть выбран из справочника.");

        if (!Priorities.Contains(request.Priority))
            errors.Add("Приоритет должен быть выбран из справочника.");

        if (request.SubmittedAt.Date > DateTime.Today)
            errors.Add("Дата подачи не может быть позднее текущей даты.");

        if (string.IsNullOrWhiteSpace(request.DocumentNumber) || request.DocumentNumber.Length > 50 || !DocumentRegex().IsMatch(request.DocumentNumber))
            errors.Add("Номер документа: не пустой, до 50 символов.");

        if (request.InternalComment.Length > 400)
            errors.Add("Внутренний комментарий не должен превышать 400 символов.");

        if (request.ExternalComment.Length > 250)
            errors.Add("Внешний комментарий для менеджера не должен превышать 250 символов.");

        if (!SpecialFlags.Contains(request.SpecialFlag))
            errors.Add("Служебная метка должна быть выбрана из справочника.");

        if (string.IsNullOrWhiteSpace(request.AssignedManager))
            errors.Add("Не указан назначенный менеджер.");

        return errors;
    }

    [GeneratedRegex(@"^[А-Яа-яЁёA-Za-z\s\-]{1,100}$")]
    private static partial Regex FullNameRegex();

    [GeneratedRegex(@"^\+7\d{10}$")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"^[А-Яа-яЁёA-Za-z0-9\s\.,\-\/№#\(\):;]+$")]
    private static partial Regex AddressRegex();

    [GeneratedRegex(@"^[А-Яа-яЁёA-Za-z0-9\s\.,\-\/№#]+$")]
    private static partial Regex DocumentRegex();
}
