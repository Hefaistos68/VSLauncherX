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
				string? repoPath = DiscoverRepositoryPath(itemPath);

				if (string.IsNullOrEmpty(repoPath))
				{
					return ("?", string.Empty);
				}

				using var repo    = new Repository(repoPath);
				var stat          = repo.RetrieveStatus();
				string status     = stat.IsDirty ? "*" : "!";
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
		private void MergeNewItems(OLVListItem? r, VsItemList source)
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
		private void MergeNewItem(OLVListItem? r, VsItem source)
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
		/// <summary>
		/// Rebuilds the filters, searching across collapsed branches and expanding ancestor folders of any match.
		/// </summary>
		private void RebuildFilters()
		{
			string searchText = this.txtFilter.Text;

			this.olvFiles.BeginUpdate();
			this.isUpdatingTreeExpansion = true;

			try
			{
				if (string.IsNullOrEmpty(searchText))
				{
					this.olvFiles.ModelFilter = null;
					RestoreFolderExpansionState(this.solutionGroups);

					return;
				}

				TextMatchFilter filter = new TextMatchFilter(this.olvFiles, searchText);

				// Clear active filter and reset to saved expansion state so all branches exist and can be expanded top-down.
				this.olvFiles.ModelFilter = null;
				RestoreFolderExpansionState(this.solutionGroups);

				List<VsFolder> foldersToExpand = TreeSearchHelper.FindFoldersToExpand(
					this.solutionGroups,
					item => filter.Filter(item));

				foreach (VsFolder folder in foldersToExpand)
				{
					if (!this.olvFiles.IsExpanded(folder))
					{
						this.olvFiles.Expand(folder);
					}
				}

				this.olvFiles.ModelFilter = filter;

				VsItem? firstMatch = TreeSearchHelper.FindFirstMatch(
					this.solutionGroups,
					item => filter.Filter(item));

				if (firstMatch is not null)
				{
					this.olvFiles.EnsureModelVisible(firstMatch);
				}
			}
			finally
			{
				this.isUpdatingTreeExpansion = false;
				this.olvFiles.EndUpdate();
			}
		}

		/// <summary>
		/// Expands collapsed ancestor branches before type-ahead searching in the tree so non-visible items can be matched and revealed.
		/// </summary>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The search event arguments.</param>
		private void olvFiles_BeforeSearching(object? sender, BeforeSearchingEventArgs e)
		{
			if (string.IsNullOrEmpty(e.StringToFind) || this.solutionGroups.Items.Count == 0)
			{
				return;
			}

			OLVColumn? searchColumn = ResolveTypeAheadSearchColumn();

			if (searchColumn is null)
			{
				return;
			}

			VsItem? focusedItem = this.olvFiles.FocusedObject as VsItem;
			IModelFilter? activeFilter = this.olvFiles.ModelFilter;
			bool includeStartItem = e.StringToFind.Length > 1;

			VsItem? match = TreeSearchHelper.FindNextMatch(
				this.solutionGroups,
				focusedItem,
				includeStartItem,
				item => MatchesTypeAheadItem(item, searchColumn, e.StringToFind, activeFilter));

			if (match is null)
			{
				return;
			}

			ExpandAncestorFolders(match);

			int matchIndex = this.olvFiles.IndexOf(match);

			if (matchIndex >= 0)
			{
				e.StartSearchFrom = matchIndex;
			}
		}

		/// <summary>
		/// Resolves the column used by <see cref="ObjectListView"/> for type-ahead prefix searching.
		/// </summary>
		/// <returns>The active search column, or <c>null</c> when the control has no columns.</returns>
		private OLVColumn? ResolveTypeAheadSearchColumn()
		{
			if (this.olvFiles.Columns.Count == 0)
			{
				return null;
			}

			if (this.olvFiles.IsSearchOnSortColumn && this.olvFiles.View == View.Details && this.olvFiles.PrimarySortColumn is not null)
			{
				return this.olvFiles.PrimarySortColumn;
			}

			return this.olvFiles.GetColumn(0);
		}

		/// <summary>
		/// Determines whether an item passes any active filter and prefix-matches the typed search text.
		/// </summary>
		/// <param name="item">The candidate tree item.</param>
		/// <param name="searchColumn">The column whose display text is compared.</param>
		/// <param name="searchText">The incremental search string.</param>
		/// <param name="activeFilter">The active model filter, if any.</param>
		/// <returns><c>true</c> if the item matches; otherwise <c>false</c>.</returns>
		private static bool MatchesTypeAheadItem(VsItem item, OLVColumn searchColumn, string searchText, IModelFilter? activeFilter)
		{
			if (activeFilter is not null && !activeFilter.Filter(item))
			{
				return false;
			}

			string text = searchColumn.GetStringValue(item) ?? string.Empty;

			return text.StartsWith(searchText, StringComparison.CurrentCultureIgnoreCase);
		}

		/// <summary>
		/// Expands all collapsed ancestor folders of <paramref name="target"/> top-down so it becomes visible in the tree.
		/// </summary>
		/// <param name="target">The item to reveal.</param>
		private void ExpandAncestorFolders(VsItem target)
		{
			List<VsFolder> ancestors = TreeSearchHelper.GetAncestorFolders(this.solutionGroups, target);

			if (ancestors.Count == 0)
			{
				return;
			}

			this.olvFiles.BeginUpdate();

			try
			{
				foreach (VsFolder ancestor in ancestors)
				{
					if (!this.olvFiles.IsExpanded(ancestor))
					{
						this.olvFiles.Expand(ancestor);
					}
				}
			}
			finally
			{
				this.olvFiles.EndUpdate();
			}
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

			if (!string.IsNullOrEmpty(this.txtFilter.Text))
			{
				RebuildFilters();
			}

			if (updateGitStatus)
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
		/// Iterates through the items in the ObjectListView and expands or collapses them based on their persisted state.
		/// </summary>
		private void IterateAndExpandItems()
		{
			this.olvFiles.BeginUpdate();
			this.isUpdatingTreeExpansion = true;

			try
			{
				RestoreFolderExpansionState(this.solutionGroups);
			}
			finally
			{
				this.isUpdatingTreeExpansion = false;
				this.olvFiles.EndUpdate();
			}
		}

		/// <summary>
		/// Recursively applies each folder's persisted <see cref="VsFolder.Expanded"/> state to the tree view.
		/// </summary>
		/// <param name="parent">Parent folder whose child folders should be restored.</param>
		private void RestoreFolderExpansionState(VsFolder parent)
		{
			foreach (VsItem item in parent.Items)
			{
				if (item is VsFolder folder)
				{
					if (folder.Expanded)
					{
						if (!this.olvFiles.IsExpanded(folder))
						{
							this.olvFiles.Expand(folder);
						}

						RestoreFolderExpansionState(folder);
					}
					else if (this.olvFiles.IsExpanded(folder))
					{
						this.olvFiles.Collapse(folder);
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

			bool enableBranches = TryPopulateBranchMenuItems(branchesMenu, i);

			branchesMenu.Enabled              = enableBranches;
			this.gitToolStripMenuItem.Enabled = enableBranches;
			ThemeHelper.ApplyTheme(this.ctxMenu, this.currentTheme);
		}

		/// <summary>
		/// Discovers the Git repository for <paramref name="item"/> and populates local branches into <paramref name="branchesMenu"/>.
		/// </summary>
		/// <param name="branchesMenu">The menu to populate with local branches.</param>
		/// <param name="item">The selected item.</param>
		/// <returns><c>true</c> if a repository was found and branches were populated; otherwise <c>false</c>.</returns>
		private bool TryPopulateBranchMenuItems(ToolStripMenuItem branchesMenu, VsItem item)
		{
			if (item is VsFolder || string.IsNullOrWhiteSpace(item.Path))
			{
				return false;
			}

			try
			{
				string? repoPath = DiscoverRepositoryPath(item);

				if (string.IsNullOrEmpty(repoPath))
				{
					return false;
				}

				using var repo       = new Repository(repoPath);
				string? activeBranch = repo.Head?.FriendlyName;

				foreach (var branch in repo.Branches)
				{
					if (branch.IsRemote)
					{
						continue;
					}

					string branchName  = branch.FriendlyName;
					var branchMenuItem = new ToolStripMenuItem(branchName)
					{
						Checked = branchName == activeBranch
					};

					branchMenuItem.Click += (sender, e) =>
					{
						BranchMenuItem_Click(sender!, e, item, branchName);
					};

					branchesMenu.DropDownItems.Add(branchMenuItem);
				}

				return true;
			}
			catch (Exception ex)
			{
				// Handle exceptions (e.g., not a Git repository)
				Debug.WriteLine(ex);

				return false;
			}
		}
	}
}