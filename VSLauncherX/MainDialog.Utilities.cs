using System.Diagnostics;
using System.IO;
using System.Reflection;

using BrightIdeasSoftware;

using LibGit2Sharp;

using Newtonsoft.Json;

using VSLauncher.DataModel;
using VSLauncher.Helpers;

namespace VSLauncher
{
	public partial class MainDialog
	{
		#region Git Credentials Helper
		// Thread-safe lazy cache of PATs loaded from optional config file
		private static readonly object _credLock = new();
		private static bool _credsLoaded = false;
		private static Dictionary<string,string> _hostPats = new(StringComparer.OrdinalIgnoreCase); // host -> PAT
		private static Dictionary<string,string> _repoPats = new(StringComparer.OrdinalIgnoreCase); // repo name -> PAT

		/// <summary>
		/// Load credential mappings from %APPDATA%/VSLauncher/git-credentials.json if present.
		/// Schema:
		/// {
		/// "hosts": { "github.com": "PAT1", "dev.azure.com": "PAT2" },
		/// "repos": { "MyRepo": "PAT3", "AnotherRepo": "PAT4" }
		/// }
		/// </summary>
		private static void EnsureCredentialConfigLoaded()
		{
			if (_credsLoaded) return;
			lock (_credLock)
			{
				if (_credsLoaded) return;
				try
				{
					string cfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VSLauncher", "git-credentials.json");
					if (File.Exists(cfgPath))
					{
						var json = File.ReadAllText(cfgPath);
						var root = JsonConvert.DeserializeObject<GitCredConfig?>(json);
						if (root != null)
						{
							_hostPats = new Dictionary<string,string>(root.Hosts ?? new(), StringComparer.OrdinalIgnoreCase);
							_repoPats = new Dictionary<string,string>(root.Repos ?? new(), StringComparer.OrdinalIgnoreCase);
						}
					}
				}
				catch { /* ignore malformed file */ }
				finally { _credsLoaded = true; }
			}
		}

		private class GitCredConfig
		{
			[JsonProperty("hosts")] public Dictionary<string,string>? Hosts { get; set; }
			[JsonProperty("repos")] public Dictionary<string,string>? Repos { get; set; }
		}

		/// <summary>
		/// Provides credentials for LibGit2Sharp remote operations.
		/// Resolution order:
		///1. Repo-specific env var VSLX_GIT_PAT_REPO_{REPO_NAME}
		///2. Host-specific env var VSLX_GIT_PAT_HOST_{HOST}
		/// (dots replaced by '_', uppercased)
		///3. Config file repo mapping (git-credentials.json)
		///4. Config file host mapping
		///5. Generic env var VSLX_GIT_PAT
		///6. DefaultCredentials
		/// </summary>
		private static Credentials GitCredentialsProvider(string url, string usernameFromUrl, SupportedCredentialTypes types)
		{
			EnsureCredentialConfigLoaded();

			// Parse URL to extract host and repo name
			string? host = null; string? repoName = null;
			if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
			{
				host = uri.Host;
				// repo name is last segment without .git
				var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
				if (segments.Length >0)
				{
					repoName = segments[^1].EndsWith(".git", StringComparison.OrdinalIgnoreCase)
						? segments[^1].Substring(0, segments[^1].Length -4)
						: segments[^1];
				}
			}

			//1 &2: Environment overrides
			if (!string.IsNullOrEmpty(repoName))
			{
				var repoEnvKey = $"VSLX_GIT_PAT_REPO_{repoName.ToUpperInvariant()}";
				var repoPat = Environment.GetEnvironmentVariable(repoEnvKey);
				if (!string.IsNullOrWhiteSpace(repoPat))
					return PatCredentials(repoPat);
			}

			if (!string.IsNullOrEmpty(host))
			{
				var hostKey = host.ToUpperInvariant().Replace('.', '_');
				var hostEnvKey = $"VSLX_GIT_PAT_HOST_{hostKey}";
				var hostPat = Environment.GetEnvironmentVariable(hostEnvKey);
				if (!string.IsNullOrWhiteSpace(hostPat))
					return PatCredentials(hostPat);
			}

			//3 &4: Config file mappings
			if (!string.IsNullOrEmpty(repoName) && _repoPats.TryGetValue(repoName, out var cfgRepoPat) && !string.IsNullOrWhiteSpace(cfgRepoPat))
				return PatCredentials(cfgRepoPat);

			if (!string.IsNullOrEmpty(host) && _hostPats.TryGetValue(host, out var cfgHostPat) && !string.IsNullOrWhiteSpace(cfgHostPat))
				return PatCredentials(cfgHostPat);

			//5: Generic PAT
			var genericPat = Environment.GetEnvironmentVariable("VSLX_GIT_PAT");
			if (!string.IsNullOrWhiteSpace(genericPat))
				return PatCredentials(genericPat);

			//6: Fallback
			return new DefaultCredentials();
		}

