using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Converge.Data;
using Converge.Models;
using Converge.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Converge.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        // Indicates if the left pane is open
        [ObservableProperty]
        private bool isPaneOpen = true;

        // Currently selected item in the tree
        [ObservableProperty]
        private ConnectionTreeItem? selectedItem;

        // Stores the last expanded width of the left pane for restoration
        private double _lastExpandedWidth = 250;

        // The width of the left pane (binds to the UI)
        [ObservableProperty]
        private GridLength leftPaneWidth = new GridLength(250);

        // TabManager instance, manages tabbed connection views
        public TabManager TabManager { get; }

        /// <summary>
        /// Constructor: injects the ConnectionWindowManager dependency and initializes the TabManager.
        /// </summary>
        public MainWindowViewModel(IConnectionWindowManager connectionWindowManager)
        {
            // Initialize TabManager with the shared connection registry
            TabManager = new TabManager(connectionWindowManager);

            // Add any additional initialization logic here as needed
        }

        /// <summary>
        /// Toggles the left pane open/closed.
        /// </summary>
        [RelayCommand]
        private void TogglePane()
        {
            if (IsPaneOpen)
            {
                _lastExpandedWidth = LeftPaneWidth.Value;
                LeftPaneWidth = new GridLength(42); // Collapsed width
                IsPaneOpen = false;
            }
            else
            {
                LeftPaneWidth = new GridLength(_lastExpandedWidth);
                IsPaneOpen = true;
            }
        }

        /// <summary>
        /// Updates the last expanded width when the pane is resized.
        /// </summary>
        partial void OnLeftPaneWidthChanged(GridLength value)
        {
            if (IsPaneOpen && value.Value > 42)
            {
                _lastExpandedWidth = value.Value;
            }
        }

        // Collection representing the items in the connection/folder tree
        public ObservableCollection<ConnectionTreeItem> ConnectionTreeItems { get; } = new();

        /// <summary>
        /// Loads folders and connections from the database, rebuilding the tree structure.
        /// </summary>
        public async Task LoadConnectionsAsync(ConvergeDbContext db)
        {
            ConnectionTreeItems.Clear();

            // Load folders and their connections
            var folders = await db.Folders
                .Include(f => f.Connections)
                .OrderBy(f => f.Order)
                .ToListAsync();

            var rootFolders = folders.Where(f => f.ParentId == null).ToList();

            // Add all root folders and build their subtrees recursively
            foreach (var folder in rootFolders)
            {
                ConnectionTreeItems.Add(BuildFolderTree(folder, folders));
            }

            // Add root-level connections (not in any folder)
            var rootConnections = await db.Connections
                .Where(c => c.FolderId == null)
                .OrderBy(c => c.Order)
                .ToListAsync();

            foreach (var conn in rootConnections)
            {
                ConnectionTreeItems.Add(new ConnectionTreeItem
                {
                    Name = conn.Name,
                    Connection = conn
                });
            }

            // If there are no connections or folders, add a placeholder item
            if (ConnectionTreeItems.Count == 0)
            {
                ConnectionTreeItems.Add(new ConnectionTreeItem { Name = "No connections available" });
            }
        }

        /// <summary>
        /// Recursively builds a folder tree with child folders and connections.
        /// </summary>
        private ConnectionTreeItem BuildFolderTree(Folder folder, List<Folder> allFolders)
        {
            var node = new ConnectionTreeItem
            {
                Name = folder.Name,
                FolderId = folder.Id,
                Children = new ObservableCollection<ConnectionTreeItem>() // Ensure initialized
            };

            // Add child folders recursively
            var childFolders = allFolders.Where(f => f.ParentId == folder.Id).OrderBy(f => f.Order);
            foreach (var child in childFolders)
            {
                node.Children.Add(BuildFolderTree(child, allFolders));
            }

            // Add connections within this folder
            foreach (var conn in folder.Connections.OrderBy(c => c.Order))
            {
                node.Children.Add(new ConnectionTreeItem
                {
                    Name = conn.Name,
                    Connection = conn
                });
            }

            return node;
        }

        /// <summary>
        /// Updates the last expanded width for restoring the left pane size.
        /// </summary>
        public void UpdateLastExpandedWidth(double newWidth)
        {
            if (newWidth > 42)
                _lastExpandedWidth = newWidth;
        }

        [RelayCommand]
        public void CloseTab(ActiveConnection connection)
        {
            if (connection != null)
                TabManager.CloseTab(connection);
        }

        [RelayCommand]
        public void PopoutTab(ActiveConnection connection)
        {
            if (connection != null)
                TabManager.PopoutTab(connection);
        }

    }
}
