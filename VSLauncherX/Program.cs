using VSLauncher;
using VSLauncher.DataModel;

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

			Application.Run(new MainDialog());
		}

		private static void Test()
		{
		}
	}
}