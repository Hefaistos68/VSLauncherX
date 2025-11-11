using System;
using System.Globalization;
using System.Windows.Data;
using VSLauncher.Helpers;

namespace VSLauncher.Converters
{
    /// <summary>
    /// Provides version text for Version column.
    /// </summary>
    public class ItemToVersionConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return string.Empty;
            return ColumnHelper.GetAspectForVersion(value) ?? string.Empty;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
