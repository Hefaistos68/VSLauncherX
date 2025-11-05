using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;
using VSLauncher.DataModel;
using VSLauncher.Helpers;
using VSLauncher.Properties;

namespace VSLauncher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	public static FileIcons VisualStudioFileIcons16 = new FileIcons(false);
	public static FileIcons VisualStudioFileIcons32 = new FileIcons(true);

	public static void ReapplyTheme()
	{
		Current.Dispatcher.Invoke(() =>
		{
			var dictionaries = Current.Resources.MergedDictionaries;
			if (dictionaries.Count > 0)
			{
				string pref = Settings.Default.PreferredTheme ?? "System";
				bool isDark = pref switch
				{
					"Dark" => true,
					"Light" => false,
					_ => IsSystemDarkMode()
				};
				dictionaries[0] = new ResourceDictionary
				{
					Source = new Uri(isDark
						? "/VSLauncherX2;component/Themes/DarkTheme.xaml"
						: "/VSLauncherX2;component/Themes/LightTheme.xaml", UriKind.Relative)
				};
			}
			// force redraw
			foreach (Window w in Current.Windows) w.InvalidateVisual();
		});
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		// Initialize file icons
		VisualStudioFileIcons16.GetIcon("Solution");
		VisualStudioFileIcons32.GetIcon("Solution");

		ApplyPreferredOrSystemTheme();
		SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

		// Handle command line arguments
		string? cmd = e.Args.Length > 0 ? e.Args[0] : null;
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
				Current.Shutdown();
				return;
			}

			bool bAdmin = AdminInfo.IsCurrentUserAdmin();
			bool bElevated = AdminInfo.IsElevated();

			if (!Settings.Default.AlwaysAdmin || bAdmin || bElevated)
			{
				runUi = true;
			}
			else if (!AutoRun.Run())
			{
				runUi = true;
			}
		}

		if (!runUi)
		{
			Current.Shutdown();
		}
	}

	private void SystemEvents_UserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
	{
		if (e.Category == UserPreferenceCategory.General && Settings.Default.PreferredTheme == "System")
		{
			ReapplyTheme();
		}
	}

	private static void ApplyPreferredOrSystemTheme() => ReapplyTheme();

	private static bool IsSystemDarkMode()
	{
		// Windows 10/11 registry key for AppsUseLightTheme (0 = dark, 1 = light)
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
			if (key?.GetValue("AppsUseLightTheme") is int lightTheme)
			{
				return lightTheme == 0; // 0 means dark mode
			}
		}
		catch { }
		
		return false;
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

