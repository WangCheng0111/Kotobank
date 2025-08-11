using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace riyu.Converters
{
    public class IsZeroConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return false;
            try
            {
                var number = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return Math.Abs(number) < double.Epsilon;
            }
            catch
            {
                return false;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