		private static UsernamePasswordCredentials PatCredentials(string pat) => new()
		{
			Username = "git",
			Password = pat
		};
		#endregion

		/// <summary>
		/// Returns if the control key is pressed.
		/// </summary>
		/// <returns>A bool.</returns>
		private static bool IsControlPressed()
		{
			return (Control.ModifierKeys & Keys.Control) == Keys.Control;
		}

		/// <summary>
		/// Creates the described task renderer.
		/// </summary>
		/// <returns>A DescribedTaskRenderer.</returns>
		private DescribedTaskRenderer CreateDescribedRenderer()
		{
			// Let's create an appropriately configured renderer.
			DescribedTaskRenderer renderer = new DescribedTaskRenderer
			{
				// Give the renderer its own collection of images. If this isn't set, the renderer will use the
				// SmallImageList from the ObjectListView. (this is standard Renderer behaviour, not specific to DescribedTaskRenderer).
				ImageList = this.imageListMainIcons,

				// Tell the renderer which property holds the text to be used as a description
				DescriptionGetter = ColumnHelper.GetDescription,

				// Change the formatting slightly
				TitleFont = new Font("Verdana", 11, FontStyle.Bold),
				DescriptionFont = new Font("Verdana", 8),
				ImageTextSpace = 8,
				TitleDescriptionSpace = 1,

				// Use older Gdi renderering, since most people think the text looks clearer
				UseGdiTextRendering = true,
				Aspect = "Name"
			};

			return renderer;
		}

		/// <summary>
		/// Finds the visual studio installer and sets up the button.
		/// </summary>
		private void FindVisualStudioInstaller()
		{
			string vsi = VisualStudioInstanceManager.InstallerPath;

			btnVsInstaller.Tag = vsi;

			if (vsi.StartsWith("http"))
			{
				btnVsInstaller.Text = "Download Visual Studio";
				btnVsInstaller.Image = Resources.Download;
			}
			else
			{
				btnVsInstaller.Tag = vsi;
				btnVsInstaller.Text = "Visual Studio Installer";
				// get icon from installer and set as image into the button
				Icon? ico = Icon.ExtractAssociatedIcon(vsi);
				btnVsInstaller.Image = ico is null ? Resources.Installer : ico.ToBitmap();
			}
		}

		/// <summary>
		/// Starts fetching the git status for all items in the tree asynchronously.
		/// </summary>
		/// <param name="folder">The folder.</param>
		private void FetchGitStatusAsync(VsFolder folder)
		{
			Task.Run(() => FetchGitStatusBackground(folder));
		}

		/// <summary>
		/// Fetches the git status for all items in the tree
		/// </summary>
		/// <param name="folder">The folder.</param>
		private void FetchGitStatusBackground(VsFolder folder)
		{
			foreach (var item in folder.Items)
			{
				if (item is not VsFolder)
				{
					string? status = null;
					string? branchName = null;

					try
					{
						using (var repo = new Repository(Path.GetDirectoryName(item.Path)))
						{
							var stat = repo.RetrieveStatus();
							status = stat.IsDirty ? "*" : "!";
							branchName = repo.Head.FriendlyName;
						}
					}
					catch (RepositoryNotFoundException)
					{
						try
						{
							using (var repo = new Repository(Path.GetDirectoryName(Path.GetDirectoryName(item.Path))))
							{
								var stat = repo.RetrieveStatus();
								status = stat.IsDirty ? "*" : "!";
								branchName = repo.Head.FriendlyName;
							}
						}
						catch (RepositoryNotFoundException)
						{
							status = "?";
							branchName = string.Empty;
						}
					}

					// Marshal the update to the UI thread
					if (this.InvokeRequired)
					{
						this.BeginInvoke(new Action(() =>
						{
							item.Status = status;
							item.BranchName = branchName;
							// Optionally refresh the UI for this item here
						}));
					}
					else
					{
						item.Status = status;
						item.BranchName = branchName;
					}
				}
				else
				{
					FetchGitStatusBackground(item as VsFolder);
				}
			}
		}

