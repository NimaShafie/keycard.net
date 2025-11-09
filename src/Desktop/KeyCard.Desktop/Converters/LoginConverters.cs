// Converters/LoginConverters.cs
using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

namespace KeyCard.Desktop.Converters
{
    /// <summary>
    /// Converts IsMockMode boolean to badge background color
    /// </summary>
    public class MockModeBadgeBackgroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isMock)
            {
                return isMock
                    ? new SolidColorBrush(Color.Parse("#2C2C34"))  // Dark gray for mock
                    : new SolidColorBrush(Color.Parse("#1F3D2F"));  // Dark green for live
            }
            return new SolidColorBrush(Color.Parse("#2C2C34"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts IsMockMode boolean to badge foreground color
    /// </summary>
    public class MockModeBadgeForegroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isMock)
            {
                return isMock
                    ? new SolidColorBrush(Color.Parse("#B388FF"))  // Purple for mock
                    : new SolidColorBrush(Color.Parse("#4CAF50"));  // Green for live
            }
            return new SolidColorBrush(Color.Parse("#B388FF"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts IsMockMode boolean to environment label text
    /// </summary>
    public class EnvironmentLabelConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isMock)
            {
                return isMock ? "MOCK ENVIRONMENT" : "LIVE PRODUCTION";
            }
            return "UNKNOWN ENVIRONMENT";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
