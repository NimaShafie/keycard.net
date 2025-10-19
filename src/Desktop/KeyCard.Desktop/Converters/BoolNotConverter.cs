// /Converters/BoolNotConverter.cs
using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace KeyCard.Desktop.Converters
{
    /// <summary>
    /// Negates a boolean value. Null or non-bool values are treated as false.
    /// </summary>
    public sealed class BoolNotConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var b = value is bool v && v;
            return !b;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var b = value is bool v && v;
            return !b;
        }
    }
}
