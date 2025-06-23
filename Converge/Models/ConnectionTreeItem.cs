using System.Collections.ObjectModel;
using Converge.Data;
namespace Converge.Models;

public class ConnectionTreeItem
{
    public string Name { get; set; } = string.Empty;

    // Optional: holds the actual connection object (null if this is a folder)
    public Connection? Connection { get; set; }

    // Child folders or connections
    public ObservableCollection<ConnectionTreeItem> Children { get; set; } = new();
}
