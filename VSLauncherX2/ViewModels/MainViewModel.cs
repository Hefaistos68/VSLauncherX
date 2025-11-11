using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic;
using System.Security.Principal;
using System.Threading.Tasks;
using System;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using LibGit2Sharp; // restore
using Newtonsoft.Json; // restore

using VSLauncher.DataModel;
using VSLauncher.Helpers;
using VSLXshared.Helpers;
using VSLauncher.Views;

namespace VSLauncher.ViewModels
{
	/// <summary>
	/// Main ViewModel for the VSLauncher WPF application
	/// </summary>
	public partial class MainViewModel : ObservableObject
	{
		#region Fields

		private readonly VisualStudioInstanceManager visualStudioInstances = new();
		private readonly DispatcherTimer              gitTimer               ;
		private bool                                   isInUpdate             ;
		// Capture a UI dispatcher early to avoid relying on Application.Current which can be null after shutdown/restart scenarios.
		private readonly Dispatcher                    uiDispatcher           ;

		// Filtered items backing store
		public ObservableCollection<VsItem> FilteredRootItems { get; } = new();

		#endregion

		#region Observable Properties

		[ObservableProperty]
		private VsFolder solutionGroups = new();

		// Ensure commands depending on SelectedItem re-query CanExecute when it changes
		[NotifyCanExecuteChangedFor(nameof(ShowItemSettingsCommand))]
		[NotifyCanExecuteChangedFor(nameof(RunItemCommand))]
		[NotifyCanExecuteChangedFor(nameof(RunItemAsAdminCommand))]
		[NotifyCanExecuteChangedFor(nameof(RenameItemCommand))]
		[NotifyCanExecuteChangedFor(nameof(RemoveItemCommand))]
		[NotifyCanExecuteChangedFor(nameof(ToggleFavoriteCommand))]
		[NotifyCanExecuteChangedFor(nameof(OpenInExplorerCommand))]
		[ObservableProperty]
		private VsItem? selectedItem;

		[ObservableProperty]
		private VisualStudioInstance? selectedVisualStudioVersion;

		[ObservableProperty]
		private string filterText = string.Empty;
		partial void OnFilterTextChanged(string value)
		{
			ApplyFilter();
		}

		[ObservableProperty]
		private string statusText = string.Empty;

		[ObservableProperty]
		private bool isGitStatusUpdating;

		[ObservableProperty]
		private ImageSource? selectedVsIcon; // icon for selected VS instance

		#endregion

		#region Collections

		public ObservableCollection<VisualStudioInstance> VisualStudioVersions { get; }

		#endregion

		#region Constructor

		public MainViewModel()
		{
			// Capture dispatcher (Application.Current may be null later if app shuts down for restart)
			uiDispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

			// Initialize collections
			VisualStudioVersions = new ObservableCollection<VisualStudioInstance>();
			foreach (var vs in visualStudioInstances.All)
			{
				VisualStudioVersions.Add(vs);
			}

			// Initialize Git timer
			gitTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(20)
			};
			gitTimer.Tick += GitTimer_Tick;

			// Load data
			LoadSolutionData();

			// Initial Git status update
			FetchGitStatusAsync();

			// Subscribe to change events
			solutionGroups.Items.OnChanged += SolutionData_OnChanged;

			// Load saved VS version
			LoadSelectedVisualStudioVersion();

			// Start Git timer
			gitTimer.Start();

			ApplyFilter();
		}

		private void ApplyFilter()
		{
			FilteredRootItems.Clear();
			string ft = FilterText?.Trim();

			if (string.IsNullOrEmpty(ft))
			{
				foreach (var i in SolutionGroups.Items)
				{
					FilteredRootItems.Add(i);
				}

				return;
			}

			foreach (var root in SolutionGroups.Items)
			{
				var filtered = FilterRecursive(root, ft);
				if (filtered != null)
					FilteredRootItems.Add(filtered);
			}
		}

