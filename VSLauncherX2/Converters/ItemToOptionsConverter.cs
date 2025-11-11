using System;
using System.Globalization;
using System.Windows.Data;
using VSLauncher.Helpers;
using VSLauncher.DataModel;

namespace VSLauncher.Converters
{
    /// <summary>
    /// Converts OptionsEnum flags to display text.
    /// </summary>
    public class ItemToOptionsConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return string.Empty;
            var flags = ColumnHelper.GetAspectForOptions(value);
            if (flags == OptionsEnum.None) return string.Empty;
            // Simple textual aggregation
            string text = string.Empty;
            if ((flags & OptionsEnum.RunBeforeOn) != 0) text += "Before ";
            if ((flags & OptionsEnum.RunAfterOn) != 0) text += "After ";
            if ((flags & OptionsEnum.RunAsAdminOn) != 0) text += "Admin ";
            return text.Trim();
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
