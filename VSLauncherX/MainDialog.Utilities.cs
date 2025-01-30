﻿using System.Diagnostics;
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
		/// Fetches the git status for all items in the tree
		/// </summary>
		/// <param name="folder">The folder.</param>
		private void FetchGitStatus(VsFolder folder)
		{
			foreach (var item in folder.Items)
			{
				if (item is not VsFolder)
				{
					// find out if this is a git repo by looking for the ".git" folder
					// if it is, then we can get the status

					try
					{
						using (var repo = new Repository(Path.GetDirectoryName(item.Path)))
						{
							var stat = repo.RetrieveStatus();

							item.Status = stat.IsDirty ? "*" : "!";
							item.BranchName = repo.Head.FriendlyName;
						}
					}
					catch (RepositoryNotFoundException ex)
					{
						// retry with the parent folder
						try
						{
							using (var repo = new Repository(Path.GetDirectoryName(Path.GetDirectoryName(item.Path))))
							{
								var stat = repo.RetrieveStatus();

								item.Status = stat.IsDirty ? "*" : "!";
								item.BranchName = repo.Head.FriendlyName;
							}
						}
						catch (RepositoryNotFoundException ex2)
						{
							// this is not a GIT repository
							item.Status = "?";
							item.BranchName = string.Empty;
						}
					}
				}
				else
				{
					FetchGitStatus(item as VsFolder);
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
		private void UpdateList()
		{
			// TODO: must verify items before loading, indicate missing items through warning icon
			this.olvFiles.SetObjects(this.solutionGroups.Items);

			IterateAndSortItems();
			IterateAndExpandItems();
			FetchGitStatus(this.solutionGroups);
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