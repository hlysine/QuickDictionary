using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using QuickDictionary.Models.WordLists;

namespace QuickDictionary.UserInterface.Validation;

public class WordListNameValidationRule : ValidationRule
{
    public static ValidationResult ValidateWordlistName(object value, CultureInfo cultureInfo)
    {
        if (!(value is string val))
            return new ValidationResult(false, "Name must be a string");

        val = val.Trim().ToLower(cultureInfo);
        if (string.IsNullOrWhiteSpace(val))
            return new ValidationResult(false, "Name cannot be empty");
        if (WordListStore.WordListFiles.Select(x => x.WordList.Name.ToLower()).Contains(val))
            return new ValidationResult(false, "This list exists already");
        if (val.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return new ValidationResult(false, "Invalid character");

        return ValidationResult.ValidResult;
    }

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        return ValidateWordlistName(value, cultureInfo);
    }
}
