using System.Collections;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using BrightIdeasSoftware;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using VSLauncher.DataModel;

namespace VSLauncher
{
	public partial class dlgImportVisualStudio : Form
	{
		/// <summary>
		/// Gets the solution group selected by the user
		/// </summary>
		public SolutionGroup Solution { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgImportVisualStudio"/> class.
		/// </summary>
		public dlgImportVisualStudio()
		{
			InitializeComponent();
			InitializeList();
		}

		private void InitializeList()
		{
			this.olvFiles.FullRowSelect = true;
			this.olvFiles.RowHeight = 26;

			this.olvFiles.HierarchicalCheckboxes = true;
			this.olvFiles.TreeColumnRenderer.IsShowLines = true;
			this.olvFiles.TreeColumnRenderer.UseTriangles = true;

			// 			TypedObjectListView<SolutionGroup> tlist = new TypedObjectListView<SolutionGroup>(this.olvFiles);
			// 			tlist.GenerateAspectGetters();

			this.olvFiles.CanExpandGetter = delegate (object x)
			{
				return x is SolutionGroup;
			};

			this.olvFiles.ChildrenGetter = delegate (object x)
			{
				return x is SolutionGroup sg ? sg.Solutions : (IEnumerable?)null;
			};
			//
			// setup the Name/Filename column
			//
			this.olvColumnFilename.ImageGetter = ColumnHelper.GetImageNameForMru;
			this.olvColumnFilename.AspectGetter = ColumnHelper.GetAspectForFile;

			//
			// setup the Path column
			//
			this.olvColumnPath.AspectGetter = ColumnHelper.GetAspectForPath;

			this.Solution = new SolutionGroup();
			this.olvFiles.SetObjects(GetRecentProjects());
		}

		/// <summary>
		/// Handles Click events for the btnSelectAfter button.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnSelectFolder_Click(object sender, EventArgs e)
		{
			// let the user select a folder through the system dialog

		}

		static string vsDirectory_pattern = "(\\d\\d\\.\\d)_(........)(.*)";
		Dictionary<string, string> vsVersions = new Dictionary<string, string>()
		{
			{ "15", "2017" },
			{ "16", "2019" },
			{ "17", "2022" }
		};

		public IEnumerable GetRecentProjects()
		{
			SolutionGroup solutionList = new SolutionGroup();

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
							string? groupName = ParseDirectoryNameIntoVsVersion(dir.Name);

							if (!string.IsNullOrEmpty(groupName))
							{
								var group = new SolutionGroup(groupName);

								// add the projects to the solution
								foreach (var s in recentProjects)
								{
									if (s.Value.LocalProperties.Type == 1)
									{
										var item = new VsFolder(Path.GetFileNameWithoutExtension(s.Key), s.Value.LocalProperties.FullPath);
										group.Solutions.Add(item);
									}
									else
									{
										var item = ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(s.Key), s.Value.LocalProperties.FullPath);
										group.Solutions.Add(item);
									}
								}

								solutionList.Solutions.Add(group);
							}
						}
					}
				}
				catch (DirectoryNotFoundException)
				{
				}
			}

			return solutionList.Solutions;
		}

		private string? ParseDirectoryNameIntoVsVersion(string name)
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
				string groupName = $"Visual Studio {version} ({versionNumber})";
				if (!string.IsNullOrEmpty(instance))
				{
					groupName += $" /{instance}";
				}

				return groupName;
			}

			return null;
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
		}

		private void listViewFiles_CellToolTipShowing(object sender, ToolTipShowingEventArgs e)
		{
			if (e.Model is SolutionGroup)
			{
				e.Text = e.Item.Text;
			}
			else
			{
				e.Text = e.SubItem.Text;
			}
		}

	}



}
