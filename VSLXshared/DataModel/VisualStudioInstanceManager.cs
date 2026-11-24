using System;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using Newtonsoft.Json;
using Microsoft.VisualStudio.Setup.Configuration;

using VSLauncher.Helpers;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// The visual studio instance manager.
	/// </summary>
	public class VisualStudioInstanceManager
	{
		private static readonly Regex vsDirectoryRegex = new(@"^(\d+\.\d+)_([0-9a-fA-F]{8})(.*)$", RegexOptions.Compiled);

		private static Dictionary<string, string> vsVersions = new Dictionary<string, string>()
		{
			{ "7", "2003" },
			{ "8", "2005" },
			{ "9", "2008" },
			{ "10", "2010" },
			{ "11", "2012" },
			{ "12", "2013" },
			{ "14", "2015" },
			{ "15", "2017" },
			{ "16", "2019" },
			{ "17", "2022" },
			{ "18", "2026" }
		};

		private List<VisualStudioInstance> allInstances;

		/// <summary>
		/// Initializes a new instance of the <see cref="VisualStudioInstanceManager"/> class.
		/// </summary>
		public VisualStudioInstanceManager()
		{
			allInstances = ReadAllInstances() ?? new List<VisualStudioInstance>(); // just to not crash if no instances are found
		}

		/// <summary>
		/// Gets the all.
		/// </summary>
		public List<VisualStudioInstance> All
		{
			get { return allInstances; }
		}

		/// <summary>
		/// Gets the count.
		/// </summary>
		public int Count
		{
			get
			{
				return allInstances.Count;
			}
		}

		/// <summary>
		/// Gets the installer path.
		/// </summary>
		public static string InstallerPath
		{
			get
			{
				string location = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio", "Installer");
				string location86 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer");
				string installerPath = Path.Combine(location, "vs_installer.exe");
				string installerPath86 = Path.Combine(location86, "vs_installer.exe");

				if (Directory.Exists(location) || Directory.Exists(location86))
				{
					if (File.Exists(installerPath))
					{
						return installerPath;
					}
					if (File.Exists(installerPath86))
					{
						return installerPath86;
					}
				}

				// not found, return the microsoft download site for Visual Studio
				return "https://visualstudio.microsoft.com/downloads/";
			}
		}

		/// <summary>
		/// Access an instance by index
		/// </summary>
		public VisualStudioInstance this[int index]
		{
			get
			{
				return allInstances[index];
			}
		}

		/// <summary>
		/// Access an instance by its version number
		/// </summary>
		public VisualStudioInstance? this[string version]
		{
			get
			{
				return allInstances.FirstOrDefault(x => x.Version.StartsWith(version, StringComparison.OrdinalIgnoreCase));
			}
		}

		/// <summary>
		/// Reads the all installed Visual Studio instances, using either the original WMI class, the the WMI class in the new namespace, or finally the VS Setup API
		/// </summary>
		/// <returns>A list of VisualStudioInstances.</returns>
		public static List<VisualStudioInstance>? ReadAllInstances()
		{
			List<VisualStudioInstance>? list = null;

			try
			{
				list = ReadAllInstancesFromWMI1();
			}
			catch (System.Exception ex)
			{
				Debug.WriteLine($"Original MSFT_VSInstance WMi class not found or not able to read. ({ex.Message})");
			}

			if (list is null || list.Count == 0)
			{
				try
				{
					list = ReadAllInstancesFromWMI2();
				}
				catch (System.Exception ex)
				{
					Debug.WriteLine($"New MSFT_VSInstance WMi class not found or not able to read. ({ex.Message})");
				}
			}

			if (list is null || list.Count == 0)
			{
				try
				{
					list = ReadAllInstancesFromSetupApi();
				}
				catch (System.Exception ex)
				{
					Debug.WriteLine($"failed to read from VS Setup API, no Visual Studio installation information available. ({ex.Message})");
				}
			}

			return list;
		}

		/// <summary>
		/// Reads the all installed Visual Studio instances from WMI
		/// </summary>
		/// <returns>A list of VisualStudioInstances.</returns>
		public static List<VisualStudioInstance> ReadAllInstancesFromWMI1()
		{
			// read all data from WMI using CimInstance MSFT_VSInstance
			// https://docs.microsoft.com/en-us/windows/win32/wmisdk/msft-vsinstance
			using ManagementObjectSearcher searcher = new ManagementObjectSearcher
			{
				Query = new SelectQuery("MSFT_VSInstance ", "", new[] { "Name", "Version", "ProductLocation", "IdentifyingNumber" })
			};

			return ReadInstancesFromSearcher(searcher);
		}

		/// <summary>
		/// Reads all installed Visual Studio instances from the root/cimv2/vs WMI namespace.
		/// </summary>
		/// <returns>A list of VisualStudioInstances.</returns>
		public static List<VisualStudioInstance> ReadAllInstancesFromWMI2()
		{
			// read all data from WMI using CimInstance MSFT_VSInstance but in the new VS namespace
			ManagementScope scope = new ManagementScope("root/cimv2/vs");
			using ManagementObjectSearcher searcher = new ManagementObjectSearcher
			{
				Query = new SelectQuery("MSFT_VSInstance ", "", new[] { "Name", "Version", "ProductLocation", "IdentifyingNumber" }),
				Scope = scope
			};

			return ReadInstancesFromSearcher(searcher);
		}

		/// <summary>
		/// Enumerates WMI query results into sorted <see cref="VisualStudioInstance"/> items.
		/// </summary>
		/// <param name="searcher">The configured WMI searcher.</param>
		/// <returns>A sorted list of instances.</returns>
		private static List<VisualStudioInstance> ReadInstancesFromSearcher(ManagementObjectSearcher searcher)
		{
			var list = new List<VisualStudioInstance>();

			using ManagementObjectCollection collection = searcher.Get();
			using ManagementObjectCollection.ManagementObjectEnumerator em = collection.GetEnumerator();

			while (em.MoveNext())
			{
				ManagementBaseObject baseObj = em.Current;

				if (baseObj.Properties["Version"].Value != null)
				{
					try
					{
						string? name       = baseObj.Properties["Name"].Value?.ToString();
						string? version    = baseObj.Properties["Version"].Value?.ToString();
						string? location   = baseObj.Properties["ProductLocation"].Value?.ToString();
						string? identifier = baseObj.Properties["IdentifyingNumber"].Value?.ToString();

						if (name != null && version != null && location != null && identifier != null && IsLaunchableVisualStudioExecutable(location))
						{
							string trimmedLocation = location.Trim();
							string qualifiedName   = QualifyDisplayName(name, trimmedLocation);
							string year            = VisualStudioInstanceManager.YearFromVersion(ExtractMajorVersion(version));

							list.Add(new VisualStudioInstance(qualifiedName, version, trimmedLocation, identifier, year));
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex.ToString());
					}
				}
			}

			list.Sort(CompareInstances);

			return list;
		}

		private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

		/// <summary>
		/// Reads the all instances from VS setup api.
		/// </summary>
		/// <returns>A list of VisualStudioInstances.</returns>
		private static List<VisualStudioInstance> ReadAllInstancesFromSetupApi()
		{
			List<VisualStudioInstance> list = new();

			try
			{
				var query = new SetupConfiguration();
				var query2 = (ISetupConfiguration2)query;
				var e = query2.EnumAllInstances();

				int fetched;
				var instances = new ISetupInstance[1];
				do
				{
					e.Next(1, instances, out fetched);

					if (fetched > 0)
					{
						var instance = instances[0];
						var instance2 = (ISetupInstance2)instance;
						var state = instance2.GetState();

						string? name = ((state & InstanceState.Registered) == InstanceState.Registered) ? instance2.GetProduct().GetId() : null;
						string? version = instance.GetInstallationVersion();
						string? location = ((state & InstanceState.Local) == InstanceState.Local) ? instance2.GetInstallationPath() : null;
						string? identifier = instance2.GetInstanceId();

						if (name != null && version != null && location != null && identifier != null)
						{
							location = Path.Combine(location, "Common7", "IDE", "devenv.exe");

							if (IsLaunchableVisualStudioExecutable(location))
							{
								string year          = VisualStudioInstanceManager.YearFromVersion(ExtractMajorVersion(version));
								string qualifiedName = QualifyDisplayName(NormalizeName(name, year), location);

								list.Add(new VisualStudioInstance(qualifiedName, version, location, identifier, year));
							}
						}
					}
				}
				while (fetched > 0);
			}
			catch (COMException ex) when (ex.HResult == REGDB_E_CLASSNOTREG)
			{
				Debug.WriteLine("The query API is not registered. Assuming no instances are installed.");
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error 0x{ex.HResult:x8}: {ex.Message}");
			}

			list.Sort(CompareInstances);

			return list;
		}

		/// <summary>
		/// Determines whether <paramref name="location"/> points to an existing Visual Studio <c>devenv.exe</c> executable.
		/// </summary>
		/// <param name="location">The candidate product location path.</param>
		/// <param name="fileExists">Optional file existence predicate for unit testing.</param>
		/// <returns><c>true</c> if <paramref name="location"/> is an existing <c>devenv.exe</c>; otherwise <c>false</c>.</returns>
		public static bool IsLaunchableVisualStudioExecutable(string? location, Func<string, bool>? fileExists = null)
		{
			if (string.IsNullOrWhiteSpace(location))
			{
				return false;
			}

			string trimmed            = location.Trim();
			string fileName           = Path.GetFileName(trimmed);
			Func<string, bool> exists = fileExists ?? File.Exists;

			if (!string.Equals(fileName, "devenv.exe", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			return exists(trimmed);
		}

		/// <summary>
		/// Qualifies a Visual Studio display name with its release channel (<c>Insiders</c> or <c>Preview</c>) when inferred from <paramref name="location"/> and not already present.
		/// </summary>
		/// <param name="name">The base display name.</param>
		/// <param name="location">The path to <c>devenv.exe</c>.</param>
		/// <returns>The disambiguated display name.</returns>
		public static string QualifyDisplayName(string name, string location)
		{
			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(location))
			{
				return name;
			}

			if (name.Contains("Insiders", StringComparison.OrdinalIgnoreCase)
				|| name.Contains("Preview", StringComparison.OrdinalIgnoreCase))
			{
				return name;
			}

			if (location.Contains(@"\Insiders\", StringComparison.OrdinalIgnoreCase)
				|| location.Contains("/Insiders/", StringComparison.OrdinalIgnoreCase))
			{
				return $"{name} Insiders";
			}

			if (location.Contains(@"\Preview\", StringComparison.OrdinalIgnoreCase)
				|| location.Contains("/Preview/", StringComparison.OrdinalIgnoreCase))
			{
				return $"{name} Preview";
			}

			return name;
		}

		/// <summary>
		/// Compares two <see cref="VisualStudioInstance"/> instances deterministically by version, display name, and identifier.
		/// </summary>
		/// <param name="x">The first instance.</param>
		/// <param name="y">The second instance.</param>
		/// <returns>A signed integer indicating relative sort order.</returns>
		public static int CompareInstances(VisualStudioInstance x, VisualStudioInstance y)
		{
			if (ReferenceEquals(x, y))
			{
				return 0;
			}

			int versionComparison = CompareVersionStrings(x.Version, y.Version);

			if (versionComparison != 0)
			{
				return versionComparison;
			}

			int nameComparison = string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

			if (nameComparison != 0)
			{
				return nameComparison;
			}

			return string.Compare(x.Identifier, y.Identifier, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Compares two Visual Studio version strings numerically when possible.
		/// </summary>
		/// <param name="x">The first version string.</param>
		/// <param name="y">The second version string.</param>
		/// <returns>Comparison result suitable for sorting.</returns>
		private static int CompareVersionStrings(string x, string y)
		{
			bool parsedX = Version.TryParse(x, out Version? vx);
			bool parsedY = Version.TryParse(y, out Version? vy);

			if (parsedX && parsedY && vx != null && vy != null)
			{
				return vx.CompareTo(vy);
			}

			return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Extracts the major version segment from a dotted version string.
		/// </summary>
		/// <param name="version">The version string.</param>
		/// <returns>The major version segment.</returns>
		private static string ExtractMajorVersion(string version)
		{
			int dotIndex = version.IndexOf('.');

			return dotIndex > 0 ? version.Substring(0, dotIndex) : version;
		}

		private static string NormalizeName(string name, string year)
		{
			return name.Replace("Microsoft.Visual", "Visual ").Replace(".Product.", " ") + " " + year;
		}

		/// <summary>
		/// Gets the version number from the year
		/// </summary>
		/// <param name="year">The year.</param>
		/// <returns>A string.</returns>
		public static string VersionFromYear(string year)
		{
			return vsVersions.Where(x => x.Value == year).FirstOrDefault().Key ?? string.Empty;
		}

		/// <summary>
		/// Gets the year from the version number
		/// </summary>
		/// <param name="version">The version.</param>
		/// <returns>A string.</returns>
		public static string YearFromVersion(string version)
		{
			if (string.IsNullOrWhiteSpace(version))
			{
				return string.Empty;
			}

			string major = ExtractMajorVersion(version.Trim());

			return vsVersions.TryGetValue(major, out string? year) ? year : major;
		}

		/// <summary>
		/// Gets the recent projects.
		/// </summary>
		/// <param name="bOnlyDefaultInstances">If true, b only default instances.</param>
		/// <returns>A VsItemList.</returns>
		public VsItemList GetRecentProjects(bool bOnlyDefaultInstances)
		{
			VsFolder solutionList = new VsFolder();
			var vsDir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "VisualStudio"));

			if (!vsDir.Exists)
			{
				return solutionList.Items;
			}

			var enumOptions = new System.IO.EnumerationOptions
			{
				IgnoreInaccessible    = true,
				RecurseSubdirectories = true
			};

			try
			{
				foreach (var dir in vsDir.EnumerateDirectories("*", enumOptions))
				{
					TryLoadRecentProjectsFromDirectory(dir, bOnlyDefaultInstances, solutionList);
				}
			}
			catch (Exception ex) when (ex is DirectoryNotFoundException or UnauthorizedAccessException or IOException)
			{
				Debug.WriteLine(ex);
			}

			return solutionList.Items;
		}

		/// <summary>
		/// Attempts to read Visual Studio MRU entries from a single Visual Studio profile directory.
		/// </summary>
		/// <param name="dir">The directory to inspect.</param>
		/// <param name="bOnlyDefaultInstances">Whether to skip non-default named instances.</param>
		/// <param name="solutionList">Target folder receiving discovered groups.</param>
		private void TryLoadRecentProjectsFromDirectory(DirectoryInfo dir, bool bOnlyDefaultInstances, VsFolder solutionList)
		{
			try
			{
				foreach (var file in dir.GetFiles("ApplicationPrivateSettings.xml"))
				{
					List<MruEntry>? recentProjects = ReadMruEntries(file.FullName);

					if (recentProjects == null)
					{
						continue;
					}

					// build Visual Studio name and version as a string from the directory name, parse the string with regex into version and instance
					(string? groupName, string? vsName) = ParseDirectoryNameIntoVsVersion(dir.Name);

					if (string.IsNullOrEmpty(groupName))
					{
						continue;
					}

					if (bOnlyDefaultInstances && groupName.Contains('/'))
					{
						continue;
					}

					var group = new VsFolder(groupName, string.Empty) { ItemType = ItemTypeEnum.VisualStudio };        // keep the path empty for VS groups
					group.Icon = this.GetByName(vsName!)?.AppIcon;
					PopulateMruGroup(group, recentProjects);
					solutionList.Items.Add(group);
				}
			}
			catch (Exception ex) when (ex is DirectoryNotFoundException or UnauthorizedAccessException or IOException or XmlException or NullReferenceException)
			{
				// possibly invalid or locked file
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Reads offline MRU entries from an ApplicationPrivateSettings.xml file.
		/// </summary>
		/// <param name="filePath">Full path to ApplicationPrivateSettings.xml.</param>
		/// <returns>Deserialized MRU entries or <c>null</c>.</returns>
		private static List<MruEntry>? ReadMruEntries(string filePath)
		{
			var xdoc = XDocument.Load(filePath);
			XElement? codeContainersOffline = xdoc.Descendants("collection").FirstOrDefault(c => c.Attribute("name")?.Value == "CodeContainers.Offline");
			string? codeContainersOfflineValue = codeContainersOffline?.Descendants("value").FirstOrDefault(v => v.Attribute("name")?.Value == "value")?.Value;

			if (string.IsNullOrWhiteSpace(codeContainersOfflineValue))
			{
				return null;
			}

			try
			{
				return JsonConvert.DeserializeObject<List<MruEntry>>(codeContainersOfflineValue);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Populates a group folder with items parsed from Visual Studio MRU entries.
		/// </summary>
		/// <param name="group">The destination group.</param>
		/// <param name="recentProjects">The MRU entries to convert.</param>
		private static void PopulateMruGroup(VsFolder group, List<MruEntry> recentProjects)
		{
			foreach (var s in recentProjects)
			{
				if (s?.Value?.LocalProperties == null || string.IsNullOrEmpty(s.Value.LocalProperties.FullPath))
				{
					continue;
				}

				VsItem item;

				if (s.Value.LocalProperties.Type == 1)
				{
					item = new VsFolder(Path.GetFileNameWithoutExtension(s.Key), s.Value.LocalProperties.FullPath) { ItemType = ItemTypeEnum.Project };
				}
				else
				{
					item = ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(s.Key), s.Value.LocalProperties.FullPath);
				}

				if (item != null)
				{
					item.Warning = item is VsFolder ? !Directory.Exists(item.Path) : !File.Exists(item.Path);
					group.Items.Add(item);
				}
			}
		}

		/// <summary>
		/// Get the highest installed version
		/// </summary>
		/// <returns>The highest installed <see cref="VisualStudioInstance"/>, or <c>null</c> if none are installed.</returns>
		public VisualStudioInstance? HighestVersion()
		{
			return this.allInstances.LastOrDefault();
		}

		/// <summary>
		/// Finds the visual studio instance by identifier without falling back to the highest version.
		/// </summary>
		/// <param name="identifier">The VS identifier.</param>
		/// <returns>The matching <see cref="VisualStudioInstance"/> or <c>null</c>.</returns>
		public VisualStudioInstance? FindByIdentifier(string? identifier)
		{
			if (string.IsNullOrEmpty(identifier))
			{
				return null;
			}

			return this.allInstances.FirstOrDefault(x => x.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Finds the visual studio instance by version prefix without falling back to the highest version.
		/// </summary>
		/// <param name="version">The version string or prefix.</param>
		/// <returns>The matching <see cref="VisualStudioInstance"/> or <c>null</c>.</returns>
		public VisualStudioInstance? FindByVersion(string? version)
		{
			if (string.IsNullOrEmpty(version))
			{
				return null;
			}

			return this.allInstances.FirstOrDefault(x => x.Version.StartsWith(version, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Gets the visual studio instance by identifier
		/// </summary>
		/// <param name="identifier">The VS identifer</param>
		/// <returns>A VisualStudioInstance or <c>null</c> if none are installed.</returns>
		public VisualStudioInstance? GetByIdentifier(string? identifier)
		{
			if (string.IsNullOrEmpty(identifier))
			{
				return HighestVersion();
			}

			var vsi = FindByIdentifier(identifier);

			return vsi ?? HighestVersion();
		}

		/// <summary>
		/// Gets the visual studio instance by name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>A VisualStudioInstance or <c>null</c> if none are installed.</returns>
		public VisualStudioInstance? GetByName(string? name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return HighestVersion();
			}

			var vsi = this.allInstances.FirstOrDefault(x => x.ShortName.Equals(name, StringComparison.InvariantCultureIgnoreCase));

			return vsi ?? HighestVersion();
		}

		/// <summary>
		/// Gets the visual studio instance by version.
		/// </summary>
		/// <param name="version">The version.</param>
		/// <returns>A VisualStudioInstance or <c>null</c> if none are installed.</returns>
		public VisualStudioInstance? GetByVersion(string? version)
		{
			if (string.IsNullOrEmpty(version))
			{
				return HighestVersion();
			}

			var vsi = FindByVersion(version);

			return vsi ?? HighestVersion();
		}

		/// <summary>
		/// Parses the directory name into vs version.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>A (string?, string?) .</returns>
		private (string?, string?) ParseDirectoryNameIntoVsVersion(string name)
		{
			var match = vsDirectoryRegex.Match(name);

			if (match.Success)
			{
				string versionNumber = match.Groups[1].Value;
				string version = "unknown";
				string instance = match.Groups[3].Value;

				// parse the version string into the correct visual studio version
				string mainVersion = ExtractMajorVersion(versionNumber);

				if (vsVersions.ContainsKey(mainVersion))
				{
					version = vsVersions[mainVersion];
				}

				string vsName = $"Visual Studio {version}";
				string groupName = $"{vsName} ({versionNumber})";

				if (!string.IsNullOrEmpty(instance))
				{
					groupName += $" /{instance}";
				}

				return (groupName, vsName);
			}

			return (null, null);
		}
	}
}