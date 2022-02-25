using System.Globalization;
using System.Windows.Controls;

namespace QuickDictionary.UserInterface.Validation;

public class WordValidationRule : ValidationRule
{
    public static ValidationResult ValidateWord(object value, CultureInfo cultureInfo)
    {
        if (!(value is string val))
            return new ValidationResult(false, "Word must be a string");

        val = val.Trim().ToLower(cultureInfo);
        if (string.IsNullOrWhiteSpace(val))
            return new ValidationResult(false, "Word must not be empty");

        return ValidationResult.ValidResult;
    }

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        return ValidateWord(value, cultureInfo);
    }
}