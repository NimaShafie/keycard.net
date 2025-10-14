// Converters/Converters.cs
using Avalonia.Data.Converters;

namespace KeyCard.Desktop.Converters
{
    public static class Converters
    {
        // Use static properties (not fields) so x:Static can resolve cleanly
        public static IValueConverter BoolToOpacity { get; } =
            new BoolToOpacityConverter { TrueOpacity = 0.6, FalseOpacity = 1.0 };

        public static IValueConverter ModeToBrush { get; } =
            new ModeToBrushConverter(); // LIVE -> green, MOCK -> orange, else fallback
    }
}
