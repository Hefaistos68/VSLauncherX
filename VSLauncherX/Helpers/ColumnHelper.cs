using VSLauncher.DataModel;

namespace VSLauncher.Helpers
{
	/// <summary>
	/// The column helper.
	/// </summary>
	public class ColumnHelper
	{
		public static object GetImageNameForFile(object row)
		{
			if (row is VsFolder)
			{
				return Program.VisualStudioFileIcons32.GetIcon("Folder");
			}

			if (row is VsSolution s)
			{
				return s.Warning ? Program.VisualStudioFileIcons32.GetIcon("Warning") : Program.VisualStudioFileIcons32.GetIcon("Solution");
			}

			if (row is VsProject p)
			{
				return p.Warning ? Program.VisualStudioFileIcons32.GetIcon("Warning") : Program.VisualStudioFileIcons32.GetIcon(p.ProjectType);
			}
			return string.Empty;
		}
		
		public static object GetImageNameForFileImport(object row)
		{
			if (row is VsFolder)
			{
				return Program.VisualStudioFileIcons16.GetIcon("Folder");
			}

			if (row is VsSolution s)
			{
				return s.Warning ? Program.VisualStudioFileIcons16.GetIcon("Warning") : Program.VisualStudioFileIcons16.GetIcon("Solution");
			}

			if (row is VsProject p)
			{
				return p.Warning ? Program.VisualStudioFileIcons16.GetIcon("Warning") : Program.VisualStudioFileIcons16.GetIcon(p.ProjectType);
			}
			return string.Empty;
		}

		public static object GetImageNameForMru(object row)
		{
			if (row is VsFolder f)
			{
				return f.Icon is null ? "VSLogo" : f.Icon;
			}

			if (row is VsSolution s)
			{
				return s.Warning ? Program.VisualStudioFileIcons32.GetIcon("Warning") : Program.VisualStudioFileIcons32.GetIcon("Solution");
			}

			if (row is VsProject p)
			{
				return p.Warning ? Program.VisualStudioFileIcons32.GetIcon("Warning") : Program.VisualStudioFileIcons32.GetIcon(p.ProjectType);
			}

			return string.Empty;
		}

		public static object GetAspectForDate(object row)
		{
			if (row is VsFolder)
			{
				return string.Empty;
			}

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

		internal static CheckState GetCheckState(object rowObject)
		{
			if (rowObject is null)
				return CheckState.Indeterminate;

			if (rowObject is VsFolder f)
			{
				if (f.Checked.HasValue)
					return f.Checked == true ? CheckState.Checked : CheckState.Unchecked;
				else
					return CheckState.Indeterminate;
			}
			return ((VsItem)rowObject).Checked ? CheckState.Checked : CheckState.Unchecked;
		}

		internal static CheckState SetCheckState(object rowObject, CheckState newValue)
		{
			bool b = newValue == CheckState.Checked;

			if (rowObject is VsFolder f)
			{
				f.Checked = b;
			}
			else
			{
				((VsItem)rowObject).Checked = b;
			}

			return newValue;
		}

		internal static object GetDescription(object rowObject)
		{
			string desc;
			if (rowObject is VsSolution s)
			{
				desc = $"Visual Studio Solution: {s.Projects?.Count} Project{((s.Projects?.Count != 1) ? 's' : "")}, {s.TypeAsName()}";

				if(Properties.Settings.Default.ShowPathForSolutions)
				{
					desc += $"\r\n{s.Path}";
				}
			}
			else if (rowObject is VsProject p)
			{
				desc = $"Visual Studio Project: {p.TypeAsName()}, .NET {p.FrameworkVersion}";

				if (Properties.Settings.Default.ShowPathForSolutions)
				{
					desc += $"\r\n{p.Path}";
				}
			}
			else if (rowObject is VsFolder sg)
			{
				desc = $"Contains {sg.ContainedSolutionsCount()} solution{((sg.ContainedSolutionsCount() != 1) ? 's' : "")}";
			}
			else
			{
				desc = ((VsItem)rowObject)?.Path ?? string.Empty;
			}

			return desc;
		}
	}
}