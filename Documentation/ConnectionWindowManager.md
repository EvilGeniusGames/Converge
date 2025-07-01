# Connection Window Manager

## Purpose

The `ConnectionWindowManager` is a central service for managing all visual and behavioral aspects of active connection views (SSH, RDP, VNC) in the Converge application. It supports both **tabbed interfaces** and **detached windows**, providing a unified experience across different protocols and views.

---

## Features

### Window & Tab Management
- Tracks all active connection views.
- Supports **tab detachment** into standalone windows and **re-attachment**.
- Enables **full-screen mode** for immersive use.
- Maintains a consistent registry of connection states (tabbed or windowed).

### Appearance & Scaling
- Centralized handling of:
  - **Font family and size**
  - **Font scaling and DPI adjustments**
  - **Cursor shape**
  - **Custom color schemes**
- Supports **per-connection appearance overrides** for user customization.
- Live update capability across all views using `INotifyPropertyChanged`.

### Clipboard Integration
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

This list outlines the essential components that make up the `ConnectionWindowManager`. This provides clarity on the responsibilities of the manager and supports future development and maintenance.

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

## Connection Registry

The **Connection Registry** is the authoritative, in-memory collection that tracks all currently active connections within the application, regardless of how they are presented (tab, window, or fullscreen). Managed by the `ConnectionWindowManager`, it exposes an observable collection that enables other components—such as the Tab Manager and Window Host—to react to changes in connection state in real time. Each entry in the registry contains information about the connection’s identity, protocol, and current view state. This centralized design simplifies coordination between different parts of the application and ensures a single source of truth for all connection-related operations.

### How the Connection Registry Works

1. **Initialization**  
   The Connection Registry is managed by the `ConnectionWindowManager`, which maintains an in-memory, observable collection of `ActiveConnection` objects representing all open connections.

2. **Observability**  
   The registry exposes its collection as an `ObservableCollection<ActiveConnection>`. This allows other components (such as the Tab Manager and Window Host) to subscribe to `CollectionChanged` events and respond automatically when connections are added, removed, or modified.

3. **Adding a Connection**  
   When a user initiates a new connection (e.g., by double-clicking a saved connection), the application creates a new `ActiveConnection` instance and adds it to the registry using `AddConnection()`. The registry ensures no duplicate connections by `Id`.

4. **Removing a Connection**  
   When a connection is closed or detached from the UI, the corresponding `ActiveConnection` is removed from the registry with `RemoveConnection()`. All subscribers are notified of the removal.

5. **Application Interaction**  
   The application does not interact with the collection directly. Instead, it always uses the `ConnectionWindowManager` to add or remove connections. Components such as the Tab Manager observe the registry and update the user interface in response to its changes.

6. **Decoupled Architecture**  
   This design enables a decoupled and reactive architecture: the registry manages connection state, while UI and management components react to changes, ensuring a responsive and maintainable application.


  ## Tab Manager
  
The **Tab Manager** is a dedicated component responsible for managing all connection views presented as tabs within the main application window. It observes the central connection registry for changes and reacts by creating, updating, or removing tabs as connections are added, modified, or detached. By maintaining its own observable collection of tab view models, the Tab Manager ensures that the user interface remains synchronized with the current set of active tabbed connections. This design promotes a clean separation of concerns, allowing the Tab Manager to focus exclusively on tab lifecycle and presentation, while the connection registry maintains the authoritative state of all open connections.

  ### MainWindow and Tab Manager Interaction Flow

1. **Tab Manager Integration**  
   The Tab Manager is initialized and associated with the MainWindow (either directly or through its ViewModel). The MainWindow’s tab interface (such as a `TabControl`) is bound to or managed by the Tab Manager.

2. **Tab Manager Observes the Registry**  
   The Tab Manager subscribes to the `ActiveConnections.CollectionChanged` event in the `ConnectionWindowManager`. When a new `ActiveConnection` with `ViewState == Tab` is added, the Tab Manager reacts by creating and adding a new tab view model to its observable collection.

3. **Tab Creation**  
   When a new tab view model is added to the Tab Manager’s collection, the MainWindow’s tab interface (bound to this collection) automatically displays a new tab for the connection.

4. **Tab Removal/Detachment**  
   If a tab is closed or detached by the user, the MainWindow (through the UI or command binding) updates the corresponding `ActiveConnection.ViewState` (e.g., to `Window` for detachment). This triggers the Tab Manager to remove the tab and the Window Host to take over management.

5. **MVVM Best Practice**  
   The MainWindow’s tab control is bound to an observable collection of tab view models managed by the Tab Manager. Any changes in the collection (add, remove, update) are automatically reflected in the UI, keeping the interface in sync with the application state.
