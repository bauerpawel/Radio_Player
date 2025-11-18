using System;
using System.Globalization;
using System.Windows.Data;

namespace RadioPlayer.WPF.Converters;

/// <summary>
/// Converts null to boolean (null = false, not null = true)
/// Used for enabling buttons when item is selected
/// </summary>
public class NullToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean buffering state to text
/// </summary>
public class BoolToBufferingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isBuffering && isBuffering)
        {
            return "Buffering...";
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts volume from 0.0-1.0 to 0-100 for slider
/// </summary>
public class VolumeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float volume)
        {
            return volume * 100f;
        }
        return 80.0; // Default
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double sliderValue)
        {
            return (float)(sliderValue / 100.0);
        }
        return 0.8f; // Default
    }
}

/// <summary>
/// Converts volume to percentage text
/// </summary>
public class VolumePercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float volume)
        {
            return $"{(int)(volume * 100)}%";
        }
        return "80%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
