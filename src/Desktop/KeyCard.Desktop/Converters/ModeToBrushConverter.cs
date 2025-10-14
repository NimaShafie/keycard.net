// Converters/ModeToBrushConverter.cs
using System;
using System.Globalization;

using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace KeyCard.Desktop.Converters
{
    public sealed class ModeToBrushConverter : IValueConverter
    {
        public IBrush LiveBrush { get; set; } = new SolidColorBrush(Color.Parse("#2ECC71"));
        public IBrush MockBrush { get; set; } = new SolidColorBrush(Color.Parse("#F39C12"));
        public IBrush FallbackBrush { get; set; } = new SolidColorBrush(Color.Parse("#CFCFF1"));

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var s = value as string;
            if (string.Equals(s, "LIVE", StringComparison.OrdinalIgnoreCase))
                return LiveBrush;
            if (string.Equals(s, "MOCK", StringComparison.OrdinalIgnoreCase))
                return MockBrush;
            return FallbackBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => BindingOperations.DoNothing;
    }
}
