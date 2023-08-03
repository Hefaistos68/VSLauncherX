using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// Structure that encapsulates basic information of icon embedded in a file.
	/// </summary>
	public struct EmbeddedIconInfo
	{
		public string FileName;
		public int IconIndex;
	}

	/// <summary>
	/// The file icons.
	/// </summary>
	public class FileIcons
	{
		private Dictionary<string, Icon?> iconsByName = new Dictionary<string, Icon?>();

		/// <summary>
		/// Initializes a new instance of the <see cref="FileIcons"/> class.
		/// </summary>
		public FileIcons(bool bLarge)
		{
			var all = RegisteredFileType.GetFileTypesAndIcon();

			foreach (var s in RegisteredFileType.listExtensionsOfInterest)
			{
				if (!all.ContainsKey(s))
				{
					all.Add(s, "");
				}
			}

			iconsByName.Add("CSProject", RegisteredFileType.ExtractIconFromFile(all[".csproj"], true));
			iconsByName.Add("VBProject", RegisteredFileType.ExtractIconFromFile(all[".vbproj"], true));
			iconsByName.Add("WebSite", RegisteredFileType.ExtractIconFromFile(all[".razor"], true));
			iconsByName.Add("FSProject", RegisteredFileType.ExtractIconFromFile(all[".fsproj"], true));
			iconsByName.Add("CPPProject", RegisteredFileType.ExtractIconFromFile(all[".vcxproj"], true));
			iconsByName.Add("JSProject", RegisteredFileType.ExtractIconFromFile(all[".esproj"], true));
			iconsByName.Add("TSProject", RegisteredFileType.ExtractIconFromFile(all[".esproj"], true));
			iconsByName.Add("Mixed", RegisteredFileType.ExtractIconFromFile(all[".sln"], true));
			iconsByName.Add("Solution", RegisteredFileType.ExtractIconFromFile(all[".sln"], true));

			iconsByName.Add("Warning", Resources.Warning);
			iconsByName.Add("Folder", Resources.Folder);
		}

		/// <summary>
		/// Gets the icon.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>An Icon.</returns>
		public Icon GetIcon(string name)
		{
			return iconsByName[name] ?? Resources.Warning;
		}

		/// <summary>
		/// Gets the icon.
		/// </summary>
		/// <param name="projectType">The project type.</param>
		/// <returns>An Icon.</returns>
		public Icon GetIcon(ProjectTypeEnum projectType)
		{
			return GetIcon(projectType.ToString());
		}
	}
	/// <summary>
	/// The registered file type.
	/// </summary>
	public class RegisteredFileType
	{
		public static List<string> listExtensionsOfInterest = new List<string> { ".sln", ".csproj", ".tsproj", ".esproj", ".vcxproj", ".fsproj", ".vbproj", ".razor" };

		#region APIs

		/// <summary>
		/// Destroys the icon.
		/// </summary>
		/// <param name="hIcon">The h icon.</param>
		/// <returns>An int.</returns>
		[DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
		private static extern unsafe int DestroyIcon(IntPtr hIcon);

		/// <summary>
		/// Extracts the icon.
		/// </summary>
		/// <param name="hInst">The h inst.</param>
		/// <param name="lpszExeFileName">The lpsz exe file name.</param>
		/// <param name="nIconIndex">The n icon index.</param>
		/// <returns>An IntPtr.</returns>
		[DllImport("shell32.dll", EntryPoint = "ExtractIconA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		private static extern IntPtr ExtractIcon(int hInst, string lpszExeFileName, int nIconIndex);

		/// <summary>
		/// Extracts the icon.
		/// </summary>
		/// <param name="szFileName">The sz file name.</param>
		/// <param name="nIconIndex">The n icon index.</param>
		/// <param name="phiconLarge">The phicon large.</param>
		/// <param name="phiconSmall">The phicon small.</param>
		/// <param name="nIcons">The n icons.</param>
		/// <returns>An uint.</returns>
		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		private static extern uint ExtractIconEx(string szFileName, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);
		#endregion APIs

		#region CORE METHODS

		/// <summary>
		/// Extract the icon from file.
		/// </summary>
		/// <param name="fileAndParam">The params string,
		/// such as ex: "C:\\Program Files\\NetMeeting\\conf.exe,1".</param>
		/// <returns>This method always returns the large size of the icon (may be 32x32 px).</returns>
		public static Icon? ExtractIconFromFile(string fileAndParam)
		{
			try
			{
				EmbeddedIconInfo embeddedIcon = getEmbeddedIconInfo(fileAndParam);

				//Gets the handle of the icon.
				IntPtr lIcon = ExtractIcon(0, embeddedIcon.FileName, embeddedIcon.IconIndex);

				//Gets the real icon.
				return Icon.FromHandle(lIcon);
			}
			catch
			{
			}

			return null;
		}

		/// <summary>
		/// Extract the icon from file.
		/// </summary>
		/// <param name="fileAndParam">The params string,
		/// such as ex: "C:\\Program Files\\NetMeeting\\conf.exe,1".</param>
		/// <param name="isLarge">
		/// Determines the returned icon is a large (may be 32x32 px)
		/// or small icon (16x16 px).</param>
		public static Icon? ExtractIconFromFile(string fileAndParam, bool isLarge)
		{
			if (string.IsNullOrWhiteSpace(fileAndParam))
			{
				return Resources.DefaultIcon;
			}

			unsafe
			{
				IntPtr[] hDummy = new IntPtr[1] { IntPtr.Zero };
				IntPtr[] hIconEx = new IntPtr[1] { IntPtr.Zero };

				try
				{
					EmbeddedIconInfo embeddedIcon = getEmbeddedIconInfo(fileAndParam);

					uint readIconCount = isLarge
		? ExtractIconEx(embeddedIcon.FileName, 0, hIconEx, hDummy, 1)
		: ExtractIconEx(embeddedIcon.FileName, 0, hDummy, hIconEx, 1);

					if (readIconCount > 0 && hIconEx[0] != IntPtr.Zero)
					{
						// Get first icon.
						Icon extractedIcon = (Icon)Icon.FromHandle(hIconEx[0]).Clone();

						return extractedIcon;
					}
					else // No icon read
					{
						return null;
					}
				}
				catch (Exception exc)
				{
					// Extract icon error.
					throw new ApplicationException("Could not extract icon", exc);
				}
				finally
				{
					// Release resources.
					foreach (IntPtr ptr in hIconEx)
						if (ptr != IntPtr.Zero)
							DestroyIcon(ptr);

					foreach (IntPtr ptr in hDummy)
						if (ptr != IntPtr.Zero)
							DestroyIcon(ptr);
				}
			}
		}

		/// <summary>
		/// Gets registered file types and their associated icon in the system.
		/// </summary>
		/// <returns>Returns a hash table which contains the file extension as keys, the icon file and param as values.</returns>
		public static Dictionary<string, string> GetFileTypesAndIcon()
		{
			Dictionary<string, string> iconsInfo = new Dictionary<string, string>();

			try
			{
				// Create a registry key object to represent the HKEY_CLASSES_ROOT registry section
				RegistryKey rkRoot = Registry.ClassesRoot;

				//Gets all sub keys' names.
				string[] keyNames = rkRoot.GetSubKeyNames();

				//Find the file icon.
				foreach (string keyName in keyNames)
				{
					if (String.IsNullOrEmpty(keyName))
						continue;

					if (!listExtensionsOfInterest.Contains(keyName))
						continue;

					RegistryKey? rkFileType = rkRoot.OpenSubKey(keyName);
					if (rkFileType == null)
						continue;

					//Gets the default value of this key that contains the information of file type.
					object? defaultValue = rkFileType.GetValue("");

					if (defaultValue == null)
					{
						// now try the OpenWithProgids subkey
						RegistryKey? rkOpenWithProgids = rkFileType.OpenSubKey("OpenWithProgids");

						if (rkOpenWithProgids == null)
						{
							// not installed, but we still want to know about it
							iconsInfo.Add(keyName, "");
							continue;
						}

						string[] rkOpenWithProgidsNames = rkOpenWithProgids.GetValueNames();

						if (rkOpenWithProgidsNames.Length > 0)
						{
							defaultValue = rkOpenWithProgidsNames[0];
						}
					}

					//Go to the key that specifies the default icon associates with this file type.
					string defaultIcon = defaultValue?.ToString() + "\\DefaultIcon";
					RegistryKey? rkFileIcon = rkRoot.OpenSubKey(defaultIcon);

					if (rkFileIcon != null)
					{
						//Get the file contains the icon and the index of the icon in that file.
						object? value = rkFileIcon.GetValue("");
						if (value != null)
						{
							//Clear all unecessary " sign in the string to avoid error.
							string fileParam = value!.ToString()!.Replace("\"", "");

							iconsInfo.Add(keyName, fileParam);
						}

						rkFileIcon.Close();
					}

					rkFileType.Close();
				}

				rkRoot.Close();
			}
			catch
			{
				// nothing we can do here
			}

			return iconsInfo;
		}
		#endregion CORE METHODS

		#region UTILITY METHODS

		/// <summary>
		/// Parses the parameters string to the structure of EmbeddedIconInfo.
		/// </summary>
		/// <param name="fileAndParam">The params string,
		/// such as ex: "C:\\Program Files\\NetMeeting\\conf.exe,1".</param>
		/// <returns></returns>
		protected static EmbeddedIconInfo getEmbeddedIconInfo(string fileAndParam)
		{
			EmbeddedIconInfo embeddedIcon = new EmbeddedIconInfo();

			if (String.IsNullOrEmpty(fileAndParam))
				return embeddedIcon;

			//Use to store the file contains icon.
			string fileName = String.Empty;

			//The index of the icon in the file.
			int iconIndex = 0;
			string iconIndexString = String.Empty;

			int commaIndex = fileAndParam.IndexOf(",");
			//if fileAndParam is some thing likes that: "C:\\Program Files\\NetMeeting\\conf.exe,1".
			if (commaIndex > 0)
			{
				fileName = fileAndParam.Substring(0, commaIndex);
				iconIndexString = fileAndParam.Substring(commaIndex + 1);
			}
			else
				fileName = fileAndParam;

			if (!String.IsNullOrEmpty(iconIndexString))
			{
				//Get the index of icon.
				iconIndex = int.Parse(iconIndexString);
				if (iconIndex < 0)
					iconIndex = 0;  //To avoid the invalid index.
			}

			embeddedIcon.FileName = fileName;
			embeddedIcon.IconIndex = iconIndex;

			return embeddedIcon;
		}

		#endregion UTILITY METHODS
	}
}