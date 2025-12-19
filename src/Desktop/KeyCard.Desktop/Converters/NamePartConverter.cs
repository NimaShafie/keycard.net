// Converters/NamePartConverter.cs
using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace KeyCard.Desktop.Converters
{
    public class NamePartConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string fullName || string.IsNullOrWhiteSpace(fullName))
                return string.Empty;

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return string.Empty;

            // parameter?.ToString() could be "First" or "Last"
            var isLast = parameter?.ToString()?.Equals("Last", StringComparison.OrdinalIgnoreCase) == true;

            if (isLast)
            {
                // First = everything except last
                return parts[^1];
            }
            else
            {
                // First = everything except last
                return string.Join(" ", parts, 0, parts.Length - 1);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Two-way binding isn't supported for name parts
            return Avalonia.Data.BindingOperations.DoNothing;
        }
    }
}
