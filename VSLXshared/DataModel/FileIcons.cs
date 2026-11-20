using System.Runtime.InteropServices;

using Microsoft.Win32;

using VSLauncher.Ide;

using VSLXshared;

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
		private static readonly Lazy<Icon?> InstalledVsCodeIcon = new(TryGetInstalledVsCodeIcon);

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

			iconsByName.Add("CSProject", SafeExtractIcon(".csproj", all[".csproj"], bLarge));
			iconsByName.Add("VBProject", SafeExtractIcon(".vbproj", all[".vbproj"], bLarge));
			iconsByName.Add("WebSite", SafeExtractIcon(".razor", all[".razor"], bLarge));
			iconsByName.Add("FSProject", SafeExtractIcon(".fsproj", all[".fsproj"], bLarge));
			iconsByName.Add("CPPProject", SafeExtractIcon(".vcxproj", all[".vcxproj"], bLarge));
			iconsByName.Add("JSProject", SafeExtractIcon(".esproj", all[".esproj"], bLarge));
			iconsByName.Add("TSProject", SafeExtractIcon(".esproj", all[".esproj"], bLarge));

			Icon solutionIcon = SafeExtractIcon(".sln", all[".sln"], bLarge);

			iconsByName.Add("Mixed", solutionIcon);
			iconsByName.Add("Solution", solutionIcon);
			iconsByName.Add("SolutionX", TryExtractIcon(".slnx", all[".slnx"], bLarge) ?? solutionIcon);
			iconsByName.Add("None", Resources.DefaultIcon);

			Icon codeWorkspaceIcon = TryExtractIcon(".code-workspace", all[".code-workspace"], bLarge)
				?? InstalledVsCodeIcon.Value
				?? Resources.DefaultIcon;

			iconsByName.Add("CodeWorkspace", codeWorkspaceIcon);
			iconsByName.Add("VsCodeProject", codeWorkspaceIcon);

			iconsByName.Add("Warning", Resources.Warning);
			iconsByName.Add("Folder", Resources.Folder);
		}

		/// <summary>
		/// Attempts to retrieve the application icon from the default installed Visual Studio Code instance.
		/// </summary>
		/// <returns>The VS Code <see cref="Icon"/>, or <c>null</c> if unavailable.</returns>
		private static Icon? TryGetInstalledVsCodeIcon()
		{
			try
			{
				return new VsCodeIdeProvider().DefaultInstance?.AppIcon;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Attempts to extract an icon from a registry descriptor or shell icon handler.
		/// </summary>
		/// <param name="extension">The file extension including leading dot.</param>
		/// <param name="fileAndParam">The registry icon descriptor.</param>
		/// <param name="bLarge">Whether to extract the large icon.</param>
		/// <returns>The extracted icon, or <c>null</c> if unavailable.</returns>
		private static Icon? TryExtractIcon(string extension, string fileAndParam, bool bLarge)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(fileAndParam))
				{
					Icon? extracted = RegisteredFileType.ExtractIconFromFile(fileAndParam, bLarge);

					if (extracted != null && !ReferenceEquals(extracted, Resources.DefaultIcon))
					{
						return extracted;
					}
				}

				return RegisteredFileType.ExtractShellIconForExtension(extension, bLarge);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Safely extracts an icon from a registered file type descriptor, falling back to the default icon.
		/// </summary>
		/// <param name="extension">The file extension including leading dot.</param>
		/// <param name="fileAndParam">The registry icon descriptor.</param>
		/// <param name="bLarge">Whether to extract the large icon.</param>
		/// <returns>The extracted icon or fallback icon.</returns>
		private static Icon SafeExtractIcon(string extension, string fileAndParam, bool bLarge)
		{
			return TryExtractIcon(extension, fileAndParam, bLarge) ?? Resources.DefaultIcon;
		}

		/// <summary>
		/// Gets the icon.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>An Icon.</returns>
		public Icon GetIcon(string name)
		{
			if (name != null && iconsByName.TryGetValue(name, out Icon? icon) && icon != null)
			{
				return icon;
			}

			return Resources.DefaultIcon;
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
		public static List<string> listExtensionsOfInterest = new List<string> { ".sln", ".slnx", ".code-workspace", ".csproj", ".tsproj", ".esproj", ".vcxproj", ".fsproj", ".vbproj", ".razor" };

		private const uint SHGFI_ICON              = 0x000000100u;
		private const uint SHGFI_LARGEICON         = 0x000000000u;
		private const uint SHGFI_SMALLICON         = 0x000000001u;
		private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010u;
		private const uint FILE_ATTRIBUTE_NORMAL   = 0x000000080u;

		#region APIs

		/// <summary>
		/// Receives information about a file object from SHGetFileInfo.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct SHFILEINFO
		{
			public IntPtr hIcon;
			public int iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szTypeName;
		}

		/// <summary>
		/// Retrieves information about an object in the file system, such as a file type icon.
		/// </summary>
		/// <param name="pszPath">The file path or dummy filename containing the extension.</param>
		/// <param name="dwFileAttributes">File attribute flags.</param>
		/// <param name="psfi">Receives the file information.</param>
		/// <param name="cbFileInfo">Size of the <see cref="SHFILEINFO"/> structure in bytes.</param>
		/// <param name="uFlags">Flags specifying the file information to retrieve.</param>
		/// <returns>Non-zero on success; <see cref="IntPtr.Zero"/> on failure.</returns>
		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

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
		[DllImport("shell32.dll", EntryPoint = "ExtractIconW", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
		private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

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
			IntPtr lIcon = IntPtr.Zero;

			try
			{
				EmbeddedIconInfo embeddedIcon = getEmbeddedIconInfo(fileAndParam);

				if (string.IsNullOrWhiteSpace(embeddedIcon.FileName))
				{
					return null;
				}

				//Gets the handle of the icon.
				lIcon = ExtractIcon(IntPtr.Zero, embeddedIcon.FileName, embeddedIcon.IconIndex);

				if (lIcon == IntPtr.Zero || lIcon == (IntPtr)1)
				{
					return null;
				}

				//Gets the real icon.
				using (Icon tempIcon = Icon.FromHandle(lIcon))
				{
					return (Icon)tempIcon.Clone();
				}
			}
			catch
			{
				return null;
			}
			finally
			{
				if (lIcon != IntPtr.Zero && lIcon != (IntPtr)1)
				{
					DestroyIcon(lIcon);
				}
			}
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

			IntPtr[] hDummy = new IntPtr[1] { IntPtr.Zero };
			IntPtr[] hIconEx = new IntPtr[1] { IntPtr.Zero };

			try
			{
				EmbeddedIconInfo embeddedIcon = getEmbeddedIconInfo(fileAndParam);

				if (string.IsNullOrWhiteSpace(embeddedIcon.FileName))
				{
					return Resources.DefaultIcon;
				}

				uint readIconCount = isLarge
					? ExtractIconEx(embeddedIcon.FileName, embeddedIcon.IconIndex, hIconEx, hDummy, 1)
					: ExtractIconEx(embeddedIcon.FileName, embeddedIcon.IconIndex, hDummy, hIconEx, 1);

				if (readIconCount > 0 && hIconEx[0] != IntPtr.Zero)
				{
					// Get first icon.
					using (Icon tempIcon = Icon.FromHandle(hIconEx[0]))
					{
						return (Icon)tempIcon.Clone();
					}
				}

				return null;
			}
			catch
			{
				return null;
			}
			finally
			{
				// Release resources.
				foreach (IntPtr ptr in hIconEx)
				{
					if (ptr != IntPtr.Zero)
					{
						DestroyIcon(ptr);
					}
				}

				foreach (IntPtr ptr in hDummy)
				{
					if (ptr != IntPtr.Zero)
					{
						DestroyIcon(ptr);
					}
				}
			}
		}

		/// <summary>
		/// Gets registered file types and their associated icon in the system.
		/// </summary>
		/// <returns>Returns a hash table which contains the file extension as keys, the icon file and param as values.</returns>
		public static Dictionary<string, string> GetFileTypesAndIcon()
		{
			Dictionary<string, string> iconsInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			try
			{
				// Create a registry key object to represent the HKEY_CLASSES_ROOT registry section
				RegistryKey rkRoot = Registry.ClassesRoot;

				//Find the file icon directly for each extension of interest.
				foreach (string keyName in listExtensionsOfInterest)
				{
					string fileParam = ResolveExtensionIconPath(rkRoot, keyName);
					iconsInfo[keyName] = fileParam;
				}
			}
			catch
			{
				// nothing we can do here
			}

			return iconsInfo;
		}

		/// <summary>
		/// Extracts the shell-registered icon for a file extension, including extensions backed by ShellEx IconHandler.
		/// </summary>
		/// <param name="extension">The file extension including leading dot (e.g. ".slnx").</param>
		/// <param name="isLarge">Determines whether to return the large (32x32 px) or small (16x16 px) icon.</param>
		/// <returns>A cloned <see cref="Icon"/> if registered and resolved; otherwise <c>null</c>.</returns>
		public static Icon? ExtractShellIconForExtension(string extension, bool isLarge)
		{
			if (string.IsNullOrWhiteSpace(extension) || !HasRegisteredIconSource(extension))
			{
				return null;
			}

			uint flags      = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES | (isLarge ? SHGFI_LARGEICON : SHGFI_SMALLICON);
			SHFILEINFO shfi = default;
			IntPtr result   = SHGetFileInfo("dummy" + extension, FILE_ATTRIBUTE_NORMAL, out shfi, (uint)Marshal.SizeOf<SHFILEINFO>(), flags);

			if (result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero)
			{
				return null;
			}

			try
			{
				using Icon tempIcon = Icon.FromHandle(shfi.hIcon);

				return (Icon)tempIcon.Clone();
			}
			catch
			{
				return null;
			}
			finally
			{
				DestroyIcon(shfi.hIcon);
			}
		}

		/// <summary>
		/// Checks whether a file extension has a registered DefaultIcon or ShellEx IconHandler in HKEY_CLASSES_ROOT.
		/// </summary>
		/// <param name="extension">The file extension to check.</param>
		/// <returns><c>true</c> if a registered icon source exists; otherwise <c>false</c>.</returns>
		private static bool HasRegisteredIconSource(string extension)
		{
			try
			{
				RegistryKey rkRoot = Registry.ClassesRoot;
				string? progId     = ResolveProgId(rkRoot, extension);

				if (string.IsNullOrWhiteSpace(progId))
				{
					return false;
				}

				using RegistryKey? rkIconHandler = rkRoot.OpenSubKey(progId + "\\ShellEx\\IconHandler");

				if (!string.IsNullOrWhiteSpace(rkIconHandler?.GetValue("")?.ToString()))
				{
					return true;
				}

				using RegistryKey? rkDefaultIcon = rkRoot.OpenSubKey(progId + "\\DefaultIcon");

				return !string.IsNullOrWhiteSpace(rkDefaultIcon?.GetValue("")?.ToString());
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Resolves the ProgID associated with a file extension in HKEY_CLASSES_ROOT.
		/// </summary>
		/// <param name="rkRoot">The HKEY_CLASSES_ROOT registry key.</param>
		/// <param name="extension">The file extension to query.</param>
		/// <returns>The ProgID string, or <c>null</c> if none is registered.</returns>
		private static string? ResolveProgId(RegistryKey rkRoot, string extension)
		{
			using RegistryKey? rkFileType = rkRoot.OpenSubKey(extension);

			if (rkFileType == null)
			{
				return null;
			}

			//Gets the default value of this key that contains the information of file type.
			string? progId = rkFileType.GetValue("")?.ToString();

			if (!string.IsNullOrWhiteSpace(progId))
			{
				return progId;
			}

			// now try the OpenWithProgids subkey
			using RegistryKey? rkOpenWithProgids = rkFileType.OpenSubKey("OpenWithProgids");

			if (rkOpenWithProgids != null)
			{
				string[] rkOpenWithProgidsNames = rkOpenWithProgids.GetValueNames();

				if (rkOpenWithProgidsNames.Length > 0)
				{
					return rkOpenWithProgidsNames[0];
				}
			}

			return null;
		}

		/// <summary>
		/// Resolves the DefaultIcon registry value for a single file extension.
		/// </summary>
		/// <param name="rkRoot">The HKEY_CLASSES_ROOT registry key.</param>
		/// <param name="extension">The file extension to query.</param>
		/// <returns>The icon path and optional index, or empty string if unavailable.</returns>
		private static string ResolveExtensionIconPath(RegistryKey rkRoot, string extension)
		{
			string? progId = ResolveProgId(rkRoot, extension);

			if (string.IsNullOrWhiteSpace(progId))
			{
				return string.Empty;
			}

			//Go to the key that specifies the default icon associates with this file type.
			string defaultIcon = progId + "\\DefaultIcon";
			using RegistryKey? rkFileIcon = rkRoot.OpenSubKey(defaultIcon);
			object? value = rkFileIcon?.GetValue("");

			if (value == null)
			{
				return string.Empty;
			}

			//Clear all unecessary " sign in the string to avoid error.
			return value.ToString()!.Replace("\"", "");
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
			{
				return embeddedIcon;
			}

			//Use to store the file contains icon.
			string fileName = String.Empty;

			//The index of the icon in the file.
			int iconIndex = 0;
			string iconIndexString = String.Empty;

			int commaIndex = fileAndParam.LastIndexOf(',');

			//if fileAndParam is some thing likes that: "C:\\Program Files\\NetMeeting\\conf.exe,1".
			if (commaIndex > 0)
			{
				fileName = fileAndParam.Substring(0, commaIndex).Trim();
				iconIndexString = fileAndParam.Substring(commaIndex + 1).Trim();
			}
			else
			{
				fileName = fileAndParam.Trim();
			}

			if (!String.IsNullOrEmpty(iconIndexString))
			{
				//Get the index or negative resource ID of icon.
				if (!int.TryParse(iconIndexString, out iconIndex))
				{
					iconIndex = 0;
				}
			}

			embeddedIcon.FileName = fileName;
			embeddedIcon.IconIndex = iconIndex;

			return embeddedIcon;
		}

		#endregion UTILITY METHODS
	}
}