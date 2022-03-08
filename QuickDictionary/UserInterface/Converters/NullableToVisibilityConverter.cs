using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuickDictionary.UserInterface.Converters;

internal class NullableToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool param = parameter as bool? ?? System.Convert.ToBoolean((string)parameter);
        return value == null ? param ? Visibility.Hidden : Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
