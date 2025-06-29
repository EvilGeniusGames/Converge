using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Reactive;
using Avalonia.VisualTree;
using Converge.Data;
using Converge.Data.Services;
using Converge.Models;
using Converge.ViewModels;
using Converge.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Org.BouncyCastle.Crmf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Converge.Views
{
    public partial class MainWindow : Window
    {
        // Drag drop related fields
        private ConnectionTreeItem? _draggedItem = null;
        private TreeViewItem? _lastDropTarget = null;
        private ConnectionTreeItem? _dragSourceItem;
        private Point _dragStartPoint;
        private bool _isDragInitiated;
        // Store expanded node IDs to preserve expansion state across reloads
        private HashSet<int> _expandedFolderIds = new();
        private HashSet<int> _expandedConnectionIds = new();


        // Constructor initializes the main window and sets up the data context
        public MainWindow()
        {
            // Constructor for MainWindow
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Closing += OnWindowClosing;
        }
        // Event handler for when the window is opened
        protected override async void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            // Set window icon
            if (OperatingSystem.IsWindows())
            {
                using var stream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://Converge/Assets/icon.ico"));
                this.Icon = new WindowIcon(stream);
            }
            else
            {
                this.Icon = new WindowIcon("avares://Converge/Assets/icon.png");
            }

            // Ensure DB and vault
            var db = Program.Services.GetRequiredService<ConvergeDbContext>();
            db.Database.Migrate();
            await VerifyOrCreateVaultAsync();

            // Get ViewModel early
            var vm = DataContext as MainWindowViewModel;

            // Load width from settings
            var setting = db.SiteSettings.FirstOrDefault(s => s.Key == "LastPaneWidth");
            if (setting != null && double.TryParse(setting.Value, out var storedWidth) && storedWidth > 42)
            {
                vm!.LeftPaneWidth = new GridLength(storedWidth);
            }

            // Load connections
            if (vm != null)
            {
                await vm.LoadConnectionsAsync(db);
            }

            // Setup caret suppression for empty folders
            var treeView = this.FindControl<TreeView>("ConnectionsTreeView");
            treeView.AddHandler(InputElement.PointerPressedEvent, TreeView_PointerPressed, RoutingStrategies.Tunnel);

            var grid = this.FindControl<Grid>("MainLayoutGrid");
            var leftColumn = grid.ColumnDefinitions[0];

            var ConnectionsTreeView = this.FindControl<TreeView>("ConnectionsTreeView");
            ConnectionsTreeView.PointerPressed += TreeView_PointerPressed;
            ConnectionsTreeView.PointerMoved += TreeView_PointerMoved;

            double lastKnownWidth = -1;

            grid.LayoutUpdated += (_, _) =>
            {
                var currentWidth = leftColumn.ActualWidth;

                if (currentWidth != lastKnownWidth)
                {
                    lastKnownWidth = currentWidth;

                    // Update width tracking
                    if (vm?.IsPaneOpen == true)
                    {
                        vm.LeftPaneWidth = new GridLength(currentWidth);
                        vm.UpdateLastExpandedWidth(currentWidth);
                    }

                    // Show/hide filter row
                    this.FindControl<TextBox>("FilterBox").IsVisible = leftColumn.ActualWidth > 120;
                    this.FindControl<Button>("ClearFilterButton").IsVisible = leftColumn.ActualWidth > 120;
                }
            };
        }
        // Method to re-encrypt stored connection passwords with a new password
        private async Task ReencryptStoredConnectionPasswordsAsync(string oldPassword, string newPassword)
        {
            var db = Program.Services.GetRequiredService<ConvergeDbContext>();

            var saltSetting = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionSalt");
            if (saltSetting == null)
                return;

            var oldKey = CryptoUtils.DeriveKey(oldPassword, saltSetting.Value);
            var newSalt = CryptoUtils.GenerateSalt();
            var newKey = CryptoUtils.DeriveKey(newPassword, newSalt);

            var connections = db.Connections.ToList();

            foreach (var conn in connections)
            {
                if (!string.IsNullOrEmpty(conn.Password))
                {
                    try
                    {
                        var decrypted = CryptoUtils.Decrypt(conn.Password, oldKey);
                        conn.Password = CryptoUtils.Encrypt(decrypted, newKey);
                    }
                    catch
                    {
                        continue; // Skip corrupted or mismatched entries
                    }
                }
            }

            var checkSetting = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionCheck");
            if (checkSetting != null) db.SiteSettings.Remove(checkSetting);
            db.SiteSettings.Remove(saltSetting);

            db.SiteSettings.Add(new SiteSetting { Key = "EncryptionSalt", Value = newSalt });
            db.SiteSettings.Add(new SiteSetting
            {
                Key = "EncryptionCheck",
                Value = CryptoUtils.Encrypt("CONVERGE-TEST", newKey)
            });

            await db.SaveChangesAsync();

            CryptoVault.Key = newKey;
        }
        // Event handler for the "New Connection" menu item
        private async void NewConnection_Click(object? sender, RoutedEventArgs e)
        {
            var editor = new ConnectionEditorWindow();
            var result = await editor.ShowDialog<Connection?>(this);

            if (result != null && DataContext is MainWindowViewModel vm)
            {
                result.LastUpdated = DateTime.UtcNow;

                if (vm.SelectedItem is { Connection: null, FolderId: not null } selectedFolder)
                {
                    result.FolderId = selectedFolder.FolderId;
                }

                var db = Program.Services.GetRequiredService<ConvergeDbContext>();
                db.Connections.Add(result);
                await db.SaveChangesAsync();
                
                // store treeview state
                _expandedFolderIds.Clear();
                _expandedConnectionIds.Clear();
                var treeViewControl = this.FindControl<TreeView>("ConnectionsTreeView");
                CaptureExpandedState(treeViewControl);

                await vm.LoadConnectionsAsync(db);
                // Restore treeview state
                RestoreExpandedState(treeViewControl);

            }
        }
        // Event handler for the "Edit Connection" menu item
        private async void EditConnection_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm &&
                vm.SelectedItem?.Connection is Connection original)
            {
                var editor = new ConnectionEditorWindow(original);
                var result = await editor.ShowDialog<Connection?>(this);

                if (result != null)
                {
                    result.Id = original.Id;
                    result.LastUpdated = DateTime.UtcNow;

                    var db = Program.Services.GetRequiredService<ConvergeDbContext>();
                    db.Entry(original).CurrentValues.SetValues(result);
                    await db.SaveChangesAsync();

                    await vm.LoadConnectionsAsync(db);
                }
            }
        }
        // TODO: Replace individual DeleteConnection_Click and DeleteFolder_Click with a unified DeleteSelectedItem_Click method
        // Issue URL: https://github.com/EvilGeniusGames/Converge/issues/4
        //       that checks the SelectedItem context and performs the appropriate deletion.
        //       Should we? Will we make the interface more confusing?

        // Event handler for the "Delete Connection" menu item
        private async void DeleteConnection_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm || vm.SelectedItem?.Connection is not Connection conn)
                return;

            var confirm = await MessageBoxConfirm($"Delete connection '{conn.Name}'?");
            if (!confirm) return;

            var db = Program.Services.GetRequiredService<ConvergeDbContext>();
            db.Connections.Remove(conn);
            await db.SaveChangesAsync();

            _expandedFolderIds.Clear();
            _expandedConnectionIds.Clear();
            var treeViewControl = this.FindControl<TreeView>("ConnectionsTreeView");
            CaptureExpandedState(treeViewControl);

            await vm.LoadConnectionsAsync(db);

            RestoreExpandedState(treeViewControl);

        }
        // Event handler for the "Close" menu item
        private void CloseApplicationMenuItem_Click(object? senader, RoutedEventArgs e)
        {
            this.Close();
        }
        // Event handler for the "Change Password" menu item
        private async void ChangePasswordMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            var changeWindow = new CreatePasswordWindow(requireOldPassword: true);
            var result = await changeWindow.ShowDialog<bool?>(this);

            if (result == true)
                await MessageBox("Password changed successfully.");
        }
        // Method to show a simple message box with a message
        private async Task MessageBox(string message)
        {
            var dialog = new Window
            {
                Width = 300,
                Height = 120,
                Title = "Notice",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            okButton.Click += (_, _) => dialog.Close();

            dialog.Content = new StackPanel
            {
                Margin = new Thickness(20),
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    okButton
                }
            };

            await dialog.ShowDialog(this);
        }
        // Method to verify or create the vault encryption settings
        private async Task<bool> VerifyOrCreateVaultAsync()
        {
            var db = Program.Services.GetRequiredService<ConvergeDbContext>();
            var saltSetting = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionSalt");

            if (saltSetting == null)
            {
                var create = new CreatePasswordWindow(requireOldPassword: false);
                var result = await create.ShowDialog<bool?>(this);

                if (result != true || string.IsNullOrWhiteSpace(create.EnteredPassword))
                {
                    Environment.Exit(0);
                }

                var salt = CryptoUtils.GenerateSalt();
                db.SiteSettings.Add(new SiteSetting { Key = "EncryptionSalt", Value = salt });

                var derivedKey = CryptoUtils.DeriveKey(create.EnteredPassword, salt);
                CryptoVault.Key = derivedKey;

                var testValue = CryptoUtils.Encrypt("CONVERGE-TEST", derivedKey);
                db.SiteSettings.Add(new SiteSetting { Key = "EncryptionCheck", Value = testValue });

                db.SaveChanges();
            }
            else
            {
                var check = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionCheck")?.Value;

                while (true)
                {
                    var enter = new EnterPasswordWindow(saltSetting.Value, check ?? "");
                    var result = await enter.ShowDialog<bool?>(this);

                    if (result == true && !string.IsNullOrWhiteSpace(enter.EnteredPassword))
                    {
                        var derivedKey = CryptoUtils.DeriveKey(enter.EnteredPassword, saltSetting.Value);
                        CryptoVault.Key = derivedKey;
                        break;
                    }

                    Environment.Exit(0);
                }
            }
            return true;
        }
        // Event handler for window closing to save the last pane width
        private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                var db = Program.Services.GetRequiredService<ConvergeDbContext>();
                var width = vm.LeftPaneWidth.Value.ToString();

                var existing = db.SiteSettings.FirstOrDefault(s => s.Key == "LastPaneWidth");
                if (existing != null)
                {
                    existing.Value = width;
                }
                else
                {
                    db.SiteSettings.Add(new SiteSetting { Key = "LastPaneWidth", Value = width });
                }

                db.SaveChanges();
            }
        }
        // Event handler for the "Create Folder" menu item
        private void CreateFolder_Click(object? sender, RoutedEventArgs e)
        {
            _ = CreateNewFolderAsync();
        }
        // Event handler for the "Delete Folder" menu item
        private void DeleteFolder_Click(object? sender, RoutedEventArgs e)
        {
            _ = DeleteSelectedFolderAsync();
        }
        // Event handler for the "Filter" button click
        private void ClearFilter_Click(object? sender, RoutedEventArgs e)
        {
            this.FindControl<TextBox>("FilterBox").Text = string.Empty;
        }
        // Event handlers for Cut, Copy, and Paste actions (currently not implemented)
        private void Cut_Click(object? sender, RoutedEventArgs e)
        {
            // TODO: Scafold Cut_Click code
            // Issue URL: https://github.com/EvilGeniusGames/Converge/issues/7
            // This method should handle cutting the selected item(s) from the tree view
        }
        private void Copy_Click(object? sender, RoutedEventArgs e)
        {
            // TODO: Scafold Copy_Click code
            // Issue URL: https://github.com/EvilGeniusGames/Converge/issues/6
            // This method should handle copying the selected item(s) from the tree view
        }
        private void Paste_Click(object? sender, RoutedEventArgs e)
        {
            // TODO: Scafold Paste_Click code
            // Issue URL: https://github.com/EvilGeniusGames/Converge/issues/5
            // This method should handle pasting the cut/copied item(s) into the tree view
        }
        // Event handler for the "New Folder" button click
        private async Task CreateNewFolderAsync()
        {
            var prompt = new PromptDialog("Create Folder", "Enter folder name:");
            var result = await prompt.ShowDialog<bool?>(this);

            if (result != true || string.IsNullOrWhiteSpace(prompt.Result))
                return;

            var folderName = prompt.Result.Trim();

            var db = Program.Services.GetRequiredService<ConvergeDbContext>();
            var vm = DataContext as MainWindowViewModel;

            try
            {
                int? parentId = null;
                if (vm?.SelectedItem is { Connection: null, FolderId: not null } selectedFolder)
                {
                    parentId = selectedFolder.FolderId;
                }

                var maxOrder = await db.Folders
                    .Where(f => f.ParentId == parentId)
                    .Select(f => (int?)f.Order)
                    .MaxAsync() ?? 0;

                var order = maxOrder + 1;

                db.Folders.Add(new Folder
                {
                    Name = folderName,
                    ParentId = parentId,
                    Order = order
                });

                await db.SaveChangesAsync();
                await vm!.LoadConnectionsAsync(db);
                // Restore expanded state after reload
                var treeViewControl = this.FindControl<TreeView>("ConnectionsTreeView");
                RestoreExpandedState(treeViewControl);
            }
            catch (Exception ex)
            {
                await MessageBox($"Error: {ex.Message}");
            }
        }
        // Method to Delete and confirm deletion of a folder
        private async Task DeleteSelectedFolderAsync()
        {
            var vm = DataContext as MainWindowViewModel;
            if (vm?.SelectedItem is not { Connection: null } selectedFolder)
                return;

            var confirm = await MessageBoxConfirm($"Delete folder '{selectedFolder.Name}' and all contents?");
            if (!confirm)
                return;

            var db = Program.Services.GetRequiredService<ConvergeDbContext>();
            if (selectedFolder.FolderId is null)
                return;

            var folder = await db.Folders
                .Include(f => f.Children)
                .Include(f => f.Connections)
                .FirstOrDefaultAsync(f => f.Id == selectedFolder.FolderId);


            if (folder == null)
                return;

            if (folder.Children.Any() || folder.Connections.Any())
            {
                db.Connections.RemoveRange(folder.Connections);
                db.Folders.RemoveRange(folder.Children);
            }

            db.Folders.Remove(folder);
            await db.SaveChangesAsync();

            await vm.LoadConnectionsAsync(db);
        }
        // Method to show a confirmation message box
        private async Task<bool> MessageBoxConfirm(string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            var dialog = new Window
            {
                Width = 300,
                Height = 140,
                Title = "Confirm",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            var yes = new Button { Content = "Yes", Width = 80 };
            var no = new Button { Content = "No", Width = 80 };

            yes.Click += (_, _) => { tcs.SetResult(true); dialog.Close(); };
            no.Click += (_, _) => { tcs.SetResult(false); dialog.Close(); };

            dialog.Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 10,
                Children =
        {
            new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Spacing = 8,
                Children = { yes, no }
            }
        }
            };

            await dialog.ShowDialog(this);
            return await tcs.Task;
        }
        private void TreeView_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _dragStartPoint = e.GetPosition(sender as Visual);
                _isDragInitiated = true;
            }
        }
        private void TreeView_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isDragInitiated) return;

            var position = e.GetPosition(sender as Visual);
            var diff = position - _dragStartPoint;

            if (Math.Abs(diff.X) > 5 || Math.Abs(diff.Y) > 5)
            {
                if (sender is TreeView treeView && e.Source is Control source &&
                    source.DataContext is ConnectionTreeItem item)
                {
                    Debug.WriteLine($"Initiating drag for item: {item.Name}");
                    _draggedItem = item;
                    var dragData = new DataObject();
                    dragData.Set("treeItem", item);
                    _expandedFolderIds.Clear();
                    _expandedConnectionIds.Clear();
                    var treeViewControl = this.FindControl<TreeView>("ConnectionsTreeView");
                    CaptureExpandedState(treeViewControl);
                    DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
                    Debug.WriteLine("DoDragDrop invoked.");
                    _isDragInitiated = false;
                }
            }
        }
        // Event handler for the TreeView's Drop event
        private async void TreeView_Drop(object? sender, DragEventArgs e)
        {
            Debug.WriteLine("DROP EVENT FIRED");

            if (!e.Data.Contains("treeItem"))
            {
                Debug.WriteLine("Drop failed: treeItem missing in data");
                return;
            }

            var droppedItem = e.Data.Get("treeItem") as ConnectionTreeItem;
            if (droppedItem == null)
            {
                Debug.WriteLine("Drop failed: droppedItem is null");
                return;
            }

            ConnectionTreeItem? targetItem = null;
            var point = e.GetPosition(this);
            var hit = this.InputHitTest(point);

            while (hit is Visual visual)
            {
                if (visual is Control control && control.DataContext is ConnectionTreeItem ctx)
                {
                    targetItem = ctx;
                    break;
                }
                hit = visual.GetVisualParent() as IInputElement;
            }

            Debug.WriteLine($"Drop: DraggedItem = {droppedItem.Name}, TargetItem = {targetItem?.Name ?? "[null]"}");

            // Prevent self-drop
            if (targetItem == null || ReferenceEquals(droppedItem, targetItem))
                return;

            // Only allow dragging connections
            if (droppedItem.Connection == null)
                return;

            var db = Program.Services.GetRequiredService<ConvergeDbContext>();
            var vm = DataContext as MainWindowViewModel;

            try
            {
                var conn = await db.Connections.FindAsync(droppedItem.Id);
                if (conn == null) return;

                int? newFolderId = null;
                int insertIndex = 0;

                // Dropping onto a folder: move to this folder, append to end
                if (targetItem.FolderId.HasValue)
                {
                    newFolderId = targetItem.FolderId;
                    var siblings = db.Connections
                        .Where(x => x.FolderId == newFolderId)
                        .OrderBy(x => x.Order)
                        .ToList();
                    insertIndex = siblings.Count;
                    // Remove from any previous folder list
                    siblings.RemoveAll(x => x.Id == conn.Id);
                    siblings.Insert(insertIndex, conn);

                    // Assign folder and update order
                    conn.FolderId = newFolderId;
                    for (int i = 0; i < siblings.Count; i++)
                        siblings[i].Order = i;
                }
                // Dropping onto a connection: move into same folder and above target
                else if (targetItem.Connection != null)
                {
                    newFolderId = targetItem.FolderId;
                    var siblings = db.Connections
                        .Where(x => x.FolderId == newFolderId)
                        .OrderBy(x => x.Order)
                        .ToList();

                    // Remove from any previous folder list
                    siblings.RemoveAll(x => x.Id == conn.Id);

                    // Insert just before target connection
                    int targetIndex = siblings.FindIndex(x => x.Id == targetItem.Connection.Id);
                    if (targetIndex < 0) targetIndex = 0;
                    siblings.Insert(targetIndex, conn);

                    // Assign folder and update order
                    conn.FolderId = newFolderId;
                    for (int i = 0; i < siblings.Count; i++)
                        siblings[i].Order = i;
                }
                else
                {
                    // Drop target not recognized, do nothing
                    return;
                }

                await db.SaveChangesAsync();
                await vm!.LoadConnectionsAsync(db);
                var treeViewControl = this.FindControl<TreeView>("ConnectionsTreeView");
                RestoreExpandedState(treeViewControl);
            }
            catch (Exception ex)
            {
                await MessageBox($"Drop error: {ex.Message}");
            }

        }
        // Event handler for the TreeView's DragOver event
        private void TreeView_DragOver(object? sender, DragEventArgs e)
        {
            Debug.WriteLine("DragOver event triggered.");

            if (e.Data.Contains("treeItem"))
            {
                e.DragEffects = DragDropEffects.Move;
                e.Handled = true;
                Debug.WriteLine("treeItem detected in drag data.");
            }
            else
            {
                Debug.WriteLine("No treeItem in drag data.");
            }
        }
        // Recursively capture expanded items
        private void CaptureExpandedState(ItemsControl? node)
        {
            if (node == null) return;

            for (int i = 0; i < node.ItemCount; i++)
            {
                var tvi = node.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                if (tvi == null) continue;

                if (tvi.IsExpanded && tvi.DataContext is ConnectionTreeItem ctx)
                {
                    if (ctx.FolderId.HasValue)
                        _expandedFolderIds.Add(ctx.FolderId.Value);
                    else if (ctx.Connection != null)
                        _expandedConnectionIds.Add(ctx.Connection.Id);
                }
                CaptureExpandedState(tvi);
            }


        }
        // Recursively restore expanded state
        private void RestoreExpandedState(ItemsControl? node)
        {
            // TODO: Refine this method to handle restoring expanded state with subfolders and connections
            // currently it only checks the immediate children of the node
            // needs to be able to traverse the entire tree structure
            if (node == null) return;

            // Helper function to check if this node or any descendant should be expanded
            bool ShouldExpand(ConnectionTreeItem item)
            {
                if ((item.FolderId.HasValue && _expandedFolderIds.Contains(item.FolderId.Value)) ||
                    (item.Connection != null && _expandedConnectionIds.Contains(item.Connection.Id)))
                    return true;

                if (item.Children != null)
                {
                    foreach (var child in item.Children)
                    {
                        if (ShouldExpand(child))
                            return true;
                    }
                }
                return false;
            }

            for (int i = 0; i < node.ItemCount; i++)
            {
                var tvi = node.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                if (tvi == null) continue;

                if (tvi.DataContext is ConnectionTreeItem currentItem)
                {
                    if (ShouldExpand(currentItem))
                    {
                        tvi.IsExpanded = true;
                    }
                    RestoreExpandedState(tvi);
                }
            }
        }

    }
}
