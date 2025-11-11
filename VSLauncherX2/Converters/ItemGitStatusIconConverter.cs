using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using VSLauncher.DataModel;

using VSLauncher.Helpers;

namespace VSLauncher.Converters
{
    /// <summary>
    /// Returns an ImageSource representing the Git status for a VsItem.
    /// Images are cached (loaded once) to avoid repeated disk/resource lookups.
    /// </summary>
    public class ItemGitStatusIconConverter : IValueConverter
    {
        private static readonly ImageSource? GitDirtyImage;
        private static readonly ImageSource? GitCleanImage;
        private const string AssemblySegment = "VSLauncherX2"; // adjust if images live in another assembly
        private const string ImageFolder = "Resources/Images"; // folder containing the images

        static ItemGitStatusIconConverter()
        {
            GitDirtyImage = LoadImage("GitDirty2.png");
            GitCleanImage = LoadImage("GitClean2.png");
        }

        private static ImageSource? LoadImage(string fileName)
        {
            try
            {
                string uriString = $"pack://application:,,,/{AssemblySegment};component/{ImageFolder}/{fileName}";
                var uri = new Uri(uriString, UriKind.Absolute);
                var bmp = new BitmapImage();

                bmp.BeginInit();
                bmp.UriSource = uri;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // ensure initial load from resource
                bmp.EndInit();
                bmp.Freeze();

                return bmp;
            }
            catch
            {
                return null;
            }
        }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not VsItem item || item is VsFolder)
            {
                return null;
            }

            string? status = item.Status;

            if (string.IsNullOrEmpty(status) || status == "?")
            {
                return null;
            }

            return status switch
            {
                "*" => GitDirtyImage,
                "!" => GitCleanImage,
                _   => null
            };
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
