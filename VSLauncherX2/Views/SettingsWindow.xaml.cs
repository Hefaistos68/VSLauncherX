using System.Windows;

namespace VSLauncher.Views
{
	public partial class SettingsWindow : Window
	{
		private string _initialTheme = "System";

		public SettingsWindow()
		{
			InitializeComponent();
			// Load current settings
			chkAlwaysAdmin.IsChecked = Properties.Settings.Default.AlwaysAdmin;
			chkAutoStart.IsChecked = Properties.Settings.Default.AutoStart;
			chkSync.IsChecked = Properties.Settings.Default.SynchronizeVS;
			chkShowPath.IsChecked = Properties.Settings.Default.ShowPathForSolutions;

			// Load theme preference (default System)
			string theme = Properties.Settings.Default.PreferredTheme ?? "System";
			_initialTheme = theme;

			foreach (var item in cboTheme.Items)
			{
				if (item is System.Windows.Controls.ComboBoxItem cbi && (string)cbi.Content == theme)
				{
					cboTheme.SelectedItem = item;
					break;
				}
			}

			if (cboTheme.SelectedIndex < 0)
			{
				cboTheme.SelectedIndex = 0; // ensure selection
			}

			cboTheme.SelectionChanged += CboTheme_SelectionChanged; // live preview
		}

		private void CboTheme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (cboTheme.SelectedItem is System.Windows.Controls.ComboBoxItem cbi)
			{
				Properties.Settings.Default.PreferredTheme = (string)cbi.Content;
				App.ReapplyTheme();
			}
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			Properties.Settings.Default.AlwaysAdmin = chkAlwaysAdmin.IsChecked == true;
			Properties.Settings.Default.AutoStart = chkAutoStart.IsChecked == true;
			Properties.Settings.Default.SynchronizeVS = chkSync.IsChecked == true;
			Properties.Settings.Default.ShowPathForSolutions = chkShowPath.IsChecked == true;
			Properties.Settings.Default.Save();
			DialogResult = true;
			Close();
		}
	}
}
