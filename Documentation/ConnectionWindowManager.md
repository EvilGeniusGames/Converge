# Connection Window Manager

## Purpose

The `ConnectionWindowManager` is a central service for managing all visual and behavioral aspects of active connection views (SSH, RDP, VNC) in the Converge application. It supports both **tabbed interfaces** and **detached windows**, providing a unified experience across different protocols and views.

---

## Features

### 🔲 Window & Tab Management
- Tracks all active connection views.
- Supports **tab detachment** into standalone windows and **re-attachment**.
- Enables **full-screen mode** for immersive use.
- Maintains a consistent registry of connection states (tabbed or windowed).

### 🖋 Appearance & Scaling
- Centralized handling of:
  - **Font family and size**
  - **Font scaling and DPI adjustments**
  - **Cursor shape**
  - **Custom color schemes**
- Supports **per-connection appearance overrides** for user customization.
- Live update capability across all views using `INotifyPropertyChanged`.

### ✂️ Clipboard Integration
- Exposes shared clipboard commands:
  - `CutCommand`
  - `CopyCommand`
  - `PasteCommand`
- Tracks focused connection or control to **route clipboard actions** accurately.
- Provides fallback behavior when target does not implement specific commands.

---

## Architecture Considerations

- Implemented as a singleton or registered via dependency injection as `IConnectionWindowManager`.
- Designed to **decouple UI components** from connection state and behavior logic.
- Enables future features like:
  - Persisting window layouts
  - Global hotkey handling
  - Advanced theming

---

## Why This Approach?

By isolating window and view management in a dedicated service, we gain:

- **Clean separation of concerns** between view rendering and connection logic.
- **Scalability** as we expand protocols (RDP, VNC) and features (multi-monitor, split views).
- A consistent user experience across different contexts (tabs vs windows).
- Easier testing, customization, and maintenance of window-related behaviors.

### Core Components of the Connection Window Manager

This list outlines the essential components that make up the `ConnectionWindowManager`. Documenting these elements provides clarity on the responsibilities of the manager and supports future development and maintenance.

#### Components

- **Connection Registry**  
  Tracks all open connections and their current state (tabbed, windowed, fullscreen).

- **Tab Manager**  
  Manages connection views within tabbed interfaces, including reordering, detachment, and re-attachment.

- **Window Host**  
  Handles the creation and management of detached windows, as well as transitions to and from full-screen mode.

- **Appearance Controller**  
  Centralizes font settings, scaling, color schemes, and ensures consistent updates to all connection views.

- **Clipboard Handler**  
  Routes cut, copy, and paste commands to the appropriate focused connection or control.

- **Focus Tracker**  
  Maintains awareness of which connection or UI element currently has input or clipboard focus.

- **Settings and Preferences**  
  Stores and applies global or per-connection appearance and behavior overrides.

- **Event Dispatcher**  
  Notifies views and components of state or appearance changes, typically using property change notification.

- **Persistence Engine** (planned)  
  Saves and restores window/tab layouts and user preferences for a seamless user experience.
