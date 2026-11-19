using System.Diagnostics;
using System.Xml.Linq;

using VSLauncher.Helpers;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// A Visual Studio project item
	/// </summary>
	public class VsProject : VsItem
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VsProject"/> class.
		/// </summary>
		public VsProject()
		{
			this.ProjectType = ProjectTypeEnum.None;
			this.ItemType = ItemTypeEnum.Project;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsProject"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="path">The path.</param>
		/// <param name="prType">The pr type.</param>
		public VsProject(string name, string path, ProjectTypeEnum prType) : base(name, path, null)
		{
			this.ProjectType = prType;
			this.ItemType = ItemTypeEnum.Project;

			this.Refresh();
		}

		/// <summary>
		/// Gets the required version.
		/// </summary>
		public string FrameworkVersion { get; private set; } = "";

		/// <summary>
		/// Gets or sets the project type.
		/// </summary>
		public ProjectTypeEnum ProjectType { get; set; }

		/// <summary>
		/// Gets the .Net version.
		/// </summary>
		/// <returns>A string.</returns>
		public string GetDotNetVersion()
		{
			if (PathHelper.PathIsValidAndCanRead(System.IO.Path.GetDirectoryName(this.Path)))
			{
				try
				{
					// load project file as XML, inspect TargetFramework / TargetFrameworks / TargetFrameworkVersion
					var xdoc = XDocument.Load(this.Path!);
					var root = xdoc.Root;

					if (root != null)
					{
						var ns = root.GetDefaultNamespace();
						string? version = ReadTargetFramework(root, ns);

						if (!string.IsNullOrWhiteSpace(version))
						{
							return version;
						}
					}
				}
				catch
				{
				}
			}

			return "<unknown>";
		}

		/// <summary>
		/// Reads the target framework element value from a project root XML element.
		/// </summary>
		/// <param name="root">The XML root element.</param>
		/// <param name="ns">The XML default namespace.</param>
		/// <returns>The framework version text, or <c>null</c> if not present.</returns>
		private static string? ReadTargetFramework(XElement root, XNamespace ns)
		{
			string[] candidateElements = new[]
			{
				"TargetFramework",
				"TargetFrameworks",
				"TargetFrameworkVersion"
			};

			foreach (string elementName in candidateElements)
			{
				var node = root.Descendants(ns + elementName).FirstOrDefault();

				if (node != null && !string.IsNullOrWhiteSpace(node.Value))
				{
					return node.Value.Trim();
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the required version for the solution file
		/// </summary>
		/// <returns>A string.</returns>
		public string? GetRequiredVersion()
		{
			return VsVersion;
		}

		/// <inheritdoc/>
		public override void Refresh()
		{
			this.Warning = !CheckIsAccessible();

			if (!Warning)
			{
				try
				{
					var fi = new FileInfo(this.Path!);
					this.LastModified = fi.LastWriteTime;
				}
				catch (System.Exception)
				{
					this.LastModified = DateTime.MinValue;
				}

				this.FrameworkVersion = this.GetDotNetVersion();
			}
		}

		/// <inheritdoc/>
		public new string? ToString()
		{
			return $"{this.Name} ({this.ProjectType})";
		}

		/// <summary>
		/// Types the as name.
		/// </summary>
		/// <param name="solutionType">The solution type.</param>
		/// <returns>An object.</returns>
		public string TypeAsName()
		{
			return this.ProjectType switch
			{
				ProjectTypeEnum.CSProject => "C#",
				ProjectTypeEnum.VBProject => "VB",
				ProjectTypeEnum.CPPProject => "VC",
				ProjectTypeEnum.FSProject => "F#",
				ProjectTypeEnum.WebSite => "Web",
				ProjectTypeEnum.JSProject => "JavaScript",
				ProjectTypeEnum.TSProject => "TypeScript",
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
	}
}