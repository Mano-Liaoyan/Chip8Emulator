using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Chip8Emulator.Converters;

public class TimerWidthConverter : IValueConverter
{
    public static readonly TimerWidthConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte b)
            return (double)b / 255.0 * 160.0; // 160px max bar width
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
