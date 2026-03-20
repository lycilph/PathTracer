using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ScriptApi.Validation;

namespace UI.Converters;

/// <summary>
/// Converts a <see cref="ValidationSeverity"/> to a display colour.
/// Errors are red, warnings are amber.
/// </summary>
[ValueConversion(typeof(ValidationSeverity), typeof(Brush))]
public sealed class ValidationSeverityToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
                          object parameter, CultureInfo culture)
    {
        if (value is ValidationSeverity severity)
        {
            return severity switch
            {
                ValidationSeverity.Error => Brushes.IndianRed,
                ValidationSeverity.Warning => Brushes.Goldenrod,
                _ => Brushes.Gray
            };
        }

        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType,
                              object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}