		private VsItem? FilterRecursive(VsItem item, string term)
		{
			bool IsMatch(VsItem i) => (i.Name ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;

			if (item is VsFolder folder)
			{
				VsFolder clone = new VsFolder(folder.Name ?? string.Empty, folder.Path ?? string.Empty)
				{ ItemType = folder.ItemType, VsVersion = folder.VsVersion, Expanded = folder.Expanded };

				foreach (var child in folder.Items)
				{
					var fc = FilterRecursive(child, term);
					if (fc != null)
						clone.Items.Add(fc);
				}

				if (clone.Items.Count > 0 || IsMatch(folder))
					return clone;

				return null;
			}
			else if (item is VsSolution sol)
			{
				if (IsMatch(sol))
					return sol; // show solution itself if matches

				// filter projects within solution
				VsSolution solClone = new VsSolution(sol.Name ?? string.Empty, sol.Path ?? string.Empty, sol.SolutionType) { VsVersion = sol.VsVersion, RequiredVersion = sol.RequiredVersion };
				foreach (var proj in sol.Projects)
				{
					var fc = FilterRecursive(proj, term);
					if (fc != null)
						solClone.Projects.Add(fc);
				}

				if (solClone.Projects.Count > 0)
					return solClone;

				return null;
			}
			else
			{
				return IsMatch(item) ? item : null;
			}
		}

		#endregion

		#region Command Implementations

		/// <summary>
		/// Add a new folder/group
		/// </summary>
		[RelayCommand]
		private void AddFolder()
		{
			var wnd = new AddFolderWindow();
			bool? result = wnd.ShowDialog();

			if (result == true)
			{
				string? name = wnd.Tag as string;
				if (string.IsNullOrWhiteSpace(name)) return;
				var newFolder = new VsFolder { Name = name.Trim(), ItemType = ItemTypeEnum.Folder };

				if (SelectedItem is VsFolder parent)
				{
					parent.Items.Add(newFolder);
					parent.Items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
				}
				else
				{
					SolutionGroups.Items.Add(newFolder);
					SolutionGroups.Items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
				}

				OnSolutionDataChanged();
			}
		}

		/// <summary>
		/// Import solutions/projects from a folder
		/// </summary>
		[RelayCommand]
		private void ImportFolder()
		{
			var wnd = new ImportFolderWindow();
			bool? result = wnd.ShowDialog();

			if (result == true && wnd.Tag is VsFolder importedRoot)
			{
				MergeNewItems(importedRoot.Items);
				OnSolutionDataChanged();
			}
		}

		private void MergeNewItems(VsItemList source)
		{
			if (source == null || source.Count == 0)
			{
				return;
			}

			if (SelectedItem is VsFolder folder)
			{
				foreach (var item in source)
				{
					folder.Items.Add(item);
				}

				folder.Items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			}
			else
			{
				VsFolder? parent = SolutionGroups.FindParent(SelectedItem);
				if (parent != null)
				{
					foreach (var item in source)
					{
						parent.Items.Add(item);
					}

					parent.Items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
				}
				else
				{
					foreach (var item in source)
					{
						SolutionGroups.Items.Add(item);
					}

					SolutionGroups.Items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
				}
			}
		}

		/// <summary>
		/// Import from Visual Studio MRU list
		/// </summary>
		[RelayCommand]
		private void ImportVisualStudio()
		{
			var wnd = new ImportVisualStudioWindow
			{
				Owner = Application.Current.MainWindow
			};
			bool? result = wnd.ShowDialog();

			if (result == true && wnd.Tag is VsFolder importedRoot)
			{
				MergeNewItems(importedRoot.Items);
				OnSolutionDataChanged();
			}
		}

		/// <summary>
		/// Import a specific solution or project file
		/// </summary>
		[RelayCommand]
		private void ImportSolutionOrProject()
		{
			var dialog = new Microsoft.Win32.OpenFileDialog
			{
				Filter = FileHelper.SolutionFilterString,
				Title = "Select a solution or project file"
			};

			if (dialog.ShowDialog() == true)
			{
				VsItem item = ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(dialog.FileName), dialog.FileName);

				if (item != null)
				{
					MergeNewItem(item);
					ShowItemSettings(item);
					OnSolutionDataChanged();
				}
			}
		}

		/// <summary>
		/// Refresh the solution list
		/// </summary>
		[RelayCommand]
		private void Refresh()
		{
			SolutionGroups.Items.OnChanged -= SolutionData_OnChanged;
			SolutionGroups.Items.Clear();
			LoadSolutionData();
			SolutionGroups.Items.OnChanged += SolutionData_OnChanged;
			FetchGitStatusAsync();
		}

