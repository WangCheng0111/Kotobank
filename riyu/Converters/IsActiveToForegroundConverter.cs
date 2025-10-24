using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace riyu.Converters;

public class IsActiveToForegroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            // 激活状态：正常颜色
            // 失焦状态：淡化颜色
            return isActive ? new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d)) : new SolidColorBrush(Color.FromRgb(0xa0, 0xa0, 0xa0));
        }
        
        return new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}