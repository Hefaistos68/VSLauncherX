using System;
using System.Globalization;
using System.Windows.Data;
using VSLauncher.Helpers;

namespace VSLauncher.Converters
{
	/// <summary>
	/// Converts a VsItem/VsFolder to the display text used for file column via ColumnHelper.GetAspectForFile.
	/// </summary>
	public class ItemToFileTextConverter : IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var aspect = ColumnHelper.GetAspectForFile(value!);
			return aspect?.ToString() ?? string.Empty;
		}

		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
	}
}
