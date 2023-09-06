using System;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Newtonsoft.Json;

using VSLauncher.Helpers;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// The visual studio instance manager.
	/// </summary>
	public class VisualStudioInstanceManager
	{
		private static string vsDirectory_pattern = "(\\d\\d\\.\\d)_(........)(.*)";

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
			{ "17", "2022" }
		};

		private List<VisualStudioInstance> allInstances;

		/// <summary>
		/// Initializes a new instance of the <see cref="VisualStudioInstanceManager"/> class.
		/// </summary>
		public VisualStudioInstanceManager()
		{
			allInstances = ReadAllInstances();
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
		public VisualStudioInstance this[string version]
		{
			get
			{
				return allInstances.Where(x => x.Version.StartsWith(version)).Single();
			}
		}

		/// <summary>
		/// Reads the all installed Visual Studio instances from WMI
		/// </summary>
		/// <returns>A list of VisualStudioInstances.</returns>
		public static List<VisualStudioInstance> ReadAllInstances()
		{
			var list = new List<VisualStudioInstance>();
			// read all data from WMI using CimInstance MSFT_VSInstance
			// https://docs.microsoft.com/en-us/windows/win32/wmisdk/msft-vsinstance

			ManagementObjectSearcher searcher = new ManagementObjectSearcher
			{
				Query = new SelectQuery("MSFT_VSInstance ", "", new[] { "Name", "Version", "ProductLocation", "IdentifyingNumber" })
			};
			ManagementObjectCollection collection = searcher.Get();
			ManagementObjectCollection.ManagementObjectEnumerator em = collection.GetEnumerator();

			while (em.MoveNext())
			{
				ManagementBaseObject baseObj = em.Current;
				if (baseObj.Properties["Version"].Value != null)
				{
					try
					{
						string? name = baseObj.Properties["Name"].Value.ToString();
						string? version = baseObj.Properties["Version"].Value.ToString();
						string? location = baseObj.Properties["ProductLocation"].Value.ToString();
						string? identifier = baseObj.Properties["IdentifyingNumber"].Value.ToString();

						if (name != null && version != null && location != null && identifier != null)
						{
							list.Add(new VisualStudioInstance(name, version, location, identifier, VisualStudioInstanceManager.YearFromVersion(version[..2])));
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex.ToString());
					}
				}
			}

			em?.Dispose();
			collection?.Dispose();
			searcher?.Dispose();

			list.Sort((x, y) => x.Version.CompareTo(y.Version));

			return list;
		}

		/// <summary>
		/// Gets the version number from the year
		/// </summary>
		/// <param name="year">The year.</param>
		/// <returns>A string.</returns>
		public static string VersionFromYear(string year)
		{
			return vsVersions.Where(x => x.Value == year).FirstOrDefault().Key;
		}

		/// <summary>
		/// Gets the year from the version number
		/// </summary>
		/// <param name="version">The version.</param>
		/// <returns>A string.</returns>
		public static string YearFromVersion(string version)
		{
			return vsVersions.Where(x => x.Key.StartsWith(version)).FirstOrDefault().Value;
		}

		/// <summary>
		/// Gets the recent projects.
		/// </summary>
		/// <param name="bOnlyDefaultInstances">If true, b only default instances.</param>
		/// <returns>A VsItemList.</returns>
		public VsItemList GetRecentProjects(bool bOnlyDefaultInstances)
		{
			VsFolder solutionList = new VsFolder();

			var vsDir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft",  "VisualStudio"));

			foreach (var dir in vsDir.GetDirectories("*", SearchOption.AllDirectories))
			{
				try
				{
					foreach (var file in dir.GetFiles("ApplicationPrivateSettings.xml"))
					{
						var xdoc = XDocument.Load(file.FullName);
						XElement codeContainersOffline = xdoc.Descendants("collection").FirstOrDefault(c => c.Attribute("name")?.Value == "CodeContainers.Offline");
						string codeContainersOfflineValue = codeContainersOffline?.Descendants("value").FirstOrDefault(v => v.Attribute("name")?.Value == "value")?.Value;

						List<MruEntry>? recentProjects = null;
						try
						{
							recentProjects = JsonConvert.DeserializeObject<List<MruEntry>>(codeContainersOfflineValue);
						}
						catch
						{
						}

						if (recentProjects != null)
						{
							// build Visual Studio name and version as a string from the directory name, parse the string with regex into version and instance
							(string? groupName, string? vsName) = ParseDirectoryNameIntoVsVersion(dir.Name);

							if (!string.IsNullOrEmpty(groupName))
							{
								if (bOnlyDefaultInstances && groupName.Contains('/'))
									continue;

								var group = new VsFolder(groupName, string.Empty) { ItemType = ItemTypeEnum.VisualStudio };        // keep the path empty for VS groups
								group.Icon = this.GetByName(vsName!)?.AppIcon;

								// add the projects to the solution
								foreach (var s in recentProjects)
								{
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

								solutionList.Items.Add(group);
							}
						}
					}
				}
				catch (DirectoryNotFoundException)
				{
				}
			}

			return solutionList.Items;
		}

		/// <summary>
		/// Get the highest installed version
		/// </summary>
		/// <returns>A string.</returns>
		public VisualStudioInstance HighestVersion()
		{
			return this.allInstances.Last();
		}

		/// <summary>
		/// Gets the visual studio instance by identifier
		/// </summary>
		/// <param name="identifier">The VS identifer</param>
		/// <returns>A VisualStudioInstance.</returns>
		public VisualStudioInstance GetByIdentifier(string identifier)
		{
			if (string.IsNullOrEmpty(identifier))
				return HighestVersion();

			var vsi = this.allInstances.Where(x => x.Identifier == identifier).FirstOrDefault();

			return vsi is null ? HighestVersion() : vsi;
		}

		/// <summary>
		/// Gets the visual studio instance by name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>A VisualStudioInstance.</returns>
		public VisualStudioInstance? GetByName(string name)
		{
			if (string.IsNullOrEmpty(name))
				return HighestVersion();

			var vsi =  this.allInstances.Where(x => x.ShortName.Equals(name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

			return vsi is null ? HighestVersion() : vsi;
		}

		/// <summary>
		/// Gets the visual studio instance by version.
		/// </summary>
		/// <param name="version">The version.</param>
		/// <returns>A VisualStudioInstance.</returns>
		public VisualStudioInstance? GetByVersion(string? version)
		{
			if (string.IsNullOrEmpty(version))
				return HighestVersion();

			var vsi =  this.allInstances.Where(x => x.Version.StartsWith(version, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

			return vsi is null ? HighestVersion() : vsi;
		}

		/// <summary>
		/// Parses the directory name into vs version.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>A (string?, string?) .</returns>
		private (string?, string?) ParseDirectoryNameIntoVsVersion(string name)
		{
			var regex = new Regex(vsDirectory_pattern);
			var match = regex.Match(name);
			if (match.Success)
			{
				string versionNumber = match.Groups[1].Value;
				string version = "unknown";
				string instance = match.Groups[3].Value;

				// parse the version string into the correct visual studio version
				string mainVersion = versionNumber.Substring(0, 2);
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