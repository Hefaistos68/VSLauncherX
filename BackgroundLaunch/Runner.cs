using System.Diagnostics;

using VSLauncher.DataModel;

namespace BackgroundLaunch
{
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
			
			if(!string.IsNullOrWhiteSpace(workingPath))
			{
				if (!PathIsValid(workingPath))
				{
					throw new ExecutionException("Invalid working path: " + workingPath);
				}
			}

			startInfo.WorkingDirectory = workingPath;
			startInfo.Verb = item.RunAsAdmin ? "runas" : "run";
			startInfo.ErrorDialog = true;

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
				// startInfo.FileName = "explorer.exe";
				startInfo.FileName = item.Path;
				if (!string.IsNullOrEmpty(item.Commands))
				{
					startInfo.Arguments += item.Commands;
				}
			}

			process = Process.Start(startInfo);

			if (item.WaitForCompletion)
			{
				process?.WaitForExit();
				process?.Close();
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
				RunAll(launchInfo.Solution.Items.First());
			}
		}

		internal void RunAll(VsItem item)
		{
			// set the working path before executing the RunBefore item
			string? workingPath = Path.GetDirectoryName(item.Path);

			if (!string.IsNullOrWhiteSpace(workingPath))
			{
				if (!PathIsValid(workingPath))
				{
					throw new ExecutionException("Invalid working path: " + workingPath);
				}

				Environment.CurrentDirectory = workingPath;
			}

			RunItem(item.RunBefore);

			RunItem(item);
			if(item is VsFolder f)
			{
				foreach (var i in f.Items)
				{
					RunAll(i);
				}
			}

			RunItem(item.RunAfter);
		}
	}
}