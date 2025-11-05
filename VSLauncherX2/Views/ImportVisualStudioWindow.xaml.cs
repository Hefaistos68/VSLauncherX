using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using VSLauncher.DataModel;
using VSLauncher.Helpers;

namespace VSLauncher.Views
{
	public partial class ImportVisualStudioWindow : Window
	{
		private readonly VisualStudioInstanceManager _vsManager = new();
		public ObservableCollection<VsItem> RootItems { get; } = new();
		public VsFolder ImportedRoot { get; private set; } = new();

		public ImportVisualStudioWindow()
		{
			InitializeComponent();
			Loaded += (_, _) => RefreshList();
		}

		private bool OnlyDefault => chkDefaultInstances.IsChecked == true;

		private void RefreshList()
		{
			RootItems.Clear();
			var items = _vsManager.GetRecentProjects(OnlyDefault);
			ImportedRoot = new VsFolder { Items = items };

			foreach (var i in items)
			{
				RootItems.Add(i);
			}
		}

		private void DefaultInstancesChanged(object sender, RoutedEventArgs e)
		{
			RefreshList();
		}

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			RefreshList();
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			// Collect checked items recursively
			VsItemList filtered = new VsItemList(null);
			CollectChecked(ImportedRoot.Items, filtered);
			ImportedRoot.Items = filtered;
			Tag = ImportedRoot;
			DialogResult = true;
			Close();
		}

		private void CollectChecked(VsItemList source, VsItemList target)
		{
			foreach (var item in source)
			{
				bool add = item is VsFolder f ? (f.Checked == true) : item.Checked;
				
				if (add)
				{
					if (item is VsFolder folder)
					{
						var clone = folder.Clone(false);
						VsItemList sub = new VsItemList(clone);
						CollectChecked(folder.Items, sub);
						clone.Items = sub;
						clone.Checked = false;
						target.Add(clone);
					}
					else
					{
						item.Checked = false;
						target.Add(item);
					}
				}
				else if (item is VsFolder subFolder && subFolder.Items.Count > 0)
				{
					VsItemList sub = new VsItemList(null);
					CollectChecked(subFolder.Items, sub);
					
					if (sub.Count > 0)
					{
						var clone = subFolder.Clone(false);
						clone.Items = sub;
						target.Add(clone);
					}
				}
			}
		}
	}
}
