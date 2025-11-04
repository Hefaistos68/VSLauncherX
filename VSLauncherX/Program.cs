using System.Diagnostics;

using VSLauncher;
using VSLauncher.DataModel;
using VSLauncher.Helpers;
using VSLauncher.Properties;

namespace VSLauncher
{
	/// <summary>
	/// The program.
	/// </summary>
	internal static class Program
	{
		public static FileIcons VisualStudioFileIcons16 = new FileIcons(false);
		public static FileIcons VisualStudioFileIcons32 = new FileIcons(true);

		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// To customize application configuration such as set high DPI settings or default font,
			// see https://aka.ms/applicationconfiguration.
			ApplicationConfiguration.Initialize();

			VisualStudioFileIcons16.GetIcon("Solution");
			VisualStudioFileIcons32.GetIcon("Solution");

			// Consolidated logic to decide once whether to create and run the UI.
			var args = Environment.GetCommandLineArgs();
			string? cmd = args.Length > 1 ? args[1] : null;

			bool runUi = false;

			if ((cmd == "autostart") || Debugger.IsAttached)
			{
				runUi = true;
			}
			else
			{
				if (cmd == "register")
				{
					// Started with admin privileges: register the task
					UpdateTaskScheduler();
				}

				bool bAdmin = AdminInfo.IsCurrentUserAdmin();
				bool bElevated = AdminInfo.IsElevated();

				if (!Settings.Default.AlwaysAdmin || bAdmin || bElevated)
				{
					runUi = true;
				}
				else
				{
					// Attempt autorun; only show UI if autorun was not performed
					if (!AutoRun.Run())
					{
						runUi = true;
					}
				}
			}

			if (runUi)
			{
				Application.Run(new MainDialog());
			}
		}

		internal static void UpdateTaskScheduler()
		{
			AutoRun.SetupLauncherTask(Settings.Default.AlwaysAdmin, Settings.Default.AutoStart);
		}

		internal static void RemoveTaskScheduler()
		{
			AutoRun.RemoveLauncherTask();
		}
	}
}