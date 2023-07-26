using System.Diagnostics;
using System.Windows.Forms;

using Newtonsoft.Json;
using VSLauncher.DataModel;

namespace BackgroundLaunch
{
	internal static class Program
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// To customize application configuration such as set high DPI settings or default font,
			// see https://aka.ms/applicationconfiguration.
			ApplicationConfiguration.Initialize();

			// get commandline parameters
			var args = System.Environment.GetCommandLineArgs();

			if(args.Length != 2)
			{
				Application.Exit();
				return;
			}

			string json = args[1].Replace('«', '\"').Replace('»', ' ');

			var item = JsonConvert.DeserializeObject<LaunchInfo>(json);

			if(item != null)
			{
				new Runner(item).Run();
			}

			Application.Exit();
		}
	}

	/// <summary>
	/// The runner.
	/// </summary>
	public class Runner
	{
		private readonly LaunchInfo launchInfo;

		/// <summary>
		/// Initializes a new instance of the <see cref="Runner"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		public Runner(LaunchInfo item)
		{
			this.launchInfo = item;
		}

		/// <summary>
		/// Runs a single item
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns>A bool.</returns>
		private bool RunItem(VsItem? item)
		{
			// dont do anything if empty
			if (item is null)
			{
				return true;
			}

			Process? process;
			ProcessStartInfo startInfo = new ProcessStartInfo();
			string? workingPath = Path.GetDirectoryName(item.Path);
			if(PathIsValid(workingPath))
			{
				startInfo.WorkingDirectory = workingPath;
			}
			startInfo.Verb = item.RunAsAdmin ? "runas" : "run";

			if (item.ItemType == eItemType.Solution)
			{
				startInfo.FileName = this.launchInfo.Target;
				startInfo.Arguments = item.Path;
				if(!string.IsNullOrEmpty(item.Commands))
				{
					startInfo.Arguments += " " + item.Commands;
				}
			}
			else if (item.ItemType == eItemType.Project)
			{
				startInfo.FileName = this.launchInfo.Target;
				startInfo.Arguments = item.Path;
				if (!string.IsNullOrEmpty(item.Commands))
				{
					startInfo.Arguments += " " + item.Commands;
				}
			}
			else
			{
				startInfo.FileName = "explorer.exe";
				startInfo.Arguments = item.Path;
				if (!string.IsNullOrEmpty(item.Commands))
				{
					startInfo.Arguments += " " + item.Commands;
				}
			}

			process = Process.Start(startInfo);

			if (item.WaitForCompletion)
			{
				process?.WaitForExit();
			}

			return process != null;
		}

		/// <summary>
		/// Paths the is valid.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns>A bool.</returns>
		private bool PathIsValid(string? path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return false;
			}

			if (!Directory.Exists(path))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Runs the all items
		/// </summary>
		internal void Run()
		{
			if (launchInfo != null)
			{
				RunItem(launchInfo.Solution.RunBefore);
				foreach (var i in launchInfo.Solution.Items)
				{
					RunItem(i);
				}
				RunItem(launchInfo.Solution.RunAfter);
			}
		}
	}
}