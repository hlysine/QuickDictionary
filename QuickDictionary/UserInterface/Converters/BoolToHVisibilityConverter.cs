using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuickDictionary.UserInterface.Converters;

public class BoolToHVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool param = parameter as bool? ?? System.Convert.ToBoolean((string)parameter);
        if ((value as bool?).GetValueOrDefault() != param)
            return Visibility.Visible;

        return Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
