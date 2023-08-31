using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSLXshared.Helpers
{
	public static class FileHelper
	{
		public static readonly string ExecutablesFilterString = "Executable files (*.exe)|*.exe|" +
																"Batch files (*.bat)|*.bat|" +
																"Command files (*.cmd)|*.cmd|" +
																"PowerShell files (*.ps1)|*.ps1|" +
																"All files (*.*)|*.*";

		public static readonly string SolutionFilterString    = "Solutions (*.sln)|*.sln|" +
																"C# Projects (*.csproj)|*.csproj|" +
																"F# Projects (*.fsproj)|*.fsproj|" +
																"TS/JS Projects (*.esproj, *.tsproj)|*.esproj|" +
																"Cxx Projects (*.vcxproj)|*.vcxproj|" +
																"All files (*.*)|*.*";


	}
}
