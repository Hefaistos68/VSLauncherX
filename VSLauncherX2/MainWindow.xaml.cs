using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VSLauncher.ViewModels;
using VSLauncher.DataModel;
using LibGit2Sharp;
using System;
using System.IO;
using System.Linq;

namespace VSLauncher;

/// <summary>
/// Main application window hosting the tree view and filter textbox.
/// Handles UI interactions and forwards state changes to the <see cref="MainViewModel"/>.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// Wires TreeView expand/collapse events to persist expanded state through the view-model.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        treeViewItems.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(OnTreeItemExpanded));
        treeViewItems.AddHandler(TreeViewItem.CollapsedEvent, new RoutedEventHandler(OnTreeItemCollapsed));

        // Hook preview mouse right button to select item under cursor before context menu opens
        treeViewItems.PreviewMouseRightButtonDown += TreeViewItems_PreviewMouseRightButtonDown;
    }

    private void TreeViewItems_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Hit test to find the TreeViewItem
        var hit = VisualTreeHelper.HitTest(treeViewItems, e.GetPosition(treeViewItems));
        if (hit == null)
        {
            return;
        }

        DependencyObject current = hit.VisualHit;
        while (current != null && current is not TreeViewItem)
        {
            current = VisualTreeHelper.GetParent(current);
        }

        if (current is TreeViewItem tvi)
        {
            tvi.IsSelected = true;
            tvi.Focus();
        }
    }

    /// <summary>
    /// Gets the strongly typed view-model bound to this window's <see cref="FrameworkElement.DataContext"/>.
    /// </summary>
    private MainViewModel? VM => DataContext as MainViewModel;

    /// <summary>
    /// Clears the filter textbox and re-focuses it for continued typing.
    /// </summary>
    /// <param name="sender">The button initiating the clear action.</param>
    /// <param name="e">The routed event args.</param>
    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        if (txtFilter != null)
        {
            txtFilter.Text = string.Empty;
            txtFilter.Focus();
        }
    }

    /// <summary>
    /// Handles selection changes in the TreeView, updating the view-model's SelectedItem.
    /// </summary>
    /// <param name="sender">The TreeView raising the event.</param>
    /// <param name="e">The selection change event args containing new value.</param>
    private void TreeViewItems_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (VM != null)
        {
            VM.SelectedItem = e.NewValue as VsItem;
        }
    }

    /// <summary>
    /// Captures folder expansion state when a TreeViewItem is expanded.
    /// </summary>
    /// <param name="sender">The TreeView or item raising the event.</param>
    /// <param name="e">The routed event args.</param>
    private void OnTreeItemExpanded(object sender, RoutedEventArgs e)
    {
        if (VM == null)
        {
            return;
        }

        if (e.OriginalSource is TreeViewItem tvi && tvi.DataContext is VsFolder f)
        {
            VM.UpdateFolderExpanded(f, true);
        }
    }

    /// <summary>
    /// Captures folder collapse state when a TreeViewItem is collapsed.
    /// </summary>
    /// <param name="sender">The TreeView or item raising the event.</param>
    /// <param name="e">The routed event args.</param>
    private void OnTreeItemCollapsed(object sender, RoutedEventArgs e)
    {
        if (VM == null) 
{
            return;
}

        if (e.OriginalSource is TreeViewItem tvi && tvi.DataContext is VsFolder f)
        {
            VM.UpdateFolderExpanded(f, false);
        }
    }

    /// <summary>
    /// Redirects typed characters in the TreeView to the filter textbox to enable incremental search.
    /// </summary>
    /// <param name="sender">The TreeView handling text input.</param>
    /// <param name="e">The text composition event args containing typed text.</param>
    private void TreeView_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (txtFilter == null)
        {
            return;
        }

        txtFilter.Focus();

        int caret = txtFilter.CaretIndex;
        string current = txtFilter.Text ?? string.Empty;

        // Insert at caret to allow continued typing after manual edits
        txtFilter.Text = current.Insert(caret, e.Text);
        txtFilter.CaretIndex = caret + e.Text.Length;

        e.Handled = true; // prevent default selection navigation
    }

    /// <summary>
    /// Handles Backspace, Delete, and Escape keys while focus is in the TreeView to manipulate the filter textbox.
    /// </summary>
    /// <param name="sender">The TreeView raising the key event.</param>
    /// <param name="e">The key event args.</param>
    private void TreeView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (txtFilter == null) 
{
            return;
}

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

    private void TreeItemContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu cm)
        {
            return;
        }

        var item = treeViewItems.SelectedItem as VsItem;

        var miGit = cm.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "miGit");
        var miBranchesContainer = cm.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "miGitBranches");
        var miFetch = cm.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "miGitFetch");
        var miPull = cm.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "miGitPull");

        if (miGit == null || miBranchesContainer == null || miFetch == null || miPull == null)
        {
            return;
        }

        miBranchesContainer.Items.Clear();

        if (item == null || item is VsFolder || string.IsNullOrEmpty(item.Path))
        {
            miGit.IsEnabled = false;
            miBranchesContainer.IsEnabled = false;
            miFetch.IsEnabled = false;
            miPull.IsEnabled = false;
            return;
        }

        string? repoDir = Path.GetDirectoryName(item.Path);
        if (repoDir == null)
        {
            miGit.IsEnabled = false;
            miBranchesContainer.IsEnabled = false;
            miFetch.IsEnabled = false;
            miPull.IsEnabled = false;
            return;
        }

        // Find .git directory walking up
        string? current = repoDir;
        while (current != null && !Directory.Exists(Path.Combine(current, ".git")))
        {
            current = Path.GetDirectoryName(current);
        }

        if (current == null)
        {
            miGit.IsEnabled = false;
            miBranchesContainer.IsEnabled = false;
            miFetch.IsEnabled = false;
            miPull.IsEnabled = false;
            return;
        }

        try
        {
            using var repo = new Repository(current);
            miGit.IsEnabled = true;
            miFetch.IsEnabled = true;
            miPull.IsEnabled = true;

            foreach (var br in repo.Branches.OrderBy(b => b.FriendlyName))
            {
                var branchItem = new MenuItem
                {
                    Header = br.FriendlyName,
                    IsCheckable = true,
                    IsChecked = br.IsCurrentRepositoryHead
                };

                branchItem.Click += (s, args) => CheckoutBranch(current, br.FriendlyName);
                miBranchesContainer.Items.Add(branchItem);
            }
            miBranchesContainer.IsEnabled = miBranchesContainer.Items.Count > 0;
        }
        catch
        {
            miGit.IsEnabled = false;
            miBranchesContainer.IsEnabled = false;
            miFetch.IsEnabled = false;
            miPull.IsEnabled = false;
        }
    }

    private void GitFetch_Click(object sender, RoutedEventArgs e)
    {
        var item = treeViewItems.SelectedItem as VsItem;
        if (item == null || item is VsFolder || string.IsNullOrEmpty(item.Path))
        {
            return;
        }

        var repoDir = FindRepoRoot(Path.GetDirectoryName(item.Path));
        if (repoDir == null)
        {
            return;
        }

        try
        {
            using var repo = new Repository(repoDir);
            var remote = repo.Network.Remotes[repo.Head.RemoteName];
            var fetchOptions = new FetchOptions();
            Commands.Fetch(repo, remote.Name, remote.FetchRefSpecs.Select(rs => rs.Specification), fetchOptions, null);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fetch failed: {ex.Message}", "Git Fetch", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GitPull_Click(object sender, RoutedEventArgs e)
    {
        var item = treeViewItems.SelectedItem as VsItem;
        if (item == null || item is VsFolder || string.IsNullOrEmpty(item.Path))
        {
            return;
        }

        var repoDir = FindRepoRoot(Path.GetDirectoryName(item.Path));
        if (repoDir == null)
        {
            return;
        }

        try
        {
            using var repo = new Repository(repoDir);
            var stat = repo.RetrieveStatus();
            if (stat.IsDirty)
            {
                if (MessageBox.Show("Uncommitted changes detected. Continue pull?", "Git Pull", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    return;
                }
            }
            var pullOptions = new PullOptions { FetchOptions = new FetchOptions() };
            Commands.Pull(repo, new Signature("VSLauncherX", "user@example.com", DateTimeOffset.Now), pullOptions);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Pull failed: {ex.Message}", "Git Pull", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string? FindRepoRoot(string? start)
    {
        string? current = start;
        while (current != null && !Directory.Exists(Path.Combine(current, ".git")))
        {
            current = Path.GetDirectoryName(current);
        }
        return current;
    }

    private void CheckoutBranch(string repoRoot, string branchName)
    {
        try
        {
            using var repo = new Repository(repoRoot);
            var target = repo.Branches[branchName];
            if (target == null)
            {
                MessageBox.Show($"Branch '{branchName}' not found.", "Git Checkout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Commands.Checkout(repo, target);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Checkout failed: {ex.Message}", "Git Checkout", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}