using System;
using System.Globalization;
using System.Windows.Data;

namespace QuickDictionary.UserInterface.Converters;

internal class NullableToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool param = parameter as bool? ?? System.Convert.ToBoolean((string)parameter);

        if (param)
        {
            return value == null;
        }
        else
        {
            return value != null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
