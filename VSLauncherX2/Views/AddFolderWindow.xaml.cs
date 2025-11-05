using System.Windows;

namespace VSLauncher.Views
{
	public partial class AddFolderWindow : Window
	{
		public AddFolderWindow()
		{
			InitializeComponent();
			UpdateOkState();
			txtFolderName.TextChanged += (s,e)=>UpdateOkState();
		}

		private void UpdateOkState()
		{
			// Find OK button via name if needed; simpler: disable window default button using Tag
			// Tag will hold new folder name on success
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtFolderName.Text)) return;
			Tag = txtFolderName.Text.Trim();
			DialogResult = true;
			Close();
		}
	}
}
