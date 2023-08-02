﻿using System.Diagnostics;
using System.Xml.Linq;

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
			this.SolutionType =	eSolutionType.None;
			this.ItemType = eItemType.Solution;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsSolution"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="path">The path.</param>
		public VsSolution(string name, string path) : base(name, path, null)
		{
			this.ItemType = eItemType.Solution;
			this.Refresh();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsSolution"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="path">The path.</param>
		/// <param name="type">The type.</param>
		public VsSolution(string name, string path, eSolutionType type) : base(name, path, null)
		{
			this.SolutionType = type;
			this.ItemType = eItemType.Solution;
			this.Refresh();
		}

		/// <summary>
		/// Gets the projects.
		/// </summary>
		[JsonIgnore]
		public VsItemList Projects { get; private set; }

		/// <summary>
		/// Gets the required version.
		/// </summary>
		public string RequiredVersion { get; private set; }

		/// <summary>
		/// Gets or sets the solution type.
		/// </summary>
		public eSolutionType SolutionType { get; set; }

		/// <summary>
		/// Gets the projects.
		/// </summary>
		/// <returns>A VsItemList.</returns>
		public VsItemList GetProjects()
		{
			// open solution file, read all "Project" entries, build VsProject items from the contents
			var projects = new VsItemList(null);
			try
			{
				var sln = File.ReadLines(this.Path);
				foreach (var s in sln)
				{
					if (s.StartsWith("Project"))
					{
						try
						{
							var parts = s.Split(',');
							var name = parts[0].Split('=')[1].Trim().Replace("\"", "");
							var path = parts[1].Trim().Replace("\"", "");
							var project = ImportHelper.GetItemFromExtension(name, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path), path));
							projects.Add(project);
						}
						catch
						{
							Debug.WriteLine($"Failed to parse '{s}' in file '{this.Path}'");
						}
					}
					else if(s.StartsWith("\tProjectSection(SolutionItems)"))
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

			try
			{
				var sln = File.ReadLines(this.Path);
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

			return version;
		}

		/// <inheritdoc/>
		public override void Refresh()
		{
			this.Warning = !CheckIsAccessible();

			if(!Warning)
			{
				this.RequiredVersion = this.GetRequiredVersion();
				this.Projects = this.GetProjects();
				GetLastModified();
				DetermineSolutionType();
			}
		}

		/// <summary>
		/// Checks the is accessible.
		/// </summary>
		/// <returns>A bool.</returns>
		private bool CheckIsAccessible()
		{
			// check if this file is accessible
			try
			{
				using (var f = File.OpenRead(this.Path))
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
			
			return false;
		}

		/// <inheritdoc/>
		public override string? ToString()
		{
			return $"{this.Name} ({this.TypeAsName()}, Visual Studio {this.RequiredVersion})";
		}

		/// <summary>
		/// Types the as name.
		/// </summary>
		/// <param name="solutionType">The solution type.</param>
		/// <returns>An object.</returns>
		internal string TypeAsName()
		{
			return this.SolutionType switch
			{
				eSolutionType.CSProject => "C#",
				eSolutionType.VBProject => "VB",
				eSolutionType.CPPProject => "VC",
				eSolutionType.FSProject => "F#",
				eSolutionType.WebSite => "Web",
				eSolutionType.JSProject => "JavaScript",
				eSolutionType.TSProject => "TypeScript",
				eSolutionType.Mixed => "Mixed",
				_ => "Unknown",
			};
		}

		/// <summary>
		/// Determines the solution type from the contained projects.
		/// </summary>
		private void DetermineSolutionType()
		{
			eProjectType pt = eProjectType.None;

			foreach (var p in this.Projects)
			{
				if (p is VsProject proj)
				{
					// if the project type is different from the previous, the solution is mixed
					if (proj.ProjectType != pt && (pt != eProjectType.None))
					{
						this.SolutionType = eSolutionType.Mixed;
						return;
					}

					pt = proj.ProjectType;
				}
			}

			// can convert directly from ProjectType to SolutionType, they are the same order and values
			this.SolutionType = (eSolutionType)pt;
		}

		private void GetLastModified()
		{
			try
			{
				this.LastModified = new FileInfo(this.Path).LastWriteTime;
			}
			catch (System.Exception ex)
			{
				this.LastModified = DateTime.MinValue;
			}
		}
	}
}
