using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Shos.Windows.Data;

/// <summary>
/// Converts an enum value to <see langword="true"/> when it equals the converter parameter.
/// Used to bind multiple <see cref="System.Windows.Controls.RadioButton"/>s to a single enum property.
/// </summary>
/// <remarks>
/// The converter parameter can be the name of an enum member (e.g. <c>ConverterParameter=Small</c>)
/// or the enum value itself (e.g. <c>ConverterParameter={x:Static local:FontSizeKind.Small}</c>).
/// </remarks>
public class EnumToBooleanConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Enum && ToEnum(value.GetType(), parameter) is { } parameterValue
           ? value.Equals(parameterValue)
           : DependencyProperty.UnsetValue;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => true.Equals(value) && ToEnum(Nullable.GetUnderlyingType(targetType) ?? targetType, parameter) is { } parameterValue
           ? parameterValue
           : Binding.DoNothing;

    static object? ToEnum(Type enumType, object? parameter)
    {
        if (!enumType.IsEnum)
            return null;
        return parameter switch {
            Enum enumValue when enumValue.GetType() == enumType                  => enumValue,
            string name when Enum.TryParse(enumType, name, out var parsedValue) => parsedValue,
            _                                                                    => null
        };
    }
}
