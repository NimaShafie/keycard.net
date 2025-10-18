// /Converters/NamePartConverter.cs
using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace KeyCard.Desktop.Converters
{
    /// <summary>
    /// Extracts "First" or "Last" from a full name string.
    /// ConverterParameter: "First" | "Last"
    /// </summary>
    public sealed class NamePartConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var full = (value as string)?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(full)) return string.Empty;

            var parts = full.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parameter?.ToString()?.Equals("Last", StringComparison.OrdinalIgnoreCase) == true
                    ? string.Empty
                    : parts[0];

            var isLast = parameter?.ToString()?.Equals("Last", StringComparison.OrdinalIgnoreCase) == true;
            if (isLast)
                return parts[^1];

            // First = everything except last
            return string.Join(' ', parts, 0, parts.Length - 1);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
