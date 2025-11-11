using System;
using System.Globalization;
using System.Windows.Data;

using VSLauncher.Helpers;

namespace VSLauncher.Converters
{
	/// <summary>
	/// Provides combined Git status and branch name text.
	/// </summary>
	public class ItemToGitBranchConverter : IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null)
			{
				return string.Empty;
			}
			string branch = ColumnHelper.GetAspectForGitBranch(value);

			return branch;
		}

		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
	}
}
