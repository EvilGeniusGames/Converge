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

