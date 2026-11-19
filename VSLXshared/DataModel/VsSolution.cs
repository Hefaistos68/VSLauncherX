using System.Diagnostics;
using System.Text.RegularExpressions;
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
		private const string SolutionFolderTypeGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
		private static readonly Regex HeaderVersionRegex = new(@"\b(\d{2,4})\b", RegexOptions.Compiled);
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
				if (IsSlnxPath(this.Path))
				{
					ParseSlnxProjects(projects);
				}
				else
				{
					ParseSlnProjects(projects);
				}
			}

			return projects;
		}

		/// <summary>
		/// Determines whether the specified path points to an XML-based .slnx solution file.
		/// </summary>
		/// <param name="path">The file path to inspect.</param>
		/// <returns><c>true</c> if the file extension is .slnx; otherwise <c>false</c>.</returns>
		private static bool IsSlnxPath(string? path)
		{
			return string.Equals(System.IO.Path.GetExtension(path), ".slnx", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Parses project entries from a classic text-based .sln solution file.
		/// </summary>
		/// <param name="projects">The destination list for discovered project items.</param>
		private void ParseSlnProjects(VsItemList projects)
		{
			try
			{
				var sln = File.ReadLines(this.Path!);

				foreach (var s in sln)
				{
					if (s.StartsWith("Project(", StringComparison.Ordinal))
					{
						if (s.Contains(SolutionFolderTypeGuid, StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}

						try
						{
							var parts = s.Split(',');

							if (parts.Length < 2)
							{
								continue;
							}

							var nameParts = parts[0].Split('=');

							if (nameParts.Length < 2)
							{
								continue;
							}

							var name = nameParts[1].Trim().Replace("\"", "");
							var path = parts[1].Trim().Replace("\"", "");
							var project = ImportHelper.GetItemFromExtension(name, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path!) ?? "", path));

							projects.Add(project);
						}
						catch
						{
							Debug.WriteLine($"Failed to parse '{s}' in file '{this.Path}'");
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Parses project entries from an XML-based .slnx solution file.
		/// </summary>
		/// <param name="projects">The destination list for discovered project items.</param>
		private void ParseSlnxProjects(VsItemList projects)
		{
			try
			{
				XDocument document = XDocument.Load(this.Path!);
				string solutionDir = System.IO.Path.GetDirectoryName(this.Path!) ?? string.Empty;

				foreach (XElement projectElement in document.Descendants())
				{
					if (!string.Equals(projectElement.Name.LocalName, "Project", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					string? relativePath = projectElement.Attributes()
						.FirstOrDefault(a => string.Equals(a.Name.LocalName, "Path", StringComparison.OrdinalIgnoreCase))?.Value;

					if (string.IsNullOrWhiteSpace(relativePath))
					{
						continue;
					}

					string fullPath = System.IO.Path.Combine(solutionDir, relativePath);
					string name     = System.IO.Path.GetFileNameWithoutExtension(relativePath);
					VsItem project  = ImportHelper.GetItemFromExtension(name, fullPath);

					projects.Add(project);
				}
			}
			catch (System.Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Gets the required version for the solution file
		/// </summary>
		/// <returns>A string.</returns>
		public string GetRequiredVersion()
		{
			if (IsSlnxPath(this.Path))
			{
				return string.Empty;
			}

			string version = string.Empty;
			// open the solution file, read the first 3 lines, parse the 3rd line

			if (PathHelper.PathIsValidAndCanRead(System.IO.Path.GetDirectoryName(this.Path)))
			{
				try
				{
					var sln = File.ReadLines(this.Path!);
					string? headerFallback = null;

					foreach (var rawLine in sln)
					{
						string s = rawLine.Trim();

						if (s.StartsWith("VisualStudioVersion", StringComparison.OrdinalIgnoreCase))
						{
							int eqIndex = s.IndexOf('=');

							if (eqIndex >= 0)
							{
								string verValue = s.Substring(eqIndex + 1).Trim();
								string major = verValue.Split('.')[0];

								if (!string.IsNullOrEmpty(major))
								{
									return major;
								}
							}
						}
						else if (headerFallback == null && s.StartsWith('#'))
						{
							var match = HeaderVersionRegex.Match(s);

							if (match.Success)
							{
								headerFallback = match.Groups[1].Value;
							}
						}
					}

					if (!string.IsNullOrEmpty(headerFallback))
					{
						version = headerFallback;
					}
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