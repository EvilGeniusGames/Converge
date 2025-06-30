using System;

namespace Converge.Models
{
    public enum ConnectionViewState
    {
        Tab,
        Window,
        Fullscreen
    }

    public class ActiveConnection
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; }
        public string Protocol { get; set; } // "SSH", "RDP", "VNC", etc.
        public int? ConnectionId { get; set; } // Link to stored connection definition if needed

        public ConnectionViewState ViewState { get; set; } = ConnectionViewState.Tab;

        public object ViewReference { get; set; } // Reference to the actual UI view/control instance
        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

        // Appearance overrides (optional/expandable)
        public string FontFamily { get; set; }
        public double? FontSize { get; set; }
        public double? DpiScale { get; set; }
        public string ColorScheme { get; set; }

        // Add additional fields as requirements evolve
    }
}
