# Converge

**Converge** is a cross-platform graphical manager for Remote Desktop Protocol (RDP) and SSH connections, built using **.NET 8**, **Avalonia UI**, and **CommunityToolkit.MVVM**.

## Overview

Converge provides a unified interface for managing remote server access across Windows and Linux platforms. Designed for system administrators, developers, and power users, it enables quick access to both SSH and RDP sessions without juggling multiple tools.

- 🖥️ **RDP Support** – Wraps native clients like `mstsc` (Windows) or `xfreerdp` (Linux)
- 💻 **SSH Access** – Integrates with `Renci.SshNet` for direct terminal connections
- 🧭 **Cross-Platform GUI** – Built with Avalonia UI for Windows and Linux compatibility
- ⚙️ **.NET 8 + MVVM** – Architected with CommunityToolkit.MVVM for maintainable, reactive code

## Technologies

- [.NET 8](https://dotnet.microsoft.com/)
- [Avalonia UI](https://avaloniaui.net/) (MIT licensed)
- [CommunityToolkit.MVVM](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Renci.SshNet](https://github.com/sshnet/SSH.NET)
- Platform-native RDP clients (`mstsc`, `xfreerdp`)

## Requirements

- Visual Studio 2022
  - Avalonia Extension installed
- .NET 8 SDK
- Optional: FreeRDP (`xfreerdp`) on Linux for RDP support

## Getting Started

1. Clone the repository
2. Open in Visual Studio 2022
3. Select `Converge` as the startup project
4. Build and run

## License

MIT License – See [LICENSE](LICENSE) for details.

## Planned Features

- ✅ SSH session manager (via Renci.SshNet)
- ✅ RDP launcher (native clients: mstsc, xfreerdp)
- ⏳ VNC support (future)
- ⏳ Session grouping and tagging
- ⏳ Credentials manager with secure storage
- ⏳ Quick Connect launcher
- ⏳ Export/import connection lists
