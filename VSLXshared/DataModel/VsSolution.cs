using System.Diagnostics;

using Newtonsoft.Json;

using VSLauncher.Helpers;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// The vs solution.
	/// </summary>
	public class VsSolution : VsItem
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VsSolution"/> class.
		/// </summary>
		public VsSolution()
		{
			this.SolutionType = SolutionTypeEnum.None;
			this.ItemType = ItemTypeEnum.Solution;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsSolution"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="path">The path.</param>
		public VsSolution(string name, string path) : base(name, path, null)
		{
			this.ItemType = ItemTypeEnum.Solution;
			this.Refresh();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsSolution"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="path">The path.</param>
		/// <param name="type">The type.</param>
		public VsSolution(string name, string path, SolutionTypeEnum type) : base(name, path, null)
		{
			this.SolutionType = type;
			this.ItemType = ItemTypeEnum.Solution;
			this.Refresh();
		}

		/// <summary>
		/// Gets the projects.
		/// </summary>
		[JsonIgnore]
		public VsItemList Projects { get; private set; } = new VsItemList(null);

		/// <summary>
		/// Added for WPF TreeView hierarchical binding. Maps to Projects so bindings to 'Items' succeed.
		/// </summary>
		[JsonIgnore]
		public VsItemList Items => Projects;

		/// <summary>
		/// Gets the required version.
		/// </summary>
		public string RequiredVersion { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the solution type.
		/// </summary>
		public SolutionTypeEnum SolutionType { get; set; }

		/// <summary>
		/// Gets the projects.
		/// </summary>
		/// <returns>A VsItemList.</returns>
		public VsItemList GetProjects()
		{
			// open solution file, read all "Project" entries, build VsProject items from the contents
			var projects = new VsItemList(null);

			if (PathHelper.PathIsValidAndCanRead(System.IO.Path.GetDirectoryName(this.Path)))
			{
				try
				{
					var sln = File.ReadLines(this.Path!);
					foreach (var s in sln)
					{
						if (s.StartsWith("Project"))
						{
							try
							{
								var parts = s.Split(',');
								var name = parts[0].Split('=')[1].Trim().Replace("\"", "");
								var path = parts[1].Trim().Replace("\"", "");
								var project = ImportHelper.GetItemFromExtension(name, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path!) ?? "", path));
								projects.Add(project);
							}
							catch
							{
								Debug.WriteLine($"Failed to parse '{s}' in file '{this.Path}'");
							}
						}
						else if (s.StartsWith("\tProjectSection(SolutionItems)"))
						{
							// delete last item added from projects list
							projects.RemoveAt(projects.Count - 1);
						}
					}
				}
				catch (System.Exception ex)
				{
					Debug.WriteLine(ex);
				}
			}

			return projects;
		}

		/// <summary>
		/// Gets the required version for the solution file
		/// </summary>
		/// <returns>A string.</returns>
		public string GetRequiredVersion()
		{
			string version = string.Empty;
			// open the solution file, read the first 3 lines, parse the 3rd line

			if (PathHelper.PathIsValidAndCanRead(System.IO.Path.GetDirectoryName(this.Path)))
			{
				try
				{
					var sln = File.ReadLines(this.Path!);
					foreach (var s in sln)
					{
						if (s.StartsWith('#'))
						{
							version = s.Split(' ').Last();
							return version;
						}
					}
					// version = sln[2].Split(',')[1].Trim().Replace("VisualStudioVersion = ", "").Replace("\"", "");
				}
				catch (System.Exception ex)
				{
					Debug.WriteLine(ex);
				}
			}

			return version;
		}

		/// <inheritdoc/>
		public override void Refresh()
		{
			this.Warning = !CheckIsAccessible();

			if (!Warning)
			{
				this.RequiredVersion = this.GetRequiredVersion();
				this.Projects = this.GetProjects();
				GetLastModified();
				DetermineSolutionType();
			}
		}

		/// <inheritdoc/>
		public new string? ToString()
		{
			return $"{this.Name} ({this.TypeAsName()}, Visual Studio {this.RequiredVersion})";
		}

		/// <summary>
		/// Types the as name.
		/// </summary>
		/// <param name="solutionType">The solution type.</param>
		/// <returns>An object.</returns>
		public string TypeAsName()
		{
			return this.SolutionType switch
			{
				SolutionTypeEnum.CSProject => "C#",
				SolutionTypeEnum.VBProject => "VB",
				SolutionTypeEnum.CPPProject => "VC",
				SolutionTypeEnum.FSProject => "F#",
				SolutionTypeEnum.WebSite => "Web",
				SolutionTypeEnum.JSProject => "JavaScript",
				SolutionTypeEnum.TSProject => "TypeScript",
				SolutionTypeEnum.Mixed => "Mixed",
				_ => "Unknown",
			};
		}

		/// <summary>
		/// Checks the is accessible.
		/// </summary>
		/// <returns>A bool.</returns>
		private bool CheckIsAccessible()
		{
			// check if this file is accessible
			if (PathHelper.PathIsValidAndCanRead(System.IO.Path.GetDirectoryName(this.Path)))
			{
				try
				{
					using (var f = File.OpenRead(this.Path!))
					{
						if (f.CanRead && (f.Read(new byte[2], 0, 2) == 2))
						{
							return true;
						}
					}
				}
				catch (System.Exception ex)
				{
					Debug.WriteLine(ex);
				}
			}

			return false;
		}

		/// <summary>
		/// Determines the solution type from the contained projects.
		/// </summary>
		private void DetermineSolutionType()
		{
			ProjectTypeEnum pt = ProjectTypeEnum.None;

			foreach (var p in this.Projects)
			{
				if (p is VsProject proj)
				{
					// if the project type is different from the previous, the solution is mixed
					if (proj.ProjectType != pt && (pt != ProjectTypeEnum.None))
					{
						this.SolutionType = SolutionTypeEnum.Mixed;
						return;
					}

					pt = proj.ProjectType;
				}
			}

			// can convert directly from ProjectType to SolutionType, they are the same order and values
			this.SolutionType = (SolutionTypeEnum)pt;
		}

		/// <summary>
		/// Gets the last modified.
		/// </summary>
		private void GetLastModified()
		{
			if (!string.IsNullOrWhiteSpace(this.Path))
			{
				try
				{
					this.LastModified = new FileInfo(this.Path).LastWriteTime;
				}
				catch
				{
					this.LastModified = DateTime.MinValue;
				}
			}
		}
	}
}