using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace riyu.Converters;

public class IsEmptyConverter : IValueConverter
{
    public static readonly IsEmptyConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            bool isEmpty = string.IsNullOrEmpty(stringValue);
            
            // 如果提供了ConverterParameter，返回该参数值
            if (parameter != null)
            {
                return isEmpty ? parameter : Avalonia.Data.BindingOperations.DoNothing;
            }
            
            // 否则返回布尔值
            return isEmpty;
        }
        
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
