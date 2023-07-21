using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// The visual studio instance.
	/// </summary>
	public class VisualStudioInstance
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VisualStudioInstance"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="version">The version.</param>
		/// <param name="location">The location.</param>
		public VisualStudioInstance(string name, string version, string location, string identifier, string year)
		{
			Name = name;
			Version = version;
			Location = location;
			Identifier = identifier;
			Year = year;
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the name.
		/// </summary>
		public string ShortName
		{
			get
			{
				return $"Visual Studio {Year}";
			}
		}

		/// <summary>
		/// Gets the version.
		/// </summary>
		public string Version { get; }

		/// <summary>
		/// Gets the release year/version
		/// </summary>
		public string Year { get; }

		/// <summary>
		/// Gets the location.
		/// </summary>
		public string Location { get; }

		public string Identifier { get; }

		/// <summary>
		/// Gets the app icon of this instance
		/// </summary>
		public Icon AppIcon
		{
			get
			{
				// get the application icon from the assembly at the Location property
				try
				{
					return Icon.ExtractAssociatedIcon(Location) ?? Resources.AppLogo;
				}
				catch
				{
					return Resources.AppLogo;
				}
			}
		}

		/// <inheritdoc/>
		public override string? ToString()
		{
			return $"{Name} ({Version})";
		}

		/// <summary>
		/// Executes the.
		/// </summary>
		internal void Execute()
		{
			Process.Start(BuildStartInfo());
		}

		/// <summary>
		/// Executes the as admin.
		/// </summary>
		internal void ExecuteAsAdmin()
		{
			Process.Start(BuildStartInfo(true));
		}

		/// <summary>
		/// Executes the new project.
		/// </summary>
		/// <param name="bAdmin">If true, b admin.</param>
		internal void ExecuteNewProject(bool bAdmin)
		{
			Process.Start(BuildStartInfo(bAdmin, command: "np"));
		}

		/// <summary>
		/// Executes the with.
		/// </summary>
		/// <param name="bAdmin">If true, b admin.</param>
		/// <param name="bShowSplash">If true, b show splash.</param>
		/// <param name="instanceName">The instance name.</param>
		/// <param name="command">The command.</param>
		internal void ExecuteWith(bool bAdmin, bool bShowSplash, string projectOrSolution, string instanceName, string command)
		{
			Process.Start(BuildStartInfo(bAdmin, bShowSplash, instanceName, command, projectOrSolution));
		}

		/// <summary>
		/// Executes the with instance.
		/// </summary>
		/// <param name="bAdmin">If true, b admin.</param>
		/// <param name="instanceName">The instance name.</param>
		internal void ExecuteWithInstance(bool bAdmin, string instanceName)
		{
			Process.Start(BuildStartInfo(bAdmin, instance: instanceName));
		}

		/// <summary>
		/// Gets all instances for this version
		/// </summary>
		/// <returns>A list of string.</returns>
		public List<string> GetInstances()
		{
			string identifier = Identifier;

			List<string> instances = new() { "<default>" };
			var vsDir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "VisualStudio"));

			foreach (var dir in vsDir.GetDirectories("*", SearchOption.TopDirectoryOnly))
			{
				if (dir.Name.Contains(identifier))
				{
					string instanceName = dir.Name.Substring(dir.Name.IndexOf(identifier) + identifier.Length);
					if (instanceName.Length > 0)
					{
						instances.Add(instanceName);
					}
				}
			}

			return instances;
		}

		/// <summary>
		/// Builds the start info.
		/// </summary>
		/// <param name="bAdmin">If true, b admin.</param>
		/// <param name="bShowSplash">If true, b show splash.</param>
		/// <param name="instance">The instance name.</param>
		/// <param name="command">The command.</param>
		/// <returns>A ProcessStartInfo.</returns>
		private ProcessStartInfo BuildStartInfo(bool bAdmin = false, bool bShowSplash = false, string? instance = null, string? command = null, string? projectOrSolution = null)
		{
			var si = new ProcessStartInfo(Location)
			{
				Verb = bAdmin ? "runas" : "run",

			};

			if(!string.IsNullOrEmpty(projectOrSolution))
			{
				si.ArgumentList.Add(projectOrSolution);
			}

			if (bShowSplash == false)
			{
				si.ArgumentList.Add($"/noSplash");
			}

			if (!string.IsNullOrEmpty(command))
			{
				si.ArgumentList.Add($"/command");
				si.ArgumentList.Add(command);
			}

			if (string.IsNullOrEmpty(instance))
			{
				si.ArgumentList.Add($"/rootSuffix {instance}");
			}

			return si;
		}
	}
}
