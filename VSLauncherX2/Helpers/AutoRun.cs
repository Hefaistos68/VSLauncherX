using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32.TaskScheduler;

namespace VSLauncher.Helpers
{
	/// <summary>
	/// The auto run.
	/// </summary>
	public class AutoRun
	{
		private const string FolderName = "VSLauncherX";
		private const string TaskName = "AppStart for ";

		/// <summary>
		/// Initializes a new instance of the <see cref="AutoRun"/> class.
		/// </summary>
		public AutoRun()
		{
		}

		/// <summary>
		/// Setups the launcher task.
		/// </summary>
		/// <param name="bElevated">If true, start elevated.</param>
		/// <param name="asAutostart">If true, start as autostart.</param>
		public static void SetupLauncherTask(bool bElevated, bool asAutostart)
		{
			// Get the service on the local machine
			using (TaskService ts = new TaskService())
			{
				TaskFolder folder;
				var bFolderExists = ts.RootFolder.SubFolders.Any(sf => sf.Name == FolderName);

				if (!bFolderExists)
				{
					folder = ts.RootFolder.CreateFolder(FolderName);
				}
				else
				{
					folder = ts.RootFolder.SubFolders.First(sf => sf.Name == FolderName);
				}

				var user = System.Security.Principal.WindowsIdentity.GetCurrent();

				string location = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
				var execAction = new ExecAction(location, "autostart" ,workingDirectory: Path.GetDirectoryName(location));
				var taskName = TaskName + GetUserName(user);

				// create the task if it doesn't exist
				if (folder.AllTasks.Any(t => t.Name == taskName))
				{
					folder.DeleteTask(taskName);
				}

				// Create a new task definition and assign properties
				TaskDefinition td = ts.NewTask();
				td.RegistrationInfo.Author = user.Name;
				td.RegistrationInfo.Description = "Visual Studio Launcher";

				td.Principal.RunLevel = bElevated ? TaskRunLevel.Highest : TaskRunLevel.LUA;
				td.Principal.LogonType = TaskLogonType.InteractiveToken;

				td.Settings.StartWhenAvailable = true;
				td.Settings.AllowDemandStart = true;
				td.Settings.IdleSettings.StopOnIdleEnd = false;
				td.Settings.DisallowStartIfOnBatteries = false;
				td.Settings.StopIfGoingOnBatteries = false;
				td.Settings.ExecutionTimeLimit = TimeSpan.Zero;
				td.Settings.AllowHardTerminate = true;
				td.Settings.MultipleInstances = TaskInstancesPolicy.Parallel;

				if (asAutostart)
				{
					// Create a trigger that will fire the task when the user logs on
					var logonTrigger = new LogonTrigger
					{
						UserId = user.User.ToString(),
						Delay = TimeSpan.FromSeconds(10)	// give task manager time to start too
					};

					td.Triggers.Add(logonTrigger);
				}

				td.Actions.Add(execAction);

				// Register the task in the root folder
				folder.RegisterTaskDefinition(taskName, td);
			}
		}

		/// <summary>
		/// Gets a simple user name.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>A string.</returns>
		private static string GetUserName(WindowsIdentity user)
		{
			return user.Name.Replace("\\", "_").Replace(".", "_");
		}

		/// <summary>
		/// Runs the.
		/// </summary>
		internal static bool Run()
		{
			// Get the service on the local machine
			using (TaskService ts = new TaskService())
			{
				TaskFolder folder;
				var bFolderExists = ts.RootFolder.SubFolders.Any(sf => sf.Name == FolderName);

				if (!bFolderExists)
				{
					throw new Exception("Task Folder not found");
				}
				else
				{
					folder = ts.RootFolder.SubFolders.First(sf => sf.Name == FolderName);
				}
				var taskName = TaskName + GetUserName(System.Security.Principal.WindowsIdentity.GetCurrent());

				if (folder.AllTasks.Any(t => t.Name == taskName))
				{
					folder.Tasks[taskName].Run();
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Removes the launcher task.
		/// </summary>
		internal static void RemoveLauncherTask()
		{
			using (TaskService ts = new TaskService())
			{
				TaskFolder folder;
				var bFolderExists = ts.RootFolder.SubFolders.Any(sf => sf.Name == FolderName);

				if (!bFolderExists)
				{
					return;
				}

				folder = ts.RootFolder.SubFolders.First(sf => sf.Name == FolderName);

				var user = System.Security.Principal.WindowsIdentity.GetCurrent();
				var taskName = TaskName + GetUserName(user);

				// create the task if it doesn't exist
				if (folder.AllTasks.Any(t => t.Name == taskName))
				{
					folder.DeleteTask(taskName);
				}
			}
		}
	}
}
