using System.Windows.Forms;

using Newtonsoft.Json;

using VSLauncher.DataModel;

namespace BackgroundLaunch
{
	/// <summary>
	/// The program.
	/// </summary>
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
				Console.WriteLine("no commandline arguments");
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
}