using LibGit2Sharp;

using VSLauncher.DataModel;

namespace VSLauncher.Helpers
{
	/// <summary>
	/// The column helper.
	/// </summary>
	public class ColumnHelper
	{
		/// <summary>
		/// Gets the aspect for date.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>An object.</returns>
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

		/// <summary>
		/// Gets the aspect for file.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>An object.</returns>
		public static object GetAspectForFile(object row)
		{
			if (row is VsFolder f)
			{
				return f.Name ?? "";
			}

			if (row is VsSolution s)
			{
				return $"{s.Name} - {s.TypeAsName()} Solution File";
			}

			if (row is VsProject p)
			{
				return $"{p.Name} - {p.TypeAsName()} Project File";
			}

			if (row is VsCodeProject cp)
			{
				return $"{cp.Name} - {cp.TypeAsName()}";
			}

			if (row is VsItem sg)
			{
				return sg.Name ?? string.Empty;
			}

			return "";
		}

		/// <summary>
		/// Gets the aspect for options.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>An object.</returns>
		public static object GetAspectForOptions(object row)
		{
			OptionsEnum e = OptionsEnum.None;
			if (row is VsItem s)
			{
				e |= s.RunBefore is null ? OptionsEnum.RunBeforeOff : OptionsEnum.RunBeforeOn;
				e |= s.RunAsAdmin ? OptionsEnum.RunAsAdminOn : OptionsEnum.RunAsAdminOff;
				e |= s.RunAfter is null ? OptionsEnum.RunAfterOff : OptionsEnum.RunAfterOn;
			}

			return e;
		}

		/// <summary>
		/// Gets the aspect for the git column
		/// </summary>
		/// <param name="rowObject">The row object.</param>
		/// <returns>An object.</returns>
		internal static object GetAspectForGit(object rowObject)
		{
			if (rowObject is VsFolder)
			{
				return string.Empty;
			}

			if (rowObject is VsItem s)
			{
				if (s.Status is null || s.Status == "?")
				{
					return string.Empty;
				}
				if(s.Status == "*")
				{
					return "Modified";
				}
				else if(s.Status == "!")
				{
					return "Clean";
				}
			}

			return string.Empty;
		}
		
		/// <summary>
		/// Gets the aspect for the git branch name.
		/// </summary>
		/// <param name="rowObject">The row object.</param>
		/// <returns>An object.</returns>
		public static object GetAspectForGitName(object rowObject)
		{
			if (rowObject is VsFolder)
			{
				return string.Empty;
			}

			if (rowObject is VsItem s)
			{
				try
				{
					return s.BranchName;
				}
				catch (RepositoryNotFoundException)
				{
					return string.Empty;
				}
			}

			return string.Empty;
		}
		/// <summary>
		/// Gets the icon for a solution item, selecting the .slnx icon when applicable.
		/// </summary>
		/// <param name="icons">The icon set to query.</param>
		/// <param name="item">The solution item.</param>
		/// <returns>The resolved <see cref="Icon"/>.</returns>
		private static Icon GetSolutionIcon(FileIcons icons, VsItem item)
		{
			if (item.Warning)
			{
				return icons.GetIcon("Warning");
			}

			bool isSlnx = string.Equals(Path.GetExtension(item.Path), ".slnx", StringComparison.OrdinalIgnoreCase);

			return icons.GetIcon(isSlnx ? "SolutionX" : "Solution");
		}

		/// <summary>
		/// Gets the icon for a VS Code project or workspace item.
		/// </summary>
		/// <param name="icons">The icon set to query.</param>
		/// <param name="project">The VS Code project item.</param>
		/// <returns>The resolved <see cref="Icon"/>.</returns>
		private static Icon GetVsCodeProjectIcon(FileIcons icons, VsCodeProject project)
		{
			if (project.Warning)
			{
				return icons.GetIcon("Warning");
			}

			string key = project.ProjectKind == VsCodeProjectKind.Workspace ? "CodeWorkspace" : "VsCodeProject";

			return icons.GetIcon(key);
		}

		/// <summary>
		/// Resolves the icon or bitmap to display for a tree or list row.
		/// </summary>
		/// <param name="row">The row model.</param>
		/// <param name="icons">The icon set to query.</param>
		/// <param name="large">Whether to use large folder icons.</param>
		/// <param name="useFolderCustomIcon">Whether to prefer <see cref="VsFolder.Icon"/> when present.</param>
		/// <returns>The resolved icon/bitmap object, or an empty string when unavailable.</returns>
		private static object GetImageForRow(object row, FileIcons icons, bool large, bool useFolderCustomIcon)
		{
			if (row is VsFolder f)
			{
				if (useFolderCustomIcon && f.Icon is not null)
				{
					return f.Icon;
				}

				return ThemeHelper.GetFolderIcon(large: large);
			}

