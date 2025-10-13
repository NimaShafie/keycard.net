// Converters/BoolToOpacityConverter.cs
using System;
using System.Globalization;
using System.Reflection;

using Avalonia.Data;
using Avalonia.Data.Converters;

namespace KeyCard.Desktop.Converters
{
    public sealed class BoolToOpacityConverter : IValueConverter
    {
        public double TrueOpacity { get; set; } = 0.6; // locked
        public double FalseOpacity { get; set; } = 1.0; // not locked

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool flag = false;

            if (value is bool b)
            {
                flag = b;
            }
            else if (value is not null)
            {
                // Look for an 'IsLocked' bool on the bound row item
                var prop = value.GetType().GetProperty("IsLocked", BindingFlags.Public | BindingFlags.Instance);
                if (prop?.PropertyType == typeof(bool))
                {
                    var v = prop.GetValue(value);
                    if (v is bool vb) flag = vb;
                }
            }

            return flag ? TrueOpacity : FalseOpacity;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => BindingOperations.DoNothing;
    }
}
