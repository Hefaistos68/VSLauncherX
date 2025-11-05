using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using VSLauncher.DataModel;
using VSLauncher.ViewModels;
using System.Collections.Generic;
using System.Diagnostics;
using VSLXshared.Helpers;
using WF = System.Windows.Forms; // alias for Screen

namespace VSLauncher.Views
{
	public partial class ExecuteVisualStudioWindow : Window
	{
		private MainViewModel? vm;
		private VsItem workingItem = new VsItem();

		private void ExecuteVisualStudioWindow_Loaded(object sender, RoutedEventArgs e)
		{
			vm = DataContext as MainViewModel ?? System.Windows.Application.Current.MainWindow?.DataContext as MainViewModel;
			
			if (Tag is VsItem existing)
{
				workingItem = existing;
}
			
			if (vm != null)
			{
				cbxInstance.Items.Clear();
				foreach (var inst in vm.SelectedVisualStudioVersion?.GetInstances() ?? new List<string>())
				{
					cbxInstance.Items.Add(inst);
				}

				if (cbxInstance.Items.Count > 0)
				{
					cbxInstance.SelectedIndex = 0;
				}

				var screens = WF.Screen.AllScreens;
				cbxMonitor.Items.Clear();
				cbxMonitor.Items.Add("<default>");

				foreach (var s in screens)
				{
					cbxMonitor.Items.Add(s.DeviceName);
				}
				
				cbxMonitor.SelectedIndex = 0;
			}

			txtName.Text = workingItem.Name ?? string.Empty;
			txtPath.Text = workingItem.Path ?? string.Empty;
			chkAdmin.IsChecked = workingItem.RunAsAdmin;
			chkSplash.IsChecked = workingItem.ShowSplash;
			cbxInstance.Text = string.IsNullOrWhiteSpace(workingItem.Instance) ? "<default>" : workingItem.Instance;
			txtCommand.Text = workingItem.Commands ?? string.Empty;
		}

		private void Browse_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new OpenFileDialog
			{
				Filter = FileHelper.SolutionFilterString,
				CheckFileExists = true,
				Multiselect = false
			};
			if (dlg.ShowDialog() == true)
			{
				txtPath.Text = dlg.FileName;
			}
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			workingItem.Name = txtName.Text;
			workingItem.Path = txtPath.Text;
			workingItem.RunAsAdmin = chkAdmin.IsChecked == true;
			workingItem.ShowSplash = chkSplash.IsChecked == true;
			workingItem.Instance = cbxInstance.Text == "<default>" ? null : cbxInstance.Text;
			workingItem.Commands = txtCommand.Text;
			if (cbxMonitor.SelectedIndex > 0) workingItem.PreferredMonitor = cbxMonitor.SelectedIndex - 1;
			Tag = workingItem;
			DialogResult = true;
			Close();
		}

		private void LinkVersions_Click(object sender, RoutedEventArgs e) => Process.Start("explorer.exe", "https://visualstudio.microsoft.com/vs/older-downloads/");

		private void LinkCommands_Click(object sender, RoutedEventArgs e) => Process.Start("explorer.exe", "https://learn.microsoft.com/en-us/visualstudio/ide/reference/command-devenv-exe");

		private void LinkInstances_Click(object sender, RoutedEventArgs e) => Process.Start("explorer.exe", "https://learn.microsoft.com/en-us/visualstudio/extensibility/devenv-command-line-switches-for-vspackage-development");

		private void PingMonitor_Click(object sender, RoutedEventArgs e)
		{
			// simple flash behavior
			if (cbxMonitor.SelectedIndex <= 0)
			{
				return;
			}

			var screen = WF.Screen.AllScreens[cbxMonitor.SelectedIndex - 1];
			var rect = screen.WorkingArea;
			var flash = new Window
			{
				WindowStyle = WindowStyle.None,
				ResizeMode = ResizeMode.NoResize,
				Left = rect.Left,
				Top = rect.Top,
				Width = rect.Width,
				Height = rect.Height,
				Background = System.Windows.Media.Brushes.Transparent,
				AllowsTransparency = true,
				Topmost = true,
				Opacity = 0.6
			};
			var border = new System.Windows.Shapes.Rectangle
			{
				Stroke = System.Windows.Media.Brushes.Red,
				StrokeThickness = 12,
				Fill = System.Windows.Media.Brushes.Transparent
			};
			flash.Content = border;
			flash.Show();
			var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(0.6) };
			t.Tick += (_, _) => { flash.Close(); t.Stop(); };
			t.Start();
		}

		private void BeforeAfter_Click(object sender, RoutedEventArgs e)
		{
			var opts = Tag as VsOptions ?? workingItem;
			var wnd = new BeforeAfterWindow(opts, workingItem.Name ?? "Item") { Owner = this };
			if (wnd.ShowDialog() == true)
			{
				// options updated in place
			}
		}
	}
}
