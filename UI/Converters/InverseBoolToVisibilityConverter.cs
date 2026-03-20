using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UI.Converters;

/// <summary>
/// Converts a bool to Visibility — true maps to Collapsed,
/// false maps to Visible. The inverse of BooleanToVisibilityConverter.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
                          object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType,
                              object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}