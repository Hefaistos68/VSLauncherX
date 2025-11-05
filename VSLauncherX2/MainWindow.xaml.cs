using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VSLauncher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        if (txtFilter != null)
        {
            txtFilter.Text = string.Empty;
            txtFilter.Focus();
        }
    }

    // Append typed characters in TreeView to filter box
    private void TreeView_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (txtFilter == null) return;
        txtFilter.Focus();
        int caret = txtFilter.CaretIndex;
        string current = txtFilter.Text ?? string.Empty;
        // Insert at caret to allow continued typing after manual edits
        txtFilter.Text = current.Insert(caret, e.Text);
        txtFilter.CaretIndex = caret + e.Text.Length;
        e.Handled = true; // prevent default selection navigation
    }

    // Handle Backspace/Delete while focus in tree to edit filter
    private void TreeView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (txtFilter == null) return;
        if (e.Key == Key.Back || e.Key == Key.Delete)
        {
            txtFilter.Focus();
            int caret = txtFilter.CaretIndex;
            string current = txtFilter.Text ?? string.Empty;
            if (e.Key == Key.Back && caret > 0)
            {
                txtFilter.Text = current.Remove(caret - 1, 1);
                txtFilter.CaretIndex = caret - 1;
            }
            else if (e.Key == Key.Delete && caret < current.Length)
            {
                txtFilter.Text = current.Remove(caret, 1);
                txtFilter.CaretIndex = caret;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            txtFilter.Text = string.Empty;
            e.Handled = true;
        }
    }
}