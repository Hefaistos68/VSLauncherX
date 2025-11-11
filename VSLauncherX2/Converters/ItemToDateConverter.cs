using System;
using System.Globalization;
using System.Windows.Data;
using VSLauncher.Helpers;

namespace VSLauncher.Converters
{
    /// <summary>
    /// Provides formatted date for Last Modified column.
    /// </summary>
    public class ItemToDateConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return string.Empty;
            return ColumnHelper.GetAspectForDate(value) ?? string.Empty;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
