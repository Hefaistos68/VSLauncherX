using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace VSLauncher.Converters
{
	/// <summary>
	/// Adjusts the width of the first data column so the second column aligns vertically regardless of TreeViewItem indent.
	/// Supports single-value conversion (legacy) and multi-value conversion (width + TreeViewItem) to avoid invalid Binding in ConverterParameter.
	/// </summary>
	public class IndentAdjustedWidthConverter : IValueConverter, IMultiValueConverter
	{
		public double IndentSize { get; set; } = 19.0; // default TreeView indent per level

		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is double originalWidth)
			{
				int depth = 0;

				if (parameter is DependencyObject d)
				{
					depth = GetDepth(d);
				}
				else if (parameter is string s && int.TryParse(s, out int parsed))
				{
					depth = parsed;
				}

				double adjustment = depth * IndentSize;
				double adjusted = Math.Max(0, originalWidth - adjustment);
				return adjusted;
			}

			return value;
		}

		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;

		// Multi-binding version: values[0] = original width, values[1] = TreeViewItem (DependencyObject)
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length >= 1 && values[0] is double originalWidth)
			{
				int depth = 0;

				if (values.Length >= 2 && values[1] is DependencyObject d)
				{
					depth = GetDepth(d);
				}

				double adjusted = Math.Max(0, originalWidth - depth * IndentSize);
				return adjusted;
			}

			return values.Length > 0 ? values[0] : 0.0;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			return Array.Empty<object>();
		}

		private static int GetDepth(DependencyObject obj)
		{
			int depth = 0;
			DependencyObject? current = obj;

			while (current != null)
			{
				current = VisualTreeHelper.GetParent(current);

				if (current is TreeViewItem)
				{
					depth++;
				}
			}

			return depth;
		}
	}
}