			if (row is VsSolution s)
			{
				return GetSolutionIcon(icons, s);
			}

			if (row is VsProject p)
			{
				return p.Warning ? icons.GetIcon("Warning") : icons.GetIcon(p.ProjectType);
			}

			if (row is VsCodeProject cp)
			{
				return GetVsCodeProjectIcon(icons, cp);
			}

			if (row is VsItem item && string.Equals(Path.GetExtension(item.Path), ".slnx", StringComparison.OrdinalIgnoreCase))
			{
				return GetSolutionIcon(icons, item);
			}

			return string.Empty;
		}

		/// <summary>
		/// Gets the image name for file.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>An object.</returns>
		public static object GetImageNameForFile(object row)
		{
			return GetImageForRow(row, Program.VisualStudioFileIcons32, large: true, useFolderCustomIcon: false);
		}

		/// <summary>
		/// Gets the image name for file import.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>An object.</returns>
		public static object GetImageNameForFileImport(object row)
		{
			return GetImageForRow(row, Program.VisualStudioFileIcons16, large: false, useFolderCustomIcon: false);
		}

		/// <summary>
		/// Gets the image name for mru.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>An object.</returns>
		public static object GetImageNameForMru(object row)
		{
			return GetImageForRow(row, Program.VisualStudioFileIcons32, large: true, useFolderCustomIcon: true);
		}

		/// <summary>
		/// Gets the check state.
		/// </summary>
		/// <param name="rowObject">The row object.</param>
		/// <returns>A CheckState.</returns>
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
			if (rowObject is VsItem item)
			{
				return item.Checked ? CheckState.Checked : CheckState.Unchecked;
			}

			return CheckState.Unchecked;
		}

		/// <summary>
		/// Sets the check state.
		/// </summary>
		/// <param name="rowObject">The row object.</param>
		/// <param name="newValue">The new value.</param>
		/// <returns>A CheckState.</returns>
		internal static CheckState SetCheckState(object rowObject, CheckState newValue)
		{
			bool b = newValue == CheckState.Checked;

			if (rowObject is VsFolder f)
			{
				f.Checked = b;
			}
			else if (rowObject is VsItem item)
			{
				item.Checked = b;
			}

			return newValue;
		}
		/// <summary>
		/// Gets the description.
		/// </summary>
		/// <param name="rowObject">The row object.</param>
		/// <returns>An object.</returns>
		internal static object? GetDescription(object rowObject)
		{
			if (rowObject is VsSolution s)
			{
				return GetSolutionDescription(s);
			}

			if (rowObject is VsProject p)
			{
				return GetProjectDescription(p);
			}

			if (rowObject is VsCodeProject cp)
			{
				return GetVsCodeProjectDescription(cp);
			}

			if (rowObject is VsFolder f)
			{
				return GetFolderDescription(f);
			}

			if (rowObject is VsItem item)
			{
				return item.Path ?? string.Empty;
			}

			return string.Empty;
		}

		/// <summary>
		/// Formats the description line for a Visual Studio solution.
		/// </summary>
		/// <param name="solution">The solution item.</param>
		/// <returns>The formatted description string.</returns>
		private static string GetSolutionDescription(VsSolution solution)
		{
			if (solution.Warning)
			{
				return "Could not find " + solution.Path;
			}

			string desc = $"Visual Studio Solution: {solution.Projects?.Count} Project{((solution.Projects?.Count != 1) ? 's' : "")}, {solution.TypeAsName()}";

			if (Properties.Settings.Default.ShowPathForSolutions)
			{
				desc += $"\r\n{solution.Path}";
			}

			return desc;
		}

		/// <summary>
		/// Formats the description line for a Visual Studio project.
		/// </summary>
		/// <param name="project">The project item.</param>
		/// <returns>The formatted description string.</returns>
		private static string GetProjectDescription(VsProject project)
		{
			if (project.Warning)
			{
				return "Could not find " + project.Path;
			}

			string desc = $"Visual Studio Project: {project.TypeAsName()}, .NET {project.FrameworkVersion}";

			if (Properties.Settings.Default.ShowPathForSolutions)
			{
				desc += $"\r\n{project.Path}";
			}

			return desc;
		}

