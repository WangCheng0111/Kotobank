using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace riyu.Converters;

public class AndBooleanConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count == 0)
            return false;

        // 检查所有值是否都为true
        return values.All(v => v is bool boolValue && boolValue);
    }
}
