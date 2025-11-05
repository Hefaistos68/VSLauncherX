using System.Diagnostics;
using System.Xml.Linq;

using VSLauncher.Helpers;
using Newtonsoft.Json;

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
			bool isSdk = false;
			if (PathHelper.PathIsValidAndCanRead(System.IO.Path.GetDirectoryName(this.Path)))
			{
				try
				{
					// load solution file as XML, find the TargetFrameworkVersion node and read the value
					var xdoc = XDocument.Load(this.Path!);
					var ns = xdoc.Root?.GetDefaultNamespace();
					var project = xdoc.Root?.Descendants((ns ?? "") + "Project").FirstOrDefault();

					if (project != null)
					{
						var sdk = project.Attribute("Sdk");
						if (sdk != null)
						{
							isSdk = true;
						}
					}

					if (isSdk)
					{
						var node = xdoc.Root?.Descendants((ns ?? "") + "TargetFrameworkVersion").FirstOrDefault();

						if (node != null)
						{
							return node.Value;
						}

						// not found, read the other value
						node = xdoc.Root?.Descendants((ns ?? "") + "TargetFramework").FirstOrDefault();
						if (node != null)
						{
							return node.Value;
						}
					}
					else
					{
						return "Framework?";
					}
				}
				catch
				{
				}
			}
			return "<unknown>";
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

		/// <summary>
		/// Empty children collection added for TreeView hierarchical binding.
		/// Projects have no child items but WPF bindings expect 'Items'.
		/// </summary>
		[JsonIgnore]
		public VsItemList Items { get; } = new VsItemList(null);
	}
}