using System.Collections.ObjectModel;
using Converge.Data;
namespace Converge.Models;

public class ConnectionTreeItem
{
    public string Name { get; set; } = string.Empty;
    public int? FolderId { get; set; }
    public int? ConnectionId => Connection?.Id;
    // Optional: holds the actual connection object (null if this is a folder)
    public Connection? Connection { get; set; }
    // Child folders or connections
    public ObservableCollection<ConnectionTreeItem> Children { get; set; } = new();
    public bool HasChildren => Children.Count > 0 || (Connection == null);

}
