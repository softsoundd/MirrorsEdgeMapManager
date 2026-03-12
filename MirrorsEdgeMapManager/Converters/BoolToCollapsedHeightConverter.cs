using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MirrorsEdgeMapManager.Converters;

public class BoolToCollapsedHeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded && isExpanded)
        {
            return new GridLength(1, GridUnitType.Star);
        }
        return new GridLength(0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
































