using Converge.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Converge.Data
{
    public class Connection
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }

        public string Protocol { get; set; } = string.Empty; // e.g., SSH, RDP, VNC
        public string Username { get; set; } = string.Empty;

        public string AuthType { get; set; } = string.Empty; // e.g., Password, Key File
        public string? Password { get; set; }
        public string? KeyFilePath { get; set; }

        public string? Notes { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public int? FolderId { get; set; }
        public Folder? Folder { get; set; }
        public int Order { get; set; }
    }
}
