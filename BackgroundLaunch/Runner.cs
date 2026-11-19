using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using VSLauncher.DataModel;
using VSLauncher.Helpers;
using VSLauncher.Ide;

namespace BackgroundLaunch
{
	/// <summary>
	/// The runner class, responsible for executing the items
	/// </summary>
	public class Runner
	{
		private static readonly IdeType[] LegacyTargetLookupOrder = { IdeType.VisualStudio, IdeType.VsCode };

		private readonly IdeManager ideManager;
		private readonly Func<ProcessStartInfo, Process?> processStarter;
		private readonly LaunchInfo launchInfo;
		private readonly Dictionary<IdeType, string> defaultTargets;

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
		/// <param name="item">The launch configuration payload.</param>
		/// <param name="ideManager">Optional IDE manager used to resolve target instances and build start info.</param>
		/// <param name="processStarter">Optional process launcher delegate; defaults to <see cref="Process.Start(ProcessStartInfo)"/>.</param>
		public Runner(
			LaunchInfo item,
			IdeManager? ideManager = null,
			Func<ProcessStartInfo, Process?>? processStarter = null)
		{
			ArgumentNullException.ThrowIfNull(item);

			this.launchInfo     = item;
			this.ideManager     = ideManager ?? new IdeManager();
			this.processStarter = processStarter ?? Process.Start;
			this.defaultTargets = BuildDefaultTargetsDictionary(item, this.ideManager);
		}

		/// <summary>
		/// Resolves the working directory for a file or directory target path.
		/// </summary>
		/// <param name="path">The target file or directory path.</param>
		/// <returns>The directory path itself when <paramref name="path"/> is an existing directory, its parent directory otherwise, or <c>null</c> when empty.</returns>
		internal static string? ResolveWorkingDirectory(string? path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return null;
			}

			if (Directory.Exists(path))
			{
				return path;
			}

			return Path.GetDirectoryName(path);
		}

