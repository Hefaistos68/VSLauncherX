using System.Windows;
using Microsoft.Win32;
using VSLauncher.DataModel;
using VSLXshared.Helpers;

namespace VSLauncher.Views
{
	public partial class BeforeAfterWindow : Window
	{
		public VsOptions Options { get; }

		public BeforeAfterWindow(VsOptions options, string title)
		{
			InitializeComponent();
			Options = options;
			txtTitle.Text = title;
			Loaded += BeforeAfterWindow_Loaded;
		}

		private void BeforeAfterWindow_Loaded(object sender, RoutedEventArgs e)
		{
			txtRunBefore.Text = Options.RunBefore?.Path ?? string.Empty;
			txtRunAfter.Text = Options.RunAfter?.Path ?? string.Empty;
			txtArgumentsBefore.Text = Options.RunBefore?.Commands ?? string.Empty;
			txtArgumentsAfter.Text = Options.RunAfter?.Commands ?? string.Empty;
			chkWaitExitBefore.IsChecked = Options.RunBefore?.WaitForCompletion ?? false;
			chkWaitExitAfter.IsChecked = Options.RunAfter?.WaitForCompletion ?? false;
			UpdateEnableStates();
		}

		private void UpdateEnableStates()
		{
			chkWaitExitBefore.IsEnabled = !string.IsNullOrWhiteSpace(txtRunBefore.Text);
			txtArgumentsBefore.IsEnabled = chkWaitExitBefore.IsEnabled;
			chkWaitExitAfter.IsEnabled = !string.IsNullOrWhiteSpace(txtRunAfter.Text);
			txtArgumentsAfter.IsEnabled = chkWaitExitAfter.IsEnabled;
		}

		private void BtnSelectBefore_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new OpenFileDialog { Filter = FileHelper.ExecutablesFilterString };
			if (dlg.ShowDialog() == true)
			{
				txtRunBefore.Text = dlg.FileName;
			}
		}

		private void BtnSelectAfter_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new OpenFileDialog { Filter = FileHelper.ExecutablesFilterString };
			if (dlg.ShowDialog() == true)
			{
				txtRunAfter.Text = dlg.FileName;
			}
		}

		private void TxtRunBefore_TextChanged(object sender, RoutedEventArgs e) => UpdateEnableStates();
		private void TxtRunAfter_TextChanged(object sender, RoutedEventArgs e) => UpdateEnableStates();

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			Options.RunBefore = string.IsNullOrWhiteSpace(txtRunBefore.Text)
				? null
				: new VsItem(txtRunBefore.Text, txtRunBefore.Text, null)
				{
					WaitForCompletion = chkWaitExitBefore.IsChecked == true,
					Commands = txtArgumentsBefore.Text
				};

			Options.RunAfter = string.IsNullOrWhiteSpace(txtRunAfter.Text)
				? null
				: new VsItem(txtRunAfter.Text, txtRunAfter.Text, null)
				{
					WaitForCompletion = chkWaitExitAfter.IsChecked == true,
					Commands = txtArgumentsAfter.Text
				};

			DialogResult = true;
			Close();
		}
	}
}
