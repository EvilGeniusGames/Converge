# Converge Application Manual

## Overview

**Converge** is a cross-platform desktop application for managing and launching RDP, SSH, and VNC connections. Built with .NET 8 and Avalonia UI, it provides a tabbed interface, secure credential storage, and a unified management experience for remote sessions.

---

## Getting Started

### 🔑 Unlocking the App
- On launch, Converge prompts for your **master password**.
- This password decrypts any saved credentials.
- If no password has been set yet, you will be prompted to create one.

---

## 🔌 Managing Connections

### Add a New Connection
1. Click **File → New Connection**.
2. Fill in:
   - **Name**
   - **Host**
   - **Port**
   - **Protocol** (SSH, RDP, or VNC)
   - **Username**
   - **Authentication Type**:
     - Password
     - SSH Key (for SSH connections)

3. Click **Save** to store the connection.

### Edit Existing Connection
- Select a connection and click **File → Edit Connection**.
- Make desired changes and click **Save**.

### Delete Connection
- Right-click a connection in the list or use the toolbar.
- Confirm deletion.

---

## 📂 Folder Management

- Connections are organized into **folders**.
- Right-click the folder tree to:
  - Create new folders
  - Rename or delete folders
  - Drag-and-drop to organize hierarchy

---

## 🔐 Password Management

- To change your master password, go to **File → Change Master Password**.
- Old password is required to authorize the change.
- Passwords are encrypted using AES and a user-provided key.

---

## 🧳 Launching Connections

- Double-click any connection or select it and click **Connect**.
- The connection will open in a **new tab** in the main window.

---

## 🗔 Window Management

- Tabs can be **detached into windows** for multi-monitor use.
- Each detached window maintains its own full-screen toggle.
- Tabs and windows are managed by the `ConnectionWindowManager`.

---

## ✂️ Clipboard Integration

- Cut, Copy, Paste commands are routed through the active terminal or control.
- Accessible via **Edit Menu** or standard shortcuts (`Ctrl+X`, `Ctrl+C`, `Ctrl+V`).

---

## 🖋 Appearance Settings

- Global font size, scaling, and colors are centrally managed.
- Each connection can override appearance settings via per-connection config (planned).
- All updates are reflected in real time using data binding.

---

## 🔄 Saving & Persistence

- Connections and folders are stored in a local **SQLite** database.
- All sensitive data is encrypted using the master password.
- Database and media can be backed up manually (future automation TBD).

---

## Notes

- RDP uses native clients (`mstsc` on Windows, `xfreerdp` on Linux).
- SSH is powered by `Renci.SshNet`.
- Future support for VNC is planned.

---

## Version

**Converge**  
Version: 0.1 Alpha  
Status: Interface Complete, Core Features Operational