		/// <summary>
		/// Merges new items into the selected list item
		/// </summary>
		/// <param name="r">The selected item</param>
		/// <param name="source">The source to add</param>
		private void MergeNewItems(OLVListItem r, VsItemList source)
		{
			if (r == null)
			{
				// nothing selected, add to the end
				this.solutionGroups.Items.AddRange(source);
				this.solutionGroups.Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
			}
			else
			{
				if (r.RowObject is VsFolder sg)
				{
					// add below selected item
					sg.Items.AddRange(source);
					sg.Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
				}
				else
				{
					// get parent of this item
					_ = this.solutionGroups.FindParent(r.RowObject as VsItem);

					// add at the end
					this.solutionGroups.Items.AddRange(source);
					this.solutionGroups.Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
				}
			}
		}

		/// <summary>
		/// Merges a new item into the selected list item
		/// </summary>
		/// <param name="r">The selected list item</param>
		/// <param name="source">The source to add</param>
		private void MergeNewItem(OLVListItem r, VsItem source)
		{
			if (r == null)
			{
				// nothing selected, add to the end
				this.solutionGroups.Items.Add(source);
				this.solutionGroups.Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
			}
			else
			{
				if (r.RowObject is VsFolder sg)
				{
					// add below selected item
					sg.Items.Add(source);
					sg.Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
				}
				else
				{
					// get parent of this item
					VsFolder? vsi = this.solutionGroups.FindParent(r.RowObject as VsItem);

					// add at the end
					vsi?.Items.Add(source);
					vsi?.Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
				}
			}

			// Sort the list of items
		}

		/// <summary>
		/// Restarts the application to register it with admin rights.
		/// </summary>
		private void RestartOurselves()
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe"),
				Arguments = "register",
				Verb = "runas",
				UseShellExecute = true
			};

