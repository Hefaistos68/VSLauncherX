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
		/// Gets the image name for file.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>An object.</returns>
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

		/// <summary>
		/// Gets the image name for file import.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>An object.</returns>
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

		/// <summary>
		/// Gets the image name for mru.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>An object.</returns>
		public static object GetImageNameForMru(object row)
		{
			if (row is VsFolder f)
			{
				return f.Icon is null ? Program.VisualStudioFileIcons32.GetIcon(name: "Folder") : f.Icon;
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
			return ((VsItem)rowObject).Checked ? CheckState.Checked : CheckState.Unchecked;
		}

		/// <summary>
		/// Gets the description.
		/// </summary>
		/// <param name="rowObject">The row object.</param>
		/// <returns>An object.</returns>
		internal static object? GetDescription(object rowObject)
		{
			string? desc;
			if (rowObject is VsSolution s)
			{
				if (s.Warning)
				{
					desc = "Could not find " + s.Path;
				}
				else
				{
					desc = $"Visual Studio Solution: {s.Projects?.Count} Project{((s.Projects?.Count != 1) ? 's' : "")}, {s.TypeAsName()}";

					if (Properties.Settings.Default.ShowPathForSolutions)
					{
						desc += $"\r\n{s.Path}";
					}
				}
			}
			else if (rowObject is VsProject p)
			{
				if (p.Warning)
				{
					desc = "Could not find " + p.Path;
				}
				else
				{
					desc = $"Visual Studio Project: {p.TypeAsName()}, .NET {p.FrameworkVersion}";

					if (Properties.Settings.Default.ShowPathForSolutions)
					{
						desc += $"\r\n{p.Path}";
					}
				}
			}
			else if (rowObject is VsFolder f)
			{
				// if the path is empty, its either a group or a visual studio version
				switch (f.ItemType)
				{
					case ItemTypeEnum.VisualStudio:
						desc = f.Name;
						break;

					case ItemTypeEnum.Solution:
						desc = f.Path;
						break;

					case ItemTypeEnum.Project:
						desc = f.Path;
						break;

					case ItemTypeEnum.Other:
						desc = string.Empty;
						break;

					case ItemTypeEnum.Folder:
						{
							var so = f.ContainedSolutionsCount();
							var pr = f.ContainedProjectsCount();
							desc = "Contains ";
							if(pr > 0)
							{
								desc += $"{pr} project{((pr != 1) ? 's' : "")}";
							}
							
							if(so > 0)
							{
								if (pr > 0)
								{
									desc += " and ";
								}
								desc += $"{so} solution{((so != 1) ? 's' : "")}";
							}

							if(pr == 0 && so == 0)
							{
								desc += "nothing yet";
							}
						}
						break;

					default:
						desc = string.Empty;
						break;
				}

				if (f.Warning)
				{
					desc = "Could not find " + f.Path;
				}
			}
			else
			{
				desc = ((VsItem)rowObject)?.Path ?? string.Empty;
			}

			return desc;
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
			else
			{
				((VsItem)rowObject).Checked = b;
			}

			return newValue;
		}
	}
}