using System.Management;
using System.Diagnostics;
using System.Xml.Linq;
using Newtonsoft.Json;
using VSLauncher.Helpers;
using System.Text.RegularExpressions;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// The visual studio instance manager.
	/// </summary>
	public class VisualStudioInstanceManager
	{
		static string vsDirectory_pattern = "(\\d\\d\\.\\d)_(........)(.*)";
		private List<VisualStudioInstance> allInstances;
		Dictionary<string, string> vsVersions = new Dictionary<string, string>()
		{
			{ "15", "2017" },
			{ "16", "2019" },
			{ "17", "2022" }
		};


		/// <summary>
		/// Initializes a new instance of the <see cref="VisualStudioInstanceManager"/> class.
		/// </summary>
		public VisualStudioInstanceManager()
		{
			allInstances = ReadAllInstances();
		}

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
						string name = baseObj.Properties["Name"].Value.ToString();
						string version = baseObj.Properties["Version"].Value.ToString();
						string location = baseObj.Properties["ProductLocation"].Value.ToString();
						string identifier = baseObj.Properties["IdentifyingNumber"].Value.ToString();
						string year = version.StartsWith("15") ? "2017" :
									  version.StartsWith("16") ? "2019" :
									  version.StartsWith("17") ? "2022" : "newer";

						list.Add(new VisualStudioInstance(name, version, location, identifier, year));
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

			return list;
		}

		public VsItemList GetRecentProjects()
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
								var group = new VsFolder(groupName, dir.FullName);
								group.Icon = this.GetByName(vsName!)?.AppIcon;

								// add the projects to the solution
								foreach (var s in recentProjects)
								{
									if (s.Value.LocalProperties.Type == 1)
									{
										var item = new VsFolder(Path.GetFileNameWithoutExtension(s.Key), s.Value.LocalProperties.FullPath);
										group.Items.Add(item);
									}
									else
									{
										var item = ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(s.Key), s.Value.LocalProperties.FullPath);
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
		/// Gets the version number from the year
		/// </summary>
		/// <param name="year">The year.</param>
		/// <returns>A string.</returns>
		public string VersionFromYear(string year)
		{
			return vsVersions.Where(x => x.Value == year).FirstOrDefault().Key;
		}

		/// <summary>
		/// Gets the year from the version number
		/// </summary>
		/// <param name="version">The version.</param>
		/// <returns>A string.</returns>
		public string YearFromVersion(string version)
		{
			return vsVersions.Where(x => x.Key.StartsWith(version)).FirstOrDefault().Key;
		}

		/// <summary>
		/// Gets the by name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>A VisualStudioInstance.</returns>
		internal VisualStudioInstance? GetByName(string name)
		{
			return this.allInstances.Where(x => x.ShortName.Equals(name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
		}

		/// <summary>
		/// Gets the by version.
		/// </summary>
		/// <param name="version">The version.</param>
		/// <returns>A VisualStudioInstance.</returns>
		internal VisualStudioInstance? GetByVersion(string version)
		{
			return this.allInstances.Where(x => x.Version.StartsWith(version, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
		}
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
