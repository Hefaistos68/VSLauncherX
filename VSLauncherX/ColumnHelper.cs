using VSLauncher.DataModel;

namespace VSLauncher
{
	/// <summary>
	/// The column helper.
	/// </summary>
	public class ColumnHelper
	{
		public static string GetImageNameForFile(object row)
		{
			if (row is SolutionGroup sg)
			{
				return "ApplicationGroup";
			}

			if (row is VsFolder)
			{
				return "FolderClosed";
			}

			return row is VsSolution ? "VsSolution" : 
					row is VsProject p ? p.ProjectType.ToString() : string.Empty;
		}

		public static string GetImageNameForMru(object row)
		{
			if (row is SolutionGroup sg)
			{
				return "VSLogo";
			}

			if (row is VsFolder)
			{
				return "FolderClosed";
			}

			return row is VsSolution ? "VsSolution" :
					row is VsProject p ? p.ProjectType.ToString() : string.Empty;
		}

		public static object GetAspectForDate(object row)
		{
			if (row is VsItem s)
			{
				return s.LastModified;
			}

			return string.Empty;
		}

		public static object GetAspectForFile(object row)
		{
			if (row is VsItem sg)
			{
				return sg.Name;
			}

			return "";
		}

		public static object GetAspectForOptions(object row)
		{
			eOptions e = eOptions.None;
			if (row is VsItem s)
			{
				e |= s.RunBefore is null ? eOptions.RunBeforeOff : eOptions.RunBeforeOn;
				e |= s.RunAsAdmin ? eOptions.RunAsAdminOn : eOptions.RunAsAdminOff;
				e |= s.RunAfter is null ? eOptions.RunAfterOff : eOptions.RunAfterOn;
			}

			return e;
		}

		public static object GetAspectForPath(object row)
		{
			return row is VsItem sg ? sg.Path ?? string.Empty : string.Empty;
		}
	}
}