using System.Collections.ObjectModel;
using Converge.Data;
namespace Converge.Models;
using Converge.Data;

public class ConnectionTreeItem : IOrderItem

{
    public string Name { get; set; } = string.Empty;
    public int? FolderId { get; set; }
    public int? ConnectionId => Connection?.Id;
    // Optional: holds the actual connection object (null if this is a folder)
    public Connection? Connection { get; set; }
    // Child folders or connections
    public ObservableCollection<ConnectionTreeItem> Children { get; set; } = new();
    public bool HasChildren => Children.Count > 0 || (Connection == null);

    public int Id => Connection?.Id ?? Folder?.Id ?? 0;
    public int Order => (Connection as IOrderItem)?.Order
                  ?? (Folder as IOrderItem)?.Order
                  ?? 0;

    public Folder? Folder { get; set; }

}
