using System.Collections.Generic;

namespace Converge.Data;

public class Folder
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? ParentId { get; set; }
    public Folder? Parent { get; set; }

    public ICollection<Folder> Children { get; set; } = new List<Folder>();

    public ICollection<Connection> Connections { get; set; } = new List<Connection>();

    public int Order { get; set; }
}
