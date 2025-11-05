using System.Windows;

namespace VSLauncher.Views
{
	public partial class WarnMultipleWindow : Window
	{
		public int Count { get; }
		public bool DontShow { get; set; }

		public WarnMultipleWindow(int count)
		{
			Count = count;
			DontShow = Properties.Settings.Default.DontShowMultiplesWarning;
			DataContext = this;
			InitializeComponent();
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			Properties.Settings.Default.DontShowMultiplesWarning = DontShow;
			Properties.Settings.Default.Save();
			DialogResult = true;
			Close();
		}
	}
}
