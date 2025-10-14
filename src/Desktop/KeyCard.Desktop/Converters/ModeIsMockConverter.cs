// Converters/ModeIsMockConverter.cs
using System;
using System.Globalization;

using Avalonia.Data;
using Avalonia.Data.Converters;

namespace KeyCard.Desktop.Converters
{
    public sealed class ModeIsMockConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => string.Equals(value as string, "MOCK", StringComparison.OrdinalIgnoreCase);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => BindingOperations.DoNothing;
    }
}
