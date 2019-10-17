using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Shos.Windows.Data
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parameterString = parameter as string;
            if (string.IsNullOrWhiteSpace(parameterString) || !Enum.IsDefined(value.GetType(), value))
                return DependencyProperty.UnsetValue;

            var parameterValue = Enum.Parse(value.GetType(), parameterString);
            return (int)parameterValue == (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parameterString = parameter as string;
            return !true.Equals(value) || string.IsNullOrWhiteSpace(parameterString)
                   ? DependencyProperty.UnsetValue
                   : Enum.Parse(targetType, parameterString);
        }
    }
}