		/// <summary>
		/// Formats the description line for a Visual Studio Code project or workspace.
		/// </summary>
		/// <param name="project">The VS Code project item.</param>
		/// <returns>The formatted description string.</returns>
		private static string GetVsCodeProjectDescription(VsCodeProject project)
		{
			if (project.Warning)
			{
				return "Could not find " + project.Path;
			}

			string desc = $"VS Code: {project.TypeAsName()}";

			if (Properties.Settings.Default.ShowPathForSolutions)
			{
				desc += $"\r\n{project.Path}";
			}

			return desc;
		}

		/// <summary>
		/// Formats the description line for a <see cref="VsFolder"/> node.
		/// </summary>
		/// <param name="folder">The folder item.</param>
		/// <returns>The formatted description string.</returns>
		private static string? GetFolderDescription(VsFolder folder)
		{
			if (folder.Warning)
			{
				return "Could not find " + folder.Path;
			}

			// if the path is empty, its either a group or a visual studio version
			return folder.ItemType switch
			{
				ItemTypeEnum.VisualStudio => folder.Name,
				ItemTypeEnum.Solution     => folder.Path,
				ItemTypeEnum.Project      => folder.Path,
				ItemTypeEnum.Folder       => BuildFolderCountsDescription(folder),
				_                         => string.Empty
			};
		}

		/// <summary>
		/// Builds the contained project/solution summary for a folder node, noting workspace-backed folders.
		/// </summary>
		/// <param name="folder">The folder item.</param>
		/// <returns>The formatted description string.</returns>
		private static string BuildFolderCountsDescription(VsFolder folder)
		{
			int so            = folder.ContainedSolutionsCount();
			int pr            = folder.ContainedProjectsCount();
			string counts     = FormatContainedCounts(pr, so);
			bool isWorkspace  = folder.IsWorkspaceBacked(allowDiskProbe: false);
			bool isVsCodeOnly = !isWorkspace && folder.IsVsCodeConfigured();

			if (!isWorkspace && !isVsCodeOnly)
			{
				return counts;
			}

			string desc = FormatVsCodeFolderDescription(counts, pr, so, isWorkspace);

			if (Properties.Settings.Default.ShowPathForSolutions && !string.IsNullOrWhiteSpace(folder.Path))
			{
				desc += $"\r\n{folder.Path}";
			}

			return desc;
		}

		/// <summary>
		/// Formats the descriptive label for a workspace-backed or VS Code-configured folder.
		/// </summary>
		/// <param name="counts">The formatted contained counts summary.</param>
		/// <param name="projects">The number of contained projects.</param>
		/// <param name="solutions">The number of contained solutions.</param>
		/// <param name="isWorkspace"><c>true</c> if the folder is backed by a <c>.code-workspace</c> file; otherwise <c>false</c>.</param>
		/// <returns>The formatted description line.</returns>
		private static string FormatVsCodeFolderDescription(string counts, int projects, int solutions, bool isWorkspace)
		{
			if (projects == 0 && solutions == 0)
			{
				return isWorkspace ? "VS Code Workspace Folder" : "VS Code Folder";
			}

			return isWorkspace ? $"{counts} (VS Code Workspace)" : $"{counts} (VS Code)";
		}

		/// <summary>
		/// Formats the human-readable project and solution count summary.
		/// </summary>
		/// <param name="projects">The number of contained projects.</param>
		/// <param name="solutions">The number of contained solutions.</param>
		/// <returns>The formatted counts string.</returns>
		private static string FormatContainedCounts(int projects, int solutions)
		{
			if (projects == 0 && solutions == 0)
			{
				return "Contains nothing yet";
			}

			var parts = new List<string>(2);

			if (projects > 0)
			{
				parts.Add($"{projects} project{(projects != 1 ? "s" : "")}");
			}

			if (solutions > 0)
			{
				parts.Add($"{solutions} solution{(solutions != 1 ? "s" : "")}");
			}

			return "Contains " + string.Join(" and ", parts);
		}

		/// <summary>
		/// Gets the image for git status
		/// </summary>
		/// <param name="rowObject">The row object.</param>
		/// <returns>An object? .</returns>
		internal static object? GetImageForGit(object rowObject)
		{
			if (rowObject is VsFolder)
			{
				return null;
			}

			if (rowObject is VsItem s)
			{
				if (s.Status is null || s.Status == "?")
				{
					return null;
				}
				if (s.Status == "*")
				{
					return ThemeHelper.GetGitStatusIcon(isDirty: true);
				}
				else if (s.Status == "!")
				{
					return ThemeHelper.GetGitStatusIcon(isDirty: false);
				}
			}

			return null;
		}

	}
}