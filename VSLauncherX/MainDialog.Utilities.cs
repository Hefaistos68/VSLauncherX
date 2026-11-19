using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

using BrightIdeasSoftware;

using LibGit2Sharp;

using Newtonsoft.Json;

using VSLauncher.DataModel;
using VSLauncher.Helpers;

using VSLXshared.Helpers;

namespace VSLauncher
{
	public partial class MainDialog
	{
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
				using Icon? ico = Icon.ExtractAssociatedIcon(vsi);
				btnVsInstaller.Image = ico is null ? Resources.Installer : ico.ToBitmap();
			}
		}

		/// <summary>
		/// Starts fetching the git status for all items in the tree asynchronously.
		/// </summary>
		/// <param name="folder">The folder.</param>
		private void FetchGitStatusAsync(VsFolder folder)
		{
			if (Interlocked.CompareExchange(ref this.gitRefreshInProgress, 1, 0) != 0)
			{
				return;
			}

			// Snapshot non-folder items on the UI thread to prevent concurrent collection modification
			var snapshot = new List<VsItem>();
			CollectGitItems(folder, snapshot);

			if (!this.IsDisposed && this.IsHandleCreated)
			{
				toolStripStatusGit.Visible = true;
			}

			_ = Task.Run(() =>
			{
				try
				{
					FetchGitStatusSnapshot(snapshot);
				}
				finally
				{
					Interlocked.Exchange(ref this.gitRefreshInProgress, 0);
					SafeInvokeOnUiThread(() =>
					{
						this.olvFiles.Invalidate();
						toolStripStatusGit.Visible = false;
					});
				}
			});
		}

		/// <summary>
		/// Recursively collects non-folder items from a folder snapshot on the UI thread.
		/// </summary>
		/// <param name="folder">Root folder to traverse.</param>
		/// <param name="target">Destination list of items.</param>
		private static void CollectGitItems(VsFolder folder, List<VsItem> target)
		{
			Stack<VsFolder> stack = new();
			stack.Push(folder);

			while (stack.Count > 0)
			{
				VsFolder current = stack.Pop();

				foreach (VsItem item in current.Items)
				{
					if (item is VsFolder subFolder)
					{
						stack.Push(subFolder);
					}
					else
					{
						target.Add(item);
					}
				}
			}
		}

		/// <summary>
		/// Fetches Git status and branch names for a snapshotted list of items on a background thread.
		/// </summary>
		/// <param name="items">Snapshot of items to inspect.</param>
		private void FetchGitStatusSnapshot(List<VsItem> items)
		{
			foreach (var item in items)
			{
				(string status, string branchName) = ReadItemGitStatus(item.Path);

				SafeInvokeOnUiThread(() =>
				{
					item.Status     = status;
					item.BranchName = branchName;
				});
			}
		}

		/// <summary>
		/// Resolves Git dirty status and branch name for a file path using repository discovery.
		/// </summary>
		/// <param name="itemPath">Path to a solution or project file.</param>
		/// <returns>Tuple of status marker and branch name.</returns>
		private static (string Status, string BranchName) ReadItemGitStatus(string? itemPath)
		{
			if (string.IsNullOrWhiteSpace(itemPath))
			{
				return ("?", string.Empty);
			}

			try
			{
				string? dir = Path.GetDirectoryName(itemPath);

				if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
				{
					return ("?", string.Empty);
				}

				string? repoPath = Repository.Discover(dir);

				if (string.IsNullOrEmpty(repoPath))
				{
					return ("?", string.Empty);
				}

				using var repo = new Repository(repoPath);
				var stat = repo.RetrieveStatus();
				string status = stat.IsDirty ? "*" : "!";
				string branchName = repo.Head?.FriendlyName ?? string.Empty;

				return (status, branchName);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);

				return ("?", string.Empty);
			}
		}

		/// <summary>
		/// Marshals an action safely onto the UI thread if the form handle is alive.
		/// </summary>
		/// <param name="action">Action to execute on the UI thread.</param>
		private void SafeInvokeOnUiThread(Action action)
		{
			if (this.IsDisposed || !this.IsHandleCreated)
			{
				return;
			}

			try
			{
				if (this.InvokeRequired)
				{
					this.BeginInvoke(action);
				}
				else
				{
					action();
				}
			}
			catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
			{
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
			else if (r.RowObject is VsFolder sg)
			{
				// add below selected item
				sg.Items.AddRange(source);
				sg.Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
			}
			else
			{
				// get parent of this item
				VsFolder targetParent = this.solutionGroups.FindParent(r.RowObject) ?? this.solutionGroups;

				// add at the end of parent folder
				targetParent.Items.AddRange(source);
				targetParent.Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
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
			else if (r.RowObject is VsFolder sg)
			{
				// add below selected item
				sg.Items.Add(source);
				sg.Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
			}
			else
			{
				// get parent of this item
				VsFolder targetParent = this.solutionGroups.FindParent(r.RowObject) ?? this.solutionGroups;

				// add at the end
				targetParent.Items.Add(source);
				targetParent.Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
			}
		}

		/// <summary>
		/// Restarts the application to register it with admin rights.
		/// </summary>
		/// <returns>True if the elevated instance started; false if cancelled or failed.</returns>
		private bool RestartOurselves()
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName        = Environment.ProcessPath ?? Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".exe"),
				Arguments       = "register",
				Verb            = "runas",
				UseShellExecute = true
			};

			try
			{
				using var process = Process.Start(startInfo);

				return process != null;
			}
			catch (Win32Exception ex)
			{
				Debug.WriteLine(ex);

				return false;
			}
		}

		/// <summary>
		/// Loads the solution data.
		/// </summary>
		/// <returns>True if data loaded or no datafile exists yet; false if the existing datafile was unreadable.</returns>
		private bool LoadSolutionData()
		{
			// load this.solutionGroups data from a JSON file in the users data folder
			string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VSLauncher", "VSLauncher.json");

			if (File.Exists(fileName))
			{
				VsFolder? data = null;

				try
				{
					string json = File.ReadAllText(fileName);
					JsonSerializerSettings settings = FileHelper.CreateSafeJsonSettings(indented: false);
					data = JsonConvert.DeserializeObject<VsFolder>(json, settings);
				}
				catch (System.Exception ex)
				{
					// probably wrong format or disallowed type
					Debug.WriteLine(ex);
				}

				if (data is null)
				{
					this.solutionDataLoadFailed = true;

					// alert the user that the datafile was unreadable
					_ = MessageBox.Show($"The datafile was unreadable. Please check the file in '{fileName}' and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

					return false;
				}
				else
				{
					UpgradeLegacySlnxItems(data);
					data.Refresh();
					this.solutionGroups         = data;
					this.solutionDataLoadFailed = false;
				}
			}
			else
			{
				this.solutionDataLoadFailed = false;
			}

			return true;
		}

		/// <summary>
		/// Upgrades legacy .slnx entries saved as base <see cref="VsItem"/> instances into <see cref="VsSolution"/> instances.
		/// </summary>
		/// <param name="folder">The root folder to inspect recursively.</param>
		private static void UpgradeLegacySlnxItems(VsFolder folder)
		{
			for (int i = 0; i < folder.Items.Count; i++)
			{
				VsItem item = folder.Items[i];

				if (item is VsFolder subFolder)
				{
					UpgradeLegacySlnxItems(subFolder);
					continue;
				}

				if (item.GetType() == typeof(VsItem) && string.Equals(Path.GetExtension(item.Path), ".slnx", StringComparison.OrdinalIgnoreCase))
				{
					folder.Items[i] = new VsSolution
					{
						Name              = item.Name,
						Path              = item.Path,
						Commands          = item.Commands,
						Instance          = item.Instance,
						VsVersion         = item.VsVersion,
						IsFavorite        = item.IsFavorite,
						BranchName        = item.BranchName,
						RunBefore         = item.RunBefore,
						RunAfter          = item.RunAfter,
						RunAsAdmin        = item.RunAsAdmin,
						ShowSplash        = item.ShowSplash,
						WaitForCompletion = item.WaitForCompletion,
						PreferredMonitor  = item.PreferredMonitor
					};
				}
			}
		}

		/// <summary>
		/// Saves the solution data.
		/// </summary>
		private void SaveSolutionData()
		{
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

				JsonSerializerSettings settings = FileHelper.CreateSafeJsonSettings(indented: true);
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

			if (i is not VsFolder && !string.IsNullOrWhiteSpace(i.Path))
			{
				try
				{
					string? dir = Path.GetDirectoryName(i.Path);
					string? repoPath = (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir)) ? Repository.Discover(dir) : null;

					if (!string.IsNullOrEmpty(repoPath))
					{
						using (var repo = new Repository(repoPath))
						{
							// Get the list of branches
							var branches = repo.Branches;
							var activeBranch = repo.Head?.FriendlyName;

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
										BranchMenuItem_Click(sender!, e, i, branchName);
									};
									branchesMenu.DropDownItems.Add(branchMenuItem);
								}
							}

							enableBranches = true;
						}
					}
				}
				catch (Exception ex)
				{
					// Handle exceptions (e.g., not a Git repository)
					Debug.WriteLine(ex);
				}
			}

			branchesMenu.Enabled = enableBranches;
			this.gitToolStripMenuItem.Enabled = enableBranches;
			ThemeHelper.ApplyTheme(this.ctxMenu, this.currentTheme);
		}
	}
}