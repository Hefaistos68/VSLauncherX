using System.Diagnostics;
using System.Xml.Linq;

using Newtonsoft.Json;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// The vs project.
	/// </summary>
	public class VsProject : VsItem
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VsProject"/> class.
		/// </summary>
		public VsProject()
		{
			this.ProjectType = eProjectType.None;
			this.ItemType = eItemType.Project;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsProject"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="path">The path.</param>
		/// <param name="prType">The pr type.</param>
		public VsProject(string name, string path, eProjectType prType) : base(name, path, null)
		{
			this.ProjectType = prType;
			this.ItemType = eItemType.Project;

			this.Refresh();
		}

		/// <summary>
		/// Gets or sets the project type.
		/// </summary>
		public eProjectType ProjectType { get; set; }

		/// <summary>
		/// Gets the required version.
		/// </summary>
		public string FrameworkVersion { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether this solution is possibly inaccessible
		/// </summary>
		[JsonIgnore]
		public bool Warning { get; set; }

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

		/// <summary>
		/// Gets the .Net version.
		/// </summary>
		/// <returns>A string.</returns>
		public string GetDotNetVersion()
		{
			try
			{
				// load solution file as XML, find the TargetFrameworkVersion node and read the value
				var xdoc = XDocument.Load(this.Path);
				var ns = xdoc.Root.GetDefaultNamespace();
				var node = xdoc.Root.Descendants(ns + "TargetFrameworkVersion").FirstOrDefault();
				
				if (node != null)
				{
					return node.Value;
				}
	
				// not found, read the other value
				node = xdoc.Root.Descendants(ns + "TargetFramework").FirstOrDefault();
				if (node != null)
				{
					return node.Value;
				}
			}
			catch (System.Exception)
			{
			}

			return "<unknown>";
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
// 				var sln = File.ReadLines(this.Path);
// 				foreach (var s in sln)
// 				{
// 					if (s.StartsWith('#'))
// 					{
// 						version = s.Split(' ').Last();
// 						return version;
// 					}
// 				}
			}
			catch (System.Exception ex)
			{
				Debug.WriteLine(ex);
			}

			return version;
		}

		/// <inheritdoc/>
		public override string? ToString()
		{
			return $"{this.Name} ({this.ProjectType})";
		}
		/// <summary>
		/// Types the as name.
		/// </summary>
		/// <param name="solutionType">The solution type.</param>
		/// <returns>An object.</returns>
		internal string TypeAsName()
		{
			return this.ProjectType switch
			{
				eProjectType.CSProject => "C#",
				eProjectType.VBProject => "VB",
				eProjectType.CPPProject => "VC",
				eProjectType.FSProject => "F#",
				eProjectType.WebSite => "Web",
				eProjectType.JSProject => "JavaScript",
				eProjectType.TSProject => "TypeScript",
				_ => "Unknown",
			};
		}

		/// <inheritdoc/>
		public override void Refresh()
		{
			this.Warning = !CheckIsAccessible();

			if (!Warning)
			{
				try
				{
					var fi = new FileInfo(this.Path);
					this.LastModified = fi.LastWriteTime;
				}
				catch (System.Exception)
				{
					this.LastModified = DateTime.MinValue;
				}

				this.FrameworkVersion = this.GetDotNetVersion();
			}
		}
	}
}
