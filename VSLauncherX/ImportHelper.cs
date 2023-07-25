using VSLauncher.DataModel;

namespace VSLauncher
{
	/// <summary>
	/// The import helper.
	/// </summary>
	public static class ImportHelper
	{
		public static VsItem GetItemFromExtension(string name, string path)
		{
			var ext = System.IO.Path.GetExtension(path)?.ToLower();

			if (ext == ".sln")
			{
				return new VsSolution(name, path);
			}
			else if (ext == ".csproj")
			{
				return new VsProject(name, path, eProjectType.CSProject);
			}
			else if (ext == ".vbproj")
			{
				return new VsProject(name, path, eProjectType.VBProject);
			}
			else if (ext == ".vcxproj")
			{
				return new VsProject(name, path, eProjectType.CPPProject);
			}
			else if (ext == ".vcproj")
			{
				return new VsProject(name, path, eProjectType.CPPProject);
			}
			else if (ext == ".fsproj")
			{
				return new VsProject(name, path, eProjectType.FSProject);
			}
			else if (ext == ".jsproj")
			{
				return new VsProject(name, path, eProjectType.JSProject);
			}
			else if (ext == ".tsproj")
			{
				return new VsProject(name, path, eProjectType.TSProject);
			}

			return new VsItem(name, path, null);
		}
	}
}
