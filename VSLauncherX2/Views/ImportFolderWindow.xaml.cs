using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

using VSLauncher.DataModel;
using VSLauncher.Helpers;

namespace VSLauncher.Views
{
	public partial class ImportFolderWindow : Window
	{
		public ObservableCollection<VsItem> DisplayItems { get; } = new();
		private bool solutionsOnly = true;
		private bool flat = false;
		private VsFolder rootFolder = new VsFolder();

		public ImportFolderWindow()
		{
			InitializeComponent();
			DataContext = this;
			Loaded += ImportFolderWindow_Loaded;
		}

		private void ImportFolderWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty(Properties.Settings.Default.LastImportFolder))
			{
				txtFolder.Text = Properties.Settings.Default.LastImportFolder;
				UpdateList();
			}
			else
			{
				Browse_Click(this, new RoutedEventArgs());
			}
		}

		private void Browse_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new System.Windows.Forms.FolderBrowserDialog
			{
				SelectedPath = Properties.Settings.Default.LastImportFolder,
				ShowNewFolderButton = false,
				RootFolder = Environment.SpecialFolder.MyComputer
			};
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				txtFolder.Text = dlg.SelectedPath;
				Properties.Settings.Default.LastImportFolder = dlg.SelectedPath;
				UpdateList();
			}
		}

		private void OptionChanged(object sender, RoutedEventArgs e)
		{
			// Update only the option that changed; defer list refresh until window is fully loaded.
			if (ReferenceEquals(sender, chkSolutionsOnly))
			{
				solutionsOnly = chkSolutionsOnly.IsChecked == true;
			}
			else if (ReferenceEquals(sender, chkFlat))
			{
				flat = chkFlat.IsChecked == true;
			}

			// Avoid triggering UpdateList during InitializeComponent before all controls exist.
			if (IsLoaded)
			{
				UpdateList();
			}
		}

		private void UpdateList()
		{
			DisplayItems.Clear();
			rootFolder = new VsFolder(Path.GetFileName(txtFolder.Text), txtFolder.Text);
			if (!Directory.Exists(txtFolder.Text)) return;
			var items = IterateFolder(txtFolder.Text, solutionsOnly, flat);
			rootFolder.Items.Clear();

			if (!flat)
			{
				rootFolder.Items.Add(items);
			}
			else
			{
				foreach (var i in items.Items)
				{
					rootFolder.Items.Add(i);
				}
			}

			foreach (var i in rootFolder.Items)
			{
				DisplayItems.Add(i);
			}
		}

		private VsFolder IterateFolder(string folderPath, bool onlySolutions, bool flatMode)
		{
			var root = new VsFolder(Path.GetFileName(folderPath), folderPath);
			try
			{
				foreach (var folder in Directory.GetDirectories(folderPath))
				{
					if (folder.StartsWith('.'))
					{
						continue;
					}
					
					var attributes = File.GetAttributes(folder);

					if (attributes.HasFlag(FileAttributes.Hidden))
					{
						continue;
					}
					
					var subItem = IterateFolder(folder, onlySolutions, flatMode);

					if (subItem.Items.Count > 0)
					{
						if (!flatMode)
						{
							root.Items.Add(subItem);
						}
						else foreach (var c in subItem.Items)
							{
								root.Items.Add(c);
							}
					}
				}
				
				foreach (var file in Directory.GetFiles(folderPath))
				{
					if (file.StartsWith('.'))
					{
						continue;
					}
					if (IsOfInterest(file, onlySolutions))
					{
						var item = ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(file), file);
						item.LastModified = new FileInfo(file).LastAccessTime;
						root.Items.Add(item);
					}
				}
			}
			catch { }

			return root;
		}

		private static readonly string[] extensionsHandled = new[] { ".sln", ".csproj", ".tsproj", ".esproj", ".vcxproj", ".fsproj", ".vbproj" };
		private bool IsOfInterest(string file, bool onlySolutions)
		{
			return onlySolutions ? Path.GetExtension(file).Equals(".sln", StringComparison.OrdinalIgnoreCase)
				: extensionsHandled.Contains(Path.GetExtension(file).ToLowerInvariant());
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			VsFolder sg = new VsFolder(txtFolder.Text, txtFolder.Text);
			var checkedItems = CollectChecked(rootFolder.Items);
			foreach (var ci in checkedItems) sg.Items.Add(ci);
			sg.Checked = false;
			Tag = sg;
			DialogResult = true;
			Close();
		}

		private System.Collections.Generic.List<VsItem> CollectChecked(VsItemList list)
		{
			var result = new System.Collections.Generic.List<VsItem>();
			foreach (var i in list)
			{
				if (i is VsFolder f)
				{
					if (f.Checked == true) result.Add(f);
					result.AddRange(CollectChecked(f.Items));
				}
				else
				{
					if (i.Checked) result.Add(i);
				}
			}
			return result;
		}

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			UpdateList();
		}
	}
}