			Process.Start(startInfo);
		}

		/// <summary>
		/// Loads the solution data.
		/// </summary>
		private void LoadSolutionData()
		{
			// load this.solutionGroups data from a JSON file in the users data folder
			string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VSLauncher", "VSLauncher.json");

			if (File.Exists(fileName))
			{
				string json = File.ReadAllText(fileName);
				JsonSerializerSettings settings = new JsonSerializerSettings()
				{
					TypeNameHandling = TypeNameHandling.All
				};

				VsFolder? data = null;

				try
				{
					data = JsonConvert.DeserializeObject<VsFolder>(json, settings);
				}
				catch (System.Exception)
				{
					// probably wrong format
				}

				if (data is null)
				{
					// alert the user that the datafile was unreadable
					_ = MessageBox.Show($"The datafile was unreadable. Please check the file in '{fileName}' and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					data.Refresh();
					this.solutionGroups = data;
				}
			}
		}

		/// <summary>
		/// Saves the solution data.
		/// </summary>
		private void SaveSolutionData()
		{
			// 			if (true)
			// 			{
			// 				JsonSerializerSettings settings = new JsonSerializerSettings()
			// 				{
			// 					Formatting = Formatting.Indented,
			// 					TypeNameHandling = TypeNameHandling.All
			// 				};
			//
			// 				string json = JsonConvert.SerializeObject(this.solutionGroups, settings);
			// 				// testing out storing the file in the google drive
			// 				new GoogleDriveStorage().Upload(json);
			// 			}

			// save this.solutionGroups data to a JSON file in the users data folder
			string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VSLauncher", "VSLauncher.json");

			try
			{
				string? dir = Path.GetDirectoryName(fileName);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				{
					// make sure the path exists
					_ = Directory.CreateDirectory(dir);
				}

				JsonSerializerSettings settings = new JsonSerializerSettings()
				{
					Formatting = Formatting.Indented,
					TypeNameHandling = TypeNameHandling.All
				};

				string json = JsonConvert.SerializeObject(this.solutionGroups, settings);
				try
				{
					File.WriteAllText(fileName, json);
				}
				catch (System.Exception ex)
				{
					// alert user of an error saving the data
					_ = MessageBox.Show($"There was an error saving the data. \r\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			catch (System.Exception ex)
			{
				_ = MessageBox.Show($"There was an error saving the data. \r\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// Rebuilds the filters.
		/// </summary>
		private void RebuildFilters()
		{
			this.olvFiles.ModelFilter = string.IsNullOrEmpty(this.txtFilter.Text) ? null : new TextMatchFilter(this.olvFiles, this.txtFilter.Text);
			// this.olvFiles.AdditionalFilter = filters.Count == 0 ? null : new CompositeAllFilter(filters);
		}

		/// <summary>
		/// Updates the list.
		/// </summary>
		private void UpdateList(bool updateGitStatus)
		{
			// TODO: must verify items before loading, indicate missing items through warning icon
			this.olvFiles.SetObjects(this.solutionGroups.Items);

			IterateAndSortItems();
			IterateAndExpandItems();
			
			if(updateGitStatus)
			{
				FetchGitStatusAsync(this.solutionGroups);
			}
		}

		private void IterateAndSortItems()
		{
			foreach (var item in this.olvFiles.Objects)
			{
				if (item is VsFolder folder)
				{
					folder.Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
				}
			}
		}

		/// <summary>
		/// Iterates through the items in the ObjectListView and expands or collapses them based on their state.
		/// </summary>
		private void IterateAndExpandItems()
		{
			foreach (var item in this.olvFiles.Objects)
			{
				if (item is VsFolder folder)
				{
					if (folder.Expanded)
					{
						this.olvFiles.Expand(item);
					}
					else
					{
						this.olvFiles.Collapse(item);
					}
				}
			}
		}

		/// <summary>
		/// Sets up the branch menu for the given item.
		/// </summary>
		/// <param name="i">The item for which to set up the branch menu.</param>
		private void SetupBranchMenu(VsItem i)
		{
			var branchesMenu = this.gitToolStripMenuItem.DropDownItems.OfType<ToolStripMenuItem>().FirstOrDefault(item => item.Text == "Branches");

			if (branchesMenu == null)
			{
				// Create a new submenu for branches
				branchesMenu = new ToolStripMenuItem("Branches");
				this.gitToolStripMenuItem.DropDownItems.Insert(0, branchesMenu);
			}
			else
			{
				// Clear existing entries
				branchesMenu.DropDownItems.Clear();
			}

			bool enableBranches = false;

			if (i is not VsFolder)
				if (Directory.Exists(Path.Combine(Path.GetDirectoryName(i.Path), ".git")))
			{
				try
				{
					using (var repo = new Repository(Path.GetDirectoryName(i.Path)))
					{
						// Get the list of branches
						var branches = repo.Branches;
						var activeBranch = repo.Head.FriendlyName;

						// Check if the branches menu already exists

						// Add branch items to the submenu
						foreach (var branch in branches)
						{
								if (!branch.IsRemote)
								{
									string branchName = branch.FriendlyName;
									var branchMenuItem = new ToolStripMenuItem(branch.FriendlyName)
									{
										Checked = branch.FriendlyName == activeBranch
									};
									branchMenuItem.Click += (sender, e) =>
									{
										BranchMenuItem_Click(sender, e, i, branchName);
									};
									branchesMenu.DropDownItems.Add(branchMenuItem);
								}
						}

						enableBranches = true;
					}
				}
				catch (Exception ex)
				{
					// Handle exceptions (e.g., not a Git repository)
				}
			}

			branchesMenu.Enabled = enableBranches;
			this.gitToolStripMenuItem.Enabled = enableBranches;
		}
	}
}