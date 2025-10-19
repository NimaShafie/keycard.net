// /Converters/StringHasTextConverter.cs
using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace KeyCard.Desktop.Converters
{
    /// <summary>
    /// Returns true when the string has non-whitespace characters.
    /// Null -> false.
    /// </summary>
    public sealed class StringHasTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is string s && !string.IsNullOrWhiteSpace(s);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // "Has text" isn't meaningfully invertible back to a string; preserve the original.
            return value;
        }
    }
}
