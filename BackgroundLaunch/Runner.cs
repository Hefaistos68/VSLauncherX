using System.ComponentModel;
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
		private readonly VisualStudioInstanceManager visualStudioInstances = new VisualStudioInstanceManager();
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
		/// <param name="allowWaitForCompletion">Whether <see cref="VsOptions.WaitForCompletion"/> waits for this process to exit.</param>
		/// <returns>The process</returns>
		private Process? RunItem(VsItem? item, bool allowWaitForCompletion = true)
		{
			// dont do anything if empty
			if (item is null)
			{
				return null;
			}

			ProcessStartInfo? startInfo = BuildStartInfo(item);

			if (startInfo is null)
			{
				return null;
			}

			Process? process = Process.Start(startInfo);

			if ((process != null) && item.PreferredMonitor.HasValue)
			{
				// move process to the given monitor
				MoveProcessToMonitor(process, item.PreferredMonitor.Value);
			}

			if (allowWaitForCompletion && item.WaitForCompletion)
			{
				WaitForExitSafe(process);
				process?.Dispose();

				return null;
			}

			if (process != null && IsExitedSafe(process))
			{
				process.Dispose();

				return null;
			}

			return process;
		}

		/// <summary>
		/// Builds a <see cref="ProcessStartInfo"/> for the given item.
		/// </summary>
		/// <param name="item">The item to launch.</param>
		/// <returns>The configured start info, or <c>null</c> if the item is not directly launchable.</returns>
		private ProcessStartInfo? BuildStartInfo(VsItem item)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo();
			string? workingPath = Path.GetDirectoryName(item.Path);

			// setup the working path
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

				startInfo.WorkingDirectory = workingPath;
			}

			startInfo.Verb = item.RunAsAdmin ? "runas" : string.Empty;

			// depending on item type, prepare executable and arguments
			if ((item.ItemType == ItemTypeEnum.Solution) || (item.ItemType == ItemTypeEnum.Project))
			{
				string targetFilename = GetTargetFilename(item);

				if (string.IsNullOrWhiteSpace(targetFilename))
				{
					throw new ExecutionException("No Visual Studio executable found.");
				}

				startInfo.UseShellExecute = item.RunAsAdmin;
				startInfo.ErrorDialog     = item.RunAsAdmin;
				startInfo.FileName        = targetFilename;
				startInfo.Arguments       = BuildVisualStudioArguments(item);

				return startInfo;
			}

			if (item.ItemType == ItemTypeEnum.Other)
			{
				if (string.IsNullOrWhiteSpace(item.Path))
				{
					return null;
				}

				startInfo.UseShellExecute = true;
				startInfo.ErrorDialog     = item.RunAsAdmin;
				startInfo.FileName        = item.Path;

				if (!string.IsNullOrEmpty(item.Commands))
				{
					startInfo.Arguments = item.Commands;
				}

				return startInfo;
			}

			return null;
		}

		/// <summary>
		/// Builds command-line arguments for launching a solution or project in Visual Studio.
		/// </summary>
		/// <param name="item">The item to launch.</param>
		/// <returns>The formatted argument string.</returns>
		private static string BuildVisualStudioArguments(VsItem item)
		{
			string arguments = "\"" + item.Path + "\"";

			if (!string.IsNullOrEmpty(item.Commands))
			{
				arguments += " " + item.Commands;
			}

			if (!item.ShowSplash)
			{
				arguments += " /NoSplash";
			}

			if (!string.IsNullOrWhiteSpace(item.Instance))
			{
				string trimmedInstance = item.Instance.Trim();
				arguments += trimmedInstance.Contains(' ', StringComparison.Ordinal)
					? $" /RootSuffix \"{trimmedInstance}\""
					: $" /RootSuffix {trimmedInstance}";
			}

			return arguments;
		}

		/// <summary>
		/// Gets the target filename, either the default VS instance, or the file specific one
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns>A string.</returns>
		private string GetTargetFilename(VsItem item)
		{
			if (string.IsNullOrEmpty(item.VsVersion))
			{
				return this.launchInfo.Target ?? this.visualStudioInstances.HighestVersion()?.Location ?? string.Empty;
			}

			return this.visualStudioInstances.GetByIdentifier(item.VsVersion)?.Location ?? this.launchInfo.Target ?? string.Empty;
		}

		/// <summary>
		/// Moves the process to monitor.
		/// </summary>
		/// <param name="process">The process.</param>
		/// <param name="preferredMonitor">The preferred monitor.</param>
		private static void MoveProcessToMonitor(Process process, int preferredMonitor)
		{
			if (preferredMonitor < 0 || preferredMonitor >= Screen.AllScreens.Length)
			{
				return;
			}

			var deadline = Stopwatch.StartNew();

			while (deadline.Elapsed < TimeSpan.FromSeconds(30) && !IsExitedSafe(process))
			{
				try
				{
					process.WaitForInputIdle(500);
				}
				catch (InvalidOperationException)
				{
					Thread.Sleep(100);
				}
				catch (Win32Exception)
				{
					return;
				}

				try
				{
					process.Refresh();

					if (process.MainWindowHandle != IntPtr.Zero)
					{
						var screens = Screen.AllScreens;

						if (preferredMonitor >= 0 && preferredMonitor < screens.Length)
						{
							SetWindowPos(process.MainWindowHandle,
								IntPtr.Zero,
								screens[preferredMonitor].WorkingArea.Left,
								screens[preferredMonitor].WorkingArea.Top,
								0, 0, SWP_NOSIZE | SWP_NOZORDER);
						}

						return;
					}
				}
				catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
				{
					return;
				}
			}
		}

		/// <summary>
		/// Safely checks whether a process has exited.
		/// </summary>
		/// <param name="process">The process to check.</param>
		/// <returns>True if exited or inaccessible.</returns>
		private static bool IsExitedSafe(Process process)
		{
			try
			{
				return process.HasExited;
			}
			catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
			{
				return true;
			}
		}

		/// <summary>
		/// Safely waits for a process to exit.
		/// </summary>
		/// <param name="process">The process to wait for.</param>
		private static void WaitForExitSafe(Process? process)
		{
			if (process is null)
			{
				return;
			}

			try
			{
				process.WaitForExit();
			}
			catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
			{
				Debug.WriteLine(ex);
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

			using var before = RunItem(item.RunBefore);
			using var main = RunItem(item);

			if (item is VsFolder f)
			{
				foreach (var i in f.Items)
				{
					RunAll(i);
				}
			}

			if (item.RunAfter?.WaitForCompletion == true)
			{
				WaitForExitSafe(main);
			}

			using var after = RunItem(item.RunAfter, allowWaitForCompletion: false);
		}
	}
}