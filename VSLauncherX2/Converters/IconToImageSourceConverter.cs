using System;
using System.Globalization;
using System.Drawing;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

namespace VSLauncher.Converters
{
	/// <summary>
	/// Converts a System.Drawing.Icon to a WPF ImageSource.
	/// </summary>
	public class IconToImageSourceConverter : IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Icon icon)
			{
				try
				{
					var bmp = Imaging.CreateBitmapSourceFromHIcon(
					icon.Handle,
					Int32Rect.Empty,
					BitmapSizeOptions.FromWidthAndHeight(icon.Width, icon.Height));
					bmp.Freeze();
					
					return bmp;
				}
				catch
				{
					return null;
				}
			}
			return null;
		}

		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}
}