		/// <summary>
		/// Runs a single item
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="allowWaitForCompletion">Whether <see cref="VsOptions.WaitForCompletion"/> waits for this process to exit.</param>
		/// <param name="waitForExit">Whether to request wait semantics when building IDE start info.</param>
		/// <returns>The process</returns>
		private Process? RunItem(VsItem? item, bool allowWaitForCompletion = true, bool waitForExit = false)
		{
			// dont do anything if empty
			if (item is null)
			{
				return null;
			}

			bool effectiveWait          = waitForExit || (allowWaitForCompletion && item.WaitForCompletion);
			ProcessStartInfo? startInfo = BuildStartInfo(item, effectiveWait);

			if (startInfo is null)
			{
				return null;
			}

			Process? process = this.processStarter(startInfo);

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
		/// <param name="waitForExit">Whether the launched IDE process should wait until closed.</param>
		/// <returns>The configured start info, or <c>null</c> if the item is not directly launchable.</returns>
		internal ProcessStartInfo? BuildStartInfo(VsItem item, bool waitForExit = false)
		{
			ArgumentNullException.ThrowIfNull(item);

			if (item is VsFolder folder && folder.ItemType == ItemTypeEnum.Folder)
			{
				if (!ShouldLaunchFolderAsWorkspace(folder))
				{
					return null;
				}

				return BuildIdeStartInfo(folder, waitForExit);
			}

			if (item.ItemType == ItemTypeEnum.Solution || item.ItemType == ItemTypeEnum.Project)
			{
				return BuildIdeStartInfo(item, waitForExit);
			}

			if (item.ItemType == ItemTypeEnum.Other)
			{
				return BuildOtherStartInfo(item);
			}

			return null;
		}

		/// <summary>
		/// Converts serialized per-IDE default targets into a lookup dictionary, applying legacy <see cref="LaunchInfo.Target"/> fallback where missing.
		/// </summary>
		/// <param name="launchInfo">The launch configuration containing explicit and legacy target settings.</param>
		/// <param name="ideManager">The IDE manager used to match a legacy target path against installed instances.</param>
		/// <returns>A dictionary mapping each IDE family to its default instance identifier.</returns>
		private static Dictionary<IdeType, string> BuildDefaultTargetsDictionary(LaunchInfo launchInfo, IdeManager ideManager)
		{
			var map = new Dictionary<IdeType, string>();

			if (launchInfo.DefaultTargets is not null)
			{
				foreach (IdeDefaultTarget target in launchInfo.DefaultTargets)
				{
					if (target is not null && !string.IsNullOrWhiteSpace(target.TargetIdentifier))
					{
						map[target.IdeType] = target.TargetIdentifier;
					}
				}
			}

			ApplyLegacyTargetFallback(map, launchInfo.Target, ideManager);

			return map;
		}

		/// <summary>
		/// Populates an IDE default target identifier from a legacy <see cref="LaunchInfo.Target"/> path when no explicit default target exists for the matching IDE.
		/// </summary>
		/// <param name="map">The default targets map to populate.</param>
		/// <param name="legacyTarget">The optional legacy executable path.</param>
		/// <param name="ideManager">The IDE manager providing installed instances.</param>
		private static void ApplyLegacyTargetFallback(
			Dictionary<IdeType, string> map,
			string? legacyTarget,
			IdeManager ideManager)
		{
			if (string.IsNullOrWhiteSpace(legacyTarget))
			{
				return;
			}

			string trimmedTarget = legacyTarget.Trim();

			foreach (IdeType ideType in LegacyTargetLookupOrder)
			{
				if (map.ContainsKey(ideType))
				{
					continue;
				}

				IIdeInstance? matched = ideManager
					.GetInstances(ideType)
					.FirstOrDefault(i => string.Equals(i.Location, trimmedTarget, StringComparison.OrdinalIgnoreCase));

				if (matched is not null && !string.IsNullOrWhiteSpace(matched.Identifier))
				{
					map[ideType] = matched.Identifier;

					return;
				}
			}
		}

		/// <summary>
		/// Determines whether a <see cref="VsFolder"/> should launch directly in VS Code as a workspace-backed folder.
		/// </summary>
		/// <param name="folder">The folder to inspect.</param>
		/// <returns><c>true</c> if the folder is workspace-backed and VS Code is installed; otherwise <c>false</c>.</returns>
		private bool ShouldLaunchFolderAsWorkspace(VsFolder folder)
		{
			return folder.ItemType == ItemTypeEnum.Folder &&
				folder.IsWorkspaceBacked() &&
				this.ideManager.HasInstalled(IdeType.VsCode);
		}

		/// <summary>
		/// Resolves an IDE provider for the item and builds a configured <see cref="ProcessStartInfo"/>.
		/// </summary>
		/// <param name="item">The launchable IDE item.</param>
		/// <param name="waitForExit">Whether the launched IDE process should wait until closed.</param>
		/// <returns>The configured start info, or <c>null</c> if no compatible IDE is installed.</returns>
		private ProcessStartInfo? BuildIdeStartInfo(VsItem item, bool waitForExit)
		{
			IIdeInstance? instance = this.ideManager.ResolveInstance(item, this.defaultTargets);

			if (instance is null)
			{
				Debug.WriteLine($"Skipping '{item.Name}' ({item.IdeType}): no compatible IDE installation resolved.");

				return null;
			}

			IIdeProvider? provider = this.ideManager.GetProvider(instance.IdeType);

			if (provider is null)
			{
				Debug.WriteLine($"Skipping '{item.Name}' ({instance.IdeType}): provider not registered.");

				return null;
			}

			ProcessStartInfo? startInfo = provider.BuildStartInfo(item, instance, waitForExit: waitForExit);

			if (startInfo is null)
			{
				return null;
			}

			ApplyWorkingDirectoryIfAccessible(startInfo, item.Path);

			return startInfo;
		}

		/// <summary>
		/// Builds a shell-execute <see cref="ProcessStartInfo"/> for custom scripts or executables.
		/// </summary>
		/// <param name="item">The <see cref="ItemTypeEnum.Other"/> item to launch.</param>
		/// <returns>The configured start info, or <c>null</c> if the item has no path.</returns>
		private static ProcessStartInfo? BuildOtherStartInfo(VsItem item)
		{
			if (string.IsNullOrWhiteSpace(item.Path))
			{
				return null;
			}

			var startInfo = new ProcessStartInfo
			{
				UseShellExecute = true,
				ErrorDialog     = item.RunAsAdmin,
				Verb            = item.RunAsAdmin ? "runas" : string.Empty,
				FileName        = item.Path
			};

			ApplyWorkingDirectoryIfAccessible(startInfo, item.Path);

			if (!string.IsNullOrEmpty(item.Commands))
			{
				startInfo.Arguments = item.Commands;
			}

			return startInfo;
		}

		/// <summary>
		/// Sets <see cref="ProcessStartInfo.WorkingDirectory"/> when the resolved directory exists and is readable.
		/// </summary>
		/// <param name="startInfo">The start info to update.</param>
		/// <param name="itemPath">The item target path.</param>
		private static void ApplyWorkingDirectoryIfAccessible(ProcessStartInfo startInfo, string? itemPath)
		{
			string? workingPath = ResolveWorkingDirectory(itemPath);

			if (PathHelper.PathIsValidAndCanRead(workingPath))
			{
				startInfo.WorkingDirectory = workingPath;
			}
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
			if (this.launchInfo.Solution != null)
			{
				RunAll(this.launchInfo.Solution);
			}
		}

		/// <summary>
		/// Runs the all.
		/// </summary>
		/// <param name="item">The item.</param>
		internal void RunAll(VsItem item)
		{
			ArgumentNullException.ThrowIfNull(item);

			string? previousDirectory = SetWorkingDirectoryIfAccessible(item.Path);

			try
			{
				using Process? before = RunItem(item.RunBefore);
				bool waitForRunAfter  = item.RunAfter?.WaitForCompletion == true;
				using Process? main   = ExecuteMainOrChildren(item, waitForRunAfter);

				if (waitForRunAfter)
				{
					WaitForExitSafe(main);
				}

				using Process? after = RunItem(item.RunAfter, allowWaitForCompletion: false);
			}
			finally
			{
				RestoreWorkingDirectory(previousDirectory);
			}
		}

		/// <summary>
		/// Launches an item directly or recurses through its child items for non-workspace folders.
		/// </summary>
		/// <param name="item">The item or folder being executed.</param>
		/// <param name="waitForRunAfter">Whether a subsequent <c>RunAfter</c> item requires waiting for the main process.</param>
		/// <returns>The launched main process, if any.</returns>
		private Process? ExecuteMainOrChildren(VsItem item, bool waitForRunAfter)
		{
			if (item is VsFolder folder && folder.ItemType is ItemTypeEnum.Folder or ItemTypeEnum.VisualStudio)
			{
				if (ShouldLaunchFolderAsWorkspace(folder))
				{
					return RunItem(folder, waitForExit: waitForRunAfter);
				}

				foreach (VsItem child in folder.Items)
				{
					RunAll(child);
				}

				return null;
			}

			return RunItem(item, waitForExit: waitForRunAfter);
		}

		/// <summary>
		/// Updates <see cref="Environment.CurrentDirectory"/> to the resolved working directory when valid and readable.
		/// </summary>
		/// <param name="itemPath">The target path of the item about to run.</param>
		/// <returns>The previous working directory when changed; otherwise <c>null</c>.</returns>
		private static string? SetWorkingDirectoryIfAccessible(string? itemPath)
		{
			string? workingPath = ResolveWorkingDirectory(itemPath);

			if (!PathHelper.PathIsValidAndCanRead(workingPath))
			{
				return null;
			}

			string previous = Environment.CurrentDirectory;

			Environment.CurrentDirectory = workingPath!;

			return previous;
		}

		/// <summary>
		/// Restores <see cref="Environment.CurrentDirectory"/> if a previous directory was saved and still exists.
		/// </summary>
		/// <param name="previousDirectory">The directory path to restore, or <c>null</c>.</param>
		private static void RestoreWorkingDirectory(string? previousDirectory)
		{
			if (!string.IsNullOrEmpty(previousDirectory) && Directory.Exists(previousDirectory))
			{
				Environment.CurrentDirectory = previousDirectory;
			}
		}
	}
}