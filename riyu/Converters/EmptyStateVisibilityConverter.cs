using System;
using System.Globalization;
using Avalonia.Data.Converters;
using System.Collections.Generic; // Added missing import

namespace riyu.Converters
{
    public class EmptyStateVisibilityConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2) return false;
            
            // values[0] 应该是 IsLoadingSheets (bool)
            // values[1] 应该是 Sheets.Count (int)
            
            var isLoading = values[0] as bool? ?? false;
            var count = values[1] as int? ?? 0;
            
            // 只有在非加载状态且没有数据时才显示空状态
            return !isLoading && count == 0;
        }

        public object[]? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
