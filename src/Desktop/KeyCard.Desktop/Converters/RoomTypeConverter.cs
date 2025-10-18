// /Converters/RoomTypeConverter.cs
using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace KeyCard.Desktop.Converters
{
    /// <summary>
    /// Temporary heuristic: derive room type from room number.
    /// Replace later with data from /api/guest/Rooms/room-options.
    /// </summary>
    public sealed class RoomTypeConverter : IValueConverter
    {
        public static readonly RoomTypeConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return string.Empty;

            if (!int.TryParse(value.ToString(), out var room))
                return string.Empty;

            // Simple, predictable mapping:
            var last = Math.Abs(room) % 10;
            if (last <= 3) return "Regular Room";
            if (last <= 6) return "King Room";
            return "Luxury Room";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
