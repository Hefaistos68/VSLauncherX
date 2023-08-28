using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Forms;

using VSLauncher.DataModel;
using VSLauncher.Helpers;

namespace BackgroundLaunch
{
	/// <summary>
	/// The runner class, responsible for executing the items
	/// </summary>
	public class Runner
	{
		private readonly LaunchInfo launchInfo;
		/// <summary>
		/// Sets the window pos.
		/// </summary>
		/// <param name="hWnd">The h wnd.</param>
		/// <param name="hWndInsertAfter">The h wnd insert after.</param>
		/// <param name="X">The x.</param>
		/// <param name="Y">The y.</param>
		/// <param name="cx">The cx.</param>
		/// <param name="cy">The cy.</param>
		/// <param name="uFlags">The u flags.</param>
		/// <returns>A bool.</returns>
		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

		private const int SWP_NOSIZE = 0x0001;
		private const int SWP_NOZORDER = 0x0004;

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
		/// <returns>The process</returns>
		private Process? RunItem(VsItem? item)
		{
			// dont do anything if empty
			if (item is null)
			{
				return null;
			}

			Process? process;
			ProcessStartInfo startInfo = new ProcessStartInfo();
			string? workingPath = Path.GetDirectoryName(item.Path);
			
			if(!string.IsNullOrWhiteSpace(workingPath))
			{
				if (!PathHelper.PathIsValid(workingPath))
				{
					throw new ExecutionException("Invalid working path: " + workingPath);
				}

				if (!PathHelper.CanRead(workingPath))
				{
					throw new ExecutionException("Access denied: " + workingPath);
				}
			}

			startInfo.WorkingDirectory = workingPath;
			startInfo.Verb = item.RunAsAdmin ? "runas" : "run";
			startInfo.ErrorDialog = true;

			if ((item.ItemType == ItemTypeEnum.Solution) || (item.ItemType == ItemTypeEnum.Project))
			{
				startInfo.FileName = this.launchInfo.Target;
				startInfo.Arguments = item.Path;
				if (!string.IsNullOrEmpty(item.Commands))
				{
					startInfo.Arguments += " " + item.Commands;
				}

				if (!item.ShowSplash)
				{
					startInfo.Arguments += " /NoSplash";
				}

				if (item.Instance != null)
				{
					startInfo.Arguments += " /RootSuffix " + item.Instance;
				}
			}
			else if(item.ItemType == ItemTypeEnum.Other)
			{
				// startInfo.FileName = "explorer.exe";
				startInfo.FileName = item.Path;
				if (!string.IsNullOrEmpty(item.Commands))
				{
					startInfo.Arguments += item.Commands;
				}
			}
			else
			{
				return null;
			}

			process = Process.Start(startInfo);

			if((process != null) && item.PreferredMonitor.HasValue)
			{
				// move process to the given monitor
				MoveProcessToMonitor(process, item.PreferredMonitor.Value);
			}

			if (item.WaitForCompletion)
			{
				process?.WaitForExit();
				process?.Close();
				process = null;
			}

			return process?.HasExited == true ? null : process;
		}

		/// <summary>
		/// Moves the process to monitor.
		/// </summary>
		/// <param name="process">The process.</param>
		/// <param name="preferredMonitor">The preferred monitor.</param>
		private static void MoveProcessToMonitor(Process process, int preferredMonitor)
		{
			while (!process.HasExited)
			{
				process.WaitForInputIdle(500);
				process.Refresh();

				if (process.MainWindowHandle != IntPtr.Zero)
				{
					if (Screen.AllScreens.Length >= preferredMonitor)
					{
						SetWindowPos(process.MainWindowHandle,
							IntPtr.Zero,
							Screen.AllScreens[preferredMonitor].WorkingArea.Left,
							Screen.AllScreens[preferredMonitor].WorkingArea.Top,
							0, 0, SWP_NOSIZE | SWP_NOZORDER);
					}

					return;
				}
			}
		}

		/// <summary>
		/// Runs the all items
		/// </summary>
		internal void Run()
		{
			if (launchInfo != null && launchInfo.Solution != null)
			{
				RunAll(launchInfo.Solution);
			}
		}

		/// <summary>
		/// Runs the all.
		/// </summary>
		/// <param name="item">The item.</param>
		internal void RunAll(VsItem item)
		{
			// set the working path before executing the RunBefore item
			string? workingPath = Path.GetDirectoryName(item.Path);

			if (!string.IsNullOrWhiteSpace(workingPath))
			{
				if (!PathHelper.PathIsValid(workingPath))
				{
					throw new ExecutionException("Invalid working path: " + workingPath);
				}

				if (!PathHelper.CanRead(workingPath))
				{
					throw new ExecutionException("Access denied: " + workingPath);
				}

				Environment.CurrentDirectory = workingPath;
			}

			var before = RunItem(item.RunBefore);

			var main = RunItem(item);
			if(item is VsFolder f)
			{
				foreach (var i in f.Items)
				{
					RunAll(i);
				}
			}

			if(item.RunAfter?.WaitForCompletion == true)
			{
				main?.WaitForExit();
			}

			RunItem(item.RunAfter);
		}
	}
}