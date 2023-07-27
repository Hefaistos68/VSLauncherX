using System.Diagnostics;

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
			try
			{
				this.LastModified = new FileInfo(this.Path).LastWriteTime;
			}
			catch (System.Exception ex)
			{
				this.LastModified = DateTime.MinValue;
			}
		}

		/// <summary>
		/// Gets or sets the project type.
		/// </summary>
		public eProjectType ProjectType { get; set; }

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
	}
}
