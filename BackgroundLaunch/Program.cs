using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

using Newtonsoft.Json;

using VSLauncher.DataModel;

using VSLXshared.Helpers;

namespace BackgroundLaunch
{
	/// <summary>
	/// The program.
	/// </summary>
	internal static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// To customize application configuration such as set high DPI settings or default font,
			// see https://aka.ms/applicationconfiguration.
			ApplicationConfiguration.Initialize();

			// get commandline parameters
			var args = Environment.GetCommandLineArgs();

			if (args.Length != 2)
			{
				Console.WriteLine("no commandline arguments");
				Application.Exit();

				return;
			}

			try
			{
				string json = DecodeLaunchPayload(args[1]);
				var item = JsonConvert.DeserializeObject<LaunchInfo>(json, FileHelper.CreateSafeJsonSettings());

				if (item != null)
				{
					new Runner(item).Run();
				}
			}
			catch (Exception ex) when (ex is JsonException or FormatException or ExecutionException)
			{
				Debug.WriteLine(ex.ToString());
			}

			Application.Exit();
		}

		/// <summary>
		/// Decodes the serialized launch info payload from either Base64 or legacy encoded command-line format.
		/// </summary>
		/// <param name="payload">The raw command-line payload.</param>
		/// <returns>The decoded JSON string.</returns>
		private static string DecodeLaunchPayload(string payload)
		{
			if (payload.StartsWith("b64:", StringComparison.Ordinal))
			{
				byte[] bytes = Convert.FromBase64String(payload.Substring(4));

				return Encoding.UTF8.GetString(bytes);
			}

			return payload.Replace('\u00AB', '\"').Replace('\u00BB', ' ');
		}
	}
}