		/// <summary>
		/// Show application settings
		/// </summary>
		[RelayCommand]
		private void ShowSettings()
		{
			var wnd = new SettingsWindow();
			if (wnd.ShowDialog() == true)
			{
				ApplySettingsSideEffects();
			}
		}

		/// <summary>
		/// Show settings for the selected item
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteItemCommands))]
		private void ShowItemSettings()
		{
			if (SelectedItem == null)
			{
				return;
			}

			ShowItemSettings(SelectedItem);
		}

		/// <summary>
		/// Show settings for the item
		/// </summary>
		private void ShowItemSettings(VsItem item)
		{
			var wnd = new ExecuteVisualStudioWindow
			{
				Owner = Application.Current.MainWindow,
				Tag   = item
			};
			bool? result = wnd.ShowDialog();

			if (result == true)
			{
				OnSolutionDataChanged();
			}
		}

		private void ApplySettingsSideEffects()
		{
			Properties.Settings.Default.Save();

			if (Properties.Settings.Default.AlwaysAdmin || Properties.Settings.Default.AutoStart)
			{
				if (Properties.Settings.Default.AlwaysAdmin)
				{
					bool isElevated = AdminInfo.IsCurrentUserAdmin() || AdminInfo.IsElevated();

					if (!isElevated)
					{
						var exe = Process.GetCurrentProcess().MainModule?.FileName;

						if (!string.IsNullOrEmpty(exe))
						{
							Process.Start(new ProcessStartInfo
							{
								FileName        = exe,
								UseShellExecute = true,
								Verb            = "runas"
							});
							Application.Current.Shutdown();
						}
					}
				}
				else
				{
					App.UpdateTaskScheduler();
				}
			}
			else
			{
				App.RemoveTaskScheduler();
			}

			OnSolutionDataChanged();
		}

		/// <summary>
		/// Run the selected item
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteItemCommands))]
		private async Task RunItem()
		{
			if (SelectedItem== null)
			{
				return;
			}

			await LaunchItem(SelectedItem, false);
		}

		/// <summary>
		/// Run the selected item as administrator
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteItemCommands))]
		private async Task RunItemAsAdmin()
		{
			if (SelectedItem== null)
			{
				return;
			}

			await LaunchItem(SelectedItem, true);
		}

		/// <summary>
		/// Rename the selected item (simple InputBox prompt implementation)
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteItemCommands))]
		private void RenameItem()
		{
			if (SelectedItem is null)
			{
				return;
			}

			string currentName = SelectedItem.Name ?? string.Empty;
			// Use VB Interaction.InputBox for quick prompt (avoids creating extra WPF dialog for now)
			string newName = Interaction.InputBox("Enter new name:", "Rename Item", currentName);

			// Cancel returns empty string when user presses Cancel and currentName was empty; guard by comparing
			if (string.IsNullOrWhiteSpace(newName) || newName.Equals(currentName, StringComparison.Ordinal))
			{
				return;
			}

			SelectedItem.Name = newName.Trim();
			OnSolutionDataChanged();
		}

		/// <summary>
		/// Remove the selected item
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteItemCommands))]
		private void RemoveItem()
		{
			if (SelectedItem== null)
			{
				return;
			}

			// Only ask if not an empty folder
			if (SelectedItem is not VsFolder folder || folder.Items.Count != 0)
			{
				var result = MessageBox.Show($"Are you sure you want to delete '{SelectedItem.Name}'?", "Delete item", MessageBoxButton.YesNo, MessageBoxImage.Question);

				if (result != MessageBoxResult.Yes)
				{
					return;
				}
			}

			VsFolder? owner = SolutionGroups.FindParent(SelectedItem);

			if (owner != null)
			{
				owner.Items.Remove(SelectedItem);
				OnSolutionDataChanged();
			}
		}

		/// <summary>
		/// Toggle favorite status of selected item
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteItemCommands))]
		private void ToggleFavorite()
		{
			if (SelectedItem== null)
			{
				return;
			}

			SelectedItem.IsFavorite = !SelectedItem.IsFavorite;
			// TODO: Rebuild taskbar jump list
			SaveSolutionData();
		}

		/// <summary>
		/// Open the selected item in Windows Explorer
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteItemCommands))]
		private void OpenInExplorer()
		{
			if (SelectedItem == null || SelectedItem is VsFolder)
			{
				return;
			}

			Process.Start("explorer.exe", $"/select, \"{SelectedItem.Path}\"");
		}

		/// <summary>
		/// Launch Visual Studio with the selected instance
		/// </summary>
		[RelayCommand]
		private void StartVs()
		{
			if (SelectedVisualStudioVersion!= null)
			{
				SelectedVisualStudioVersion.Execute();
			}
		}

		/// <summary>
		/// Launch Visual Studio as administrator with the selected instance
		/// </summary>
		[RelayCommand]
		private void StartVsAdmin()
		{
			if (SelectedVisualStudioVersion!= null)
			{
				SelectedVisualStudioVersion.ExecuteAsAdmin();
			}
		}

		/// <summary>
		/// Launch a new instance of Visual Studio with the selected instance
		/// </summary>
		[RelayCommand]
		private void StartVsNewInstance()
		{
			if (SelectedVisualStudioVersion!= null)
			{
				// use VB interaction to request name of instance
				string instanceName = Interaction.InputBox("Enter instance name:", "New Instance", "Exp");

				if (string.IsNullOrWhiteSpace(instanceName))
				{
					return;
				}

				SelectedVisualStudioVersion.ExecuteWithInstance(false, instanceName);
			}
		}

		/// <summary>
		/// Launch a new project with the selected instance of Visual Studio
		/// </summary>
		[RelayCommand]
		private void StartVsNewProject()
		{
			if (SelectedVisualStudioVersion!= null)
			{
				SelectedVisualStudioVersion.ExecuteNewProject(false);
			}
		}

		/// <summary>
		/// Show Visual Studio launch options dialog
		/// </summary>
		[RelayCommand]
		private void StartVsDialog()
		{
			if (SelectedVisualStudioVersion == null)
			{
				SelectedVisualStudioVersion?.Execute();
				return;
			}

			var wnd = new ExecuteVisualStudioWindow
			{
				Owner = Application.Current.MainWindow,
				Tag   = SelectedItem
			};
			bool? result = wnd.ShowDialog();

			if (result == true)
			{
				if (wnd.Tag is VsItem updated)
				{
					var vs = SelectedVisualStudioVersion;
					vs.ExecuteWith(updated.RunAsAdmin, updated.ShowSplash, updated.Path ?? string.Empty, updated.Instance, updated.Commands);
				}
				else
				{
					SelectedVisualStudioVersion.Execute();
				}
			}
		}

		/// <summary>
		/// Open the Activity Log for the selected instance of Visual Studio
		/// </summary>
		[RelayCommand]
		private void OpenActivityLog()
		{
			if (SelectedVisualStudioVersion!= null)
			{
				var vs     = SelectedVisualStudioVersion;
				string version = $"{vs.MainVersion}.0_{vs.Identifier}";
				string s      = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string file   = Path.Combine(s, "Microsoft", "VisualStudio", version, "ActivityLog.xml");

				if (File.Exists(file))
				{
					Process.Start(new ProcessStartInfo
					{
						FileName        = file,
						UseShellExecute = true,
						Verb            = "open"
					});
				}
			}
		}

		/// <summary>
		/// Open the Visual Studio Installer (or VS download page if not installed)
		/// </summary>
		[RelayCommand]
		private void OpenVsInstaller()
		{
			string installer = VisualStudioInstanceManager.InstallerPath;

			try
			{
				if (installer.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				{
					// Open download URL in default browser
					Process.Start(new ProcessStartInfo
					{
						FileName        = installer,
						UseShellExecute = true
					});
					return;
				}

				// Local installer executable
				bool isElevated = AdminInfo.IsCurrentUserAdmin() || AdminInfo.IsElevated();
				if (!isElevated)
				{
					var result = MessageBox.Show(
						"The Visual Studio Installer may require elevated privileges. Run as administrator?",
						"Start Visual Studio Installer",
						MessageBoxButton.YesNo,
						MessageBoxImage.Question);

					if (result == MessageBoxResult.Yes)
					{
						Process.Start(new ProcessStartInfo
						{
							FileName        = installer,
							UseShellExecute = true,
							Verb            = "runas"
						});
						return;
					}
				}

				// Normal launch
				Process.Start(new ProcessStartInfo
				{
					FileName        = installer,
					UseShellExecute = true
				});
			}
			catch (System.ComponentModel.Win32Exception ex)
			{
				MessageBox.Show($"Failed to start Visual Studio Installer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private bool CanExecuteItemCommands() => SelectedItem!= null;

		#endregion

		#region Git Operations

		private void GitTimer_Tick(object? sender, EventArgs e)
		{
			// Only update if window is active
			if (Application.Current?.MainWindow?.IsActive == true)
			{
				FetchGitStatusAsync();
			}
		}

		private void FetchGitStatusAsync()
		{
			IsGitStatusUpdating = true;

			Task.Run(() =>
				  {
					  FetchGitStatusForFolder(SolutionGroups);
				  }).ContinueWith(t =>
				{
					IsGitStatusUpdating = false;
					// redraw the treeview column 2 (Git status) by raising PropertyChanged for all items
					void RaiseForItems(VsFolder folder)
					{
						foreach (var item in folder.Items)
						{
							if (item is VsFolder subFolder)
							{
								RaiseForItems(subFolder);
							}
							else
							{
								OnPropertyChanged(nameof(item.Status));
								OnPropertyChanged(nameof(item.BranchName));
							}
						}
					}
				}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private void FetchGitStatusForFolder(VsFolder folder)
		{
			foreach (var item in folder.Items)
			{
				if (item is VsFolder subFolder)
				{
					FetchGitStatusForFolder(subFolder);
				}
				else
				{
					UpdateGitStatus(item);
				}
			}
		}

		private void UpdateGitStatus(VsItem item)
		{
			string? status     = "?";
			string? branchName = null;

			try
			{
				var repoPath = Path.GetDirectoryName(item.Path);
				if (repoPath != null && Directory.Exists(Path.Combine(repoPath, ".git")))
				{
					using var repo = new Repository(repoPath);
					var stat       = repo.RetrieveStatus();
					status         = stat.IsDirty ? "*" : "!";

					branchName     = repo.Head.FriendlyName;
				}
				else if (repoPath != null)
				{
					// Try parent directory
					var parentPath = Path.GetDirectoryName(repoPath);
					if (parentPath != null && Directory.Exists(Path.Combine(parentPath, ".git")))
					{
						using var repo = new Repository(parentPath);
						var stat       = repo.RetrieveStatus();
						status         = stat.IsDirty ? "*" : "!";

						branchName     = repo.Head.FriendlyName;
					}
				}
			}
			catch (RepositoryNotFoundException)
			{
				status     = "?";
				branchName = string.Empty;
			}
			catch (Exception)
			{
				status     = "?";
				branchName = string.Empty;
			}

			// Update on UI thread (guard against null Application.Current)
			var dispatcher = Application.Current?.Dispatcher ?? uiDispatcher;
			if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
			{
				return; // cannot update UI
			}

			dispatcher.BeginInvoke(() =>
					  {
						  item.Status     = status;
						  item.BranchName = branchName ?? string.Empty;
					  });
		}

		#endregion

		#region Data Management

		private void LoadSolutionData()
		{
			string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VSLauncher", "VSLauncher.json");

			if (File.Exists(fileName))
			{
				try
				{
					string json      = File.ReadAllText(fileName);
					var settings     = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
					var data         = JsonConvert.DeserializeObject<VsFolder>(json, settings);
					if (data != null)
					{
						data.Refresh();
						RestoreExpandedState(data);
						SolutionGroups = data;
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Error loading solution data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void SaveSolutionData()
		{
			CaptureExpandedState(SolutionGroups);
			string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VSLauncher", "VSLauncher.json");

			try
			{
				string? dir = Path.GetDirectoryName(fileName);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}

				var settings = new JsonSerializerSettings { Formatting = Formatting.Indented, TypeNameHandling = TypeNameHandling.All };
				string json   = JsonConvert.SerializeObject(SolutionGroups, settings);
				File.WriteAllText(fileName, json);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error saving solution data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void CaptureExpandedState(VsFolder root)
		{
			foreach (var item in root.Items)
			{
				if (item is VsFolder f)
				{
					// Expanded already reflects UI state (updated via events)
					CaptureExpandedState(f);
				}
			}
		}

		private void RestoreExpandedState(VsFolder root)
		{
			foreach (var item in root.Items)
			{
				if (item is VsFolder f)
				{
					// Expanded loaded from persisted file; recurse
					RestoreExpandedState(f);
				}
			}
		}

		public void UpdateFolderExpanded(VsFolder folder, bool expanded)
		{
			if (folder == null) return;

			folder.Expanded = expanded;
			// Removed: SolutionGroups.Items.Changed = true; to avoid re-entrant ApplyFilter during TreeView expansion causing cyclic Style evaluation.
		}

		#region Restored Methods

		private void LoadSelectedVisualStudioVersion()
		{
			isInUpdate = true;

			if (!string.IsNullOrEmpty(Properties.Settings.Default.SelectedVSversion))
			{
				SelectedVisualStudioVersion = VisualStudioVersions.FirstOrDefault(v => v.Identifier == Properties.Settings.Default.SelectedVSversion);
			}

			if (SelectedVisualStudioVersion is null)
			{
				SelectedVisualStudioVersion = VisualStudioVersions.FirstOrDefault();
			}

			isInUpdate = false;
		}

		private bool SolutionData_OnChanged(bool changed)
		{
			if (changed)
			{
				SolutionGroups.LastModified = DateTime.Now;
				SaveSolutionData();
				ApplyFilter();
			}

			return false;
		}

		private void OnSolutionDataChanged()
		{
			SolutionData_OnChanged(true);
		}

		private async Task LaunchItem(VsItem item, bool asAdmin)
		{
			VisualStudioInstance? vs        = SelectedVisualStudioVersion;
			ItemLauncher?         launcher  = null;

			if (item is VsFolder folder)
			{
				if (!Properties.Settings.Default.DontShowMultiplesWarning)
				{
					var count = folder.ContainedSolutionsCount() + folder.ContainedProjectsCount();
					if (count > 3)
					{
						var warn = new WarnMultipleWindow(count) { Owner = Application.Current.MainWindow };
						if (warn.ShowDialog() != true)
							return;
					}
				}
				vs       = string.IsNullOrEmpty(folder.VsVersion) ? visualStudioInstances.GetByIdentifier(folder.VsVersion) : vs;
				launcher = new ItemLauncher(folder, vs);
			}
			else if (item is VsSolution solution)
			{
				vs       = visualStudioInstances.GetByVersion(solution.RequiredVersion) ?? vs;
				launcher = new ItemLauncher(solution, vs);
			}
			else if (item is VsProject project)
			{
				vs       = visualStudioInstances.GetByIdentifier(project.VsVersion) ?? vs;
				launcher = new ItemLauncher(project, vs);
			}

			if (launcher != null)
			{
				StatusText = $"Launching '{item.Name}'...";
				try
				{
					await launcher.Launch(asAdmin);
					if (launcher.LastException != null)
					{
						MessageBox.Show(launcher.LastException.Message, "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
				finally
				{
					StatusText = string.Empty;
				}
			}
		}

		private void MergeNewItem(VsItem item)
		{
			if (SelectedItem is VsFolder folder)
			{
				folder.Items.Add(item);
				folder.Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
			}
			else
			{
				VsFolder? parent = SolutionGroups.FindParent(SelectedItem);
				if (parent != null)
				{
					parent.Items.Add(item);
					parent.Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
				}
				else
				{
					SolutionGroups.Items.Add(item);
					SolutionGroups.Items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
				}
			}
		}

		partial void OnSelectedVisualStudioVersionChanged(VisualStudioInstance? value)
		{
			if (value == null)
			{
				SelectedVsIcon = null;
				return;
			}

			try
			{
				var iconHandle = value.AppIcon.Handle;
				var src        = Imaging.CreateBitmapSourceFromHIcon(iconHandle, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(32, 32));
				src.Freeze();
				SelectedVsIcon = src;
			}
			catch
			{
				SelectedVsIcon = null;
			}
		}

		#endregion // Restored Methods
		#endregion // Data Management
	}
}
