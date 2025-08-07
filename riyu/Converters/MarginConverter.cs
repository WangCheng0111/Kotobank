using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia;

namespace riyu.Converters
{
    public class MarginConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double y)
            {
                // 正值表示从上滑入（底部外边距），负值表示向下滑出（顶部外边距）
                if (y > 0)
                    return new Thickness(0, 0, 0, y); // 从上滑入
                else
                    return new Thickness(0, -y, 0, 0); // 向下滑出
            }
            return new Thickness(0);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Thickness t)
                return t.Bottom; // 对应底部外边距
            return 0.0;
        }
    }
}
