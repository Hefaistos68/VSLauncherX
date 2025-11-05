using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows;
using VSLauncher.DataModel;
using System.Drawing;

namespace VSLauncher.Converters
{
    public class ItemToIconConverter : IValueConverter
    {
        private static readonly FileIcons Icons32 = new(true); // large icons
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Icon? icon = null;
            switch (value)
            {
                case VsSolution sol:
                    icon = sol.Warning ? Icons32.GetIcon("Warning") : Icons32.GetIcon("Solution");
                    break;
                case VsProject proj:
                    icon = proj.Warning ? Icons32.GetIcon("Warning") : Icons32.GetIcon(proj.ProjectType);
                    break;
                case VsFolder folder:
                    icon = folder.Icon ?? Icons32.GetIcon("Folder");
                    break;
                case VsItem item:
                    icon = item.Warning ? Icons32.GetIcon("Warning") : Icons32.GetIcon("Folder");
                    break;
            }
            return IconToImage(icon);
        }
        private static BitmapSource? IconToImage(Icon? icon)
        {
            if (icon == null) return null;
            var src = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(24,24));
            src.Freeze();
            return src;
        }
        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}