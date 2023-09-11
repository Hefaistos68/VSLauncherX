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

			// add logic to start either normal, or as part of autostart, with or without elevation
			var args = Environment.GetCommandLineArgs();
			
			if((args.Length > 1 && args[1] == "autostart") || Debugger.IsAttached)
			{
				// we have command line arguments, so we are started as part of autostart
				Application.Run(new MainDialog());
			}
			else
			{
				if(args.Length > 1 && args[1] == "register")
				{
					// at this point the app was started with admin privs, so we can register the task
					UpdateTaskScheduler();
				}

				if (!Settings.Default.AlwaysAdmin)
				{
					Application.Run(new MainDialog());
				}
				else
				{
					// we are started normally
					AutoRun.Run();
				}
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