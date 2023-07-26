using System.Diagnostics;
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
			this.RequiredVersion = this.GetRequiredVersion();
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
		}

		/// <summary>
		/// Gets or sets the solution type.
		/// </summary>
		public eSolutionType SolutionType { get; set; }
		
		/// <summary>
		/// Gets the required version.
		/// </summary>
		public string RequiredVersion { get; private set; }

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
					if(s.StartsWith('#'))
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
		public override string? ToString()
		{
			return $"{this.Name} ({this.SolutionType}, Visual Studio {this.RequiredVersion})";
		}
	}
}
