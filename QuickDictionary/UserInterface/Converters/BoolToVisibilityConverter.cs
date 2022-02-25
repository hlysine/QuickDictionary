using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuickDictionary.UserInterface.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var param = (parameter as bool?) ?? System.Convert.ToBoolean((string)parameter);
        if ((value as bool?).GetValueOrDefault() != param)
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}