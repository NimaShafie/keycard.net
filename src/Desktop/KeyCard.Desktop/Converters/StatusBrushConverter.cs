// Converters/StatusBrushConverter.cs
using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

namespace KeyCard.Desktop.Converters
{
    public sealed class StatusBrushConverter : IValueConverter
    {
        public static readonly IBrush DefaultBg = Brush.Parse("#2A2A36");
        public static readonly IBrush DefaultFg = Brushes.White;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var status = (value?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
            var kind = (parameter?.ToString() ?? "bg").ToLowerInvariant();

            // Booking statuses
            if (status is "reserved" or "pending")
                return kind == "bg" ? Brush.Parse("#5E5B8A") : Brushes.White;
            if (status is "checkedin" or "checked in")
                return kind == "bg" ? Brush.Parse("#2E7D32") : Brushes.White;
            if (status is "checkedout" or "checked out")
                return kind == "bg" ? Brush.Parse("#546E7A") : Brushes.White;

            // Housekeeping tasks
            if (status.Contains("in progress"))
                return kind == "bg" ? Brush.Parse("#1976D2") : Brushes.White;
            if (status.Contains("completed") || status == "done")
                return kind == "bg" ? Brush.Parse("#388E3C") : Brushes.White;

            // Room statuses
            if (status.Contains("dirty"))
                return kind == "bg" ? Brush.Parse("#D32F2F") : Brushes.White;
            if (status.Contains("clean"))
                return kind == "bg" ? Brush.Parse("#43A047") : Brushes.White;
            if (status.Contains("occupied"))
                return kind == "bg" ? Brush.Parse("#7B1FA2") : Brushes.White;
            if (status.Contains("vacant"))
                return kind == "bg" ? Brush.Parse("#5E35B1") : Brushes.White;

            return kind == "bg" ? DefaultBg : DefaultFg;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
