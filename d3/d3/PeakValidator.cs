using System.ComponentModel.DataAnnotations;

namespace PeaksApi;

public static class PeakValidator
{
    // Проверяет корректность данных вершины
    public static List<string> Validate(ConqueredPeak peak)
    {
        var context = new ValidationContext(peak);
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(peak, context, errors, true);
        return errors.Select(e => e.ErrorMessage ?? "Ошибка валидации").ToList();
    }
}