using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using System.Security.Principal;
using VSLauncher.Helpers;
using System.Drawing;
using VSLXshared;
using System.Windows.Forms;

namespace VSLauncher.DataModel
{
    /// <summary>
    /// The visual studio instance.
    /// </summary>
    public class VisualStudioInstance
	{
		private Icon? cachedAppIcon;

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
		/// Gets the short version number, only major and minor version
		/// </summary>
		public string ShortVersion { get { return String.Join('.', this.Version.Split('.').Take(2)); } }

		/// <summary>
		/// Gets the main version number, only major version
		/// </summary>
		public string MainVersion { get { return this.Version.Split('.').First(); } }

		/// <summary>
		/// Gets the release year/version
		/// </summary>
		public string Year { get; }

		/// <summary>
		/// Gets the location.
		/// </summary>
		public string Location { get; }

		/// <summary>
		/// Gets the identifier.
		/// </summary>
		public string Identifier { get; }

		/// <summary>
		/// Gets the app icon of this instance
		/// </summary>
		public Icon AppIcon
		{
			get
			{
				if (this.cachedAppIcon is not null)
				{
					return this.cachedAppIcon;
				}

				// get the application icon from the assembly at the Location property
				try
				{
					this.cachedAppIcon = Icon.ExtractAssociatedIcon(Location) ?? Resources.AppLogo;
				}
				catch
				{
					this.cachedAppIcon = Resources.AppLogo;
				}

				return this.cachedAppIcon;
			}
		}

		/// <inheritdoc/>
		public override string? ToString()
		{
			return $"{Name} ({Version})";
		}

		/// <summary>
		/// Executes the the processstartinfo
		/// </summary>
		/// <param name="psi">The ProcessStartInfo</param>
		/// <returns>A bool.</returns>
		private bool Execute(ProcessStartInfo psi)
		{
			try
			{
				using var process = Process.Start(psi);
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			return true;
		}
		/// <summary>
		/// Executes the.
		/// </summary>
		public void Execute()
		{
			Execute(BuildStartInfo());
		}

		/// <summary>
		/// Executes the as admin.
		/// </summary>
		public void ExecuteAsAdmin()
		{
			Execute(BuildStartInfo(true));
		}

		/// <summary>
		/// Executes the new project.
		/// </summary>
		/// <param name="bAdmin">If true, b admin.</param>
		public void ExecuteNewProject(bool bAdmin)
		{
			Execute(BuildStartInfo(bAdmin, command: "File.NewProject"));
		}

		/// <summary>
		/// Executes the with.
		/// </summary>
		/// <param name="bAdmin">If true, b admin.</param>
		/// <param name="bShowSplash">If true, b show splash.</param>
		/// <param name="instanceName">The instance name.</param>
		/// <param name="command">The command.</param>
		public void ExecuteWith(bool bAdmin, bool bShowSplash, string projectOrSolution, string? instanceName, string? command)
		{
			Execute(BuildStartInfo(bAdmin, bShowSplash, instanceName, command, projectOrSolution));
		}

		/// <summary>
		/// Executes the with instance.
		/// </summary>
		/// <param name="bAdmin">If true, b admin.</param>
		/// <param name="instanceName">The instance name.</param>
		public void ExecuteWithInstance(bool bAdmin, string? instanceName)
		{
			Execute(BuildStartInfo(bAdmin, instance: instanceName));
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

			if (!vsDir.Exists || string.IsNullOrEmpty(identifier))
			{
				return instances;
			}

			try
			{
				foreach (var dir in vsDir.GetDirectories("*", SearchOption.TopDirectoryOnly))
				{
					int idx = dir.Name.IndexOf(identifier, StringComparison.OrdinalIgnoreCase);

					if (idx >= 0)
					{
						string instanceName = dir.Name.Substring(idx + identifier.Length);

						if (instanceName.Length > 0)
						{
							instances.Add(instanceName);
						}
					}
				}
			}
			catch (Exception ex) when (ex is DirectoryNotFoundException or UnauthorizedAccessException or IOException)
			{
				Debug.WriteLine(ex);
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
				Verb            = bAdmin ? "runas" : string.Empty,
				UseShellExecute = bAdmin
			};

			if(SystemUtility.IsAdmin() && !bAdmin)
			{
				// TODO: must run as non-admin user, revert to user from admin
			}

			var args = BuildVisualStudioCommandline(bShowSplash, instance, command, projectOrSolution); //.ForEach(x => si.ArgumentList.Add(x));
			si.Arguments = string.Join(' ', args);
			
			return si;
		}

		/// <summary>
		/// Builds the visual studio commandline.
		/// </summary>
		/// <param name="bShowSplash">If true, b show splash.</param>
		/// <param name="instance">The instance.</param>
		/// <param name="command">The command.</param>
		/// <param name="projectOrSolution">The project or solution.</param>
		/// <returns>A list of string.</returns>
		private List<string> BuildVisualStudioCommandline(bool bShowSplash = false, string? instance = null, string? command = null, string? projectOrSolution = null)
		{
			List<string> list = new();

			if (!string.IsNullOrEmpty(instance))
			{
				list.Add($"/rootSuffix {instance}");
			}

			if (bShowSplash == false)
			{
				list.Add($"/noSplash");
			}

			if (!string.IsNullOrEmpty(command))
			{
				list.Add($"/command");
				list.Add(command);
			}

			if (!string.IsNullOrEmpty(projectOrSolution))
			{
				list.Add($"\"{projectOrSolution}\"");
			}

			return list;
		}

	}
}
