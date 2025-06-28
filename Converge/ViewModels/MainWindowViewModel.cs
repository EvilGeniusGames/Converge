using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Converge.Data;
using Converge.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Converge.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isPaneOpen = true;
    [ObservableProperty]
    private ConnectionTreeItem? selectedItem;

    private double _lastExpandedWidth = 250; // Default starting width

    [ObservableProperty]
    private GridLength leftPaneWidth = new GridLength(250);

    [RelayCommand]
    private void TogglePane()
    {
        if (IsPaneOpen)
        {
            _lastExpandedWidth = LeftPaneWidth.Value;
            LeftPaneWidth = new GridLength(42); // Collapsed bar width
            IsPaneOpen = false;
        }
        else
        {
            LeftPaneWidth = new GridLength(_lastExpandedWidth);
            IsPaneOpen = true;
        }
    }
    partial void OnLeftPaneWidthChanged(GridLength value)
    {
        if (IsPaneOpen && value.Value > 42)
        {
            _lastExpandedWidth = value.Value;
        }
    }

    public ObservableCollection<ConnectionTreeItem> ConnectionTreeItems { get; } = new();

    public async Task LoadConnectionsAsync(ConvergeDbContext db)
    {
        ConnectionTreeItems.Clear();

        var folders = await db.Folders
            .Include(f => f.Connections)
            .OrderBy(f => f.Order)
            .ToListAsync();

        var rootFolders = folders.Where(f => f.ParentId == null).ToList();

        foreach (var folder in rootFolders)
        {
            ConnectionTreeItems.Add(BuildFolderTree(folder, folders));
        }

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

        if (ConnectionTreeItems.Count == 0)
        {
            ConnectionTreeItems.Add(new ConnectionTreeItem { Name = "No connections available" });
        }
    }

    private ConnectionTreeItem BuildFolderTree(Folder folder, List<Folder> allFolders)
    {
        var node = new ConnectionTreeItem
        {
            Name = folder.Name,
            FolderId = folder.Id,
            Children = new ObservableCollection<ConnectionTreeItem>() // Ensure initialized
        };


        var childFolders = allFolders.Where(f => f.ParentId == folder.Id).OrderBy(f => f.Order);
        foreach (var child in childFolders)
        {
            node.Children.Add(BuildFolderTree(child, allFolders));
        }

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
    public void UpdateLastExpandedWidth(double newWidth)
    {
        if (newWidth > 42)
            _lastExpandedWidth = newWidth;
    }
}
