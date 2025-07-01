// /Converge/Services/TabManager.cs

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Converge.Models;

namespace Converge.Services
{
    public class TabManager
    {
        private readonly ObservableCollection<ActiveConnection> _tabs = new();
        public ReadOnlyObservableCollection<ActiveConnection> Tabs { get; }

        private readonly IConnectionWindowManager _connectionWindowManager;

        public TabManager(IConnectionWindowManager connectionWindowManager)
        {
            _connectionWindowManager = connectionWindowManager;
            Tabs = new ReadOnlyObservableCollection<ActiveConnection>(_tabs);

            // Subscribe to the connection registry
            _connectionWindowManager.ActiveConnections.CollectionChanged += ActiveConnections_CollectionChanged;

            // Initialize tabs with any pre-existing connections (rare, but supports restoration)
            foreach (var conn in _connectionWindowManager.ActiveConnections.Where(c => c.ViewState == ConnectionViewState.Tab))
            {
                _tabs.Add(conn);
            }
        }

        private void ActiveConnections_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Add new tabs
            if (e.NewItems != null)
            {
                foreach (ActiveConnection conn in e.NewItems)
                {
                    if (conn.ViewState == ConnectionViewState.Tab && !_tabs.Contains(conn))
                        _tabs.Add(conn);
                }
            }

            // Remove closed or detached tabs
            if (e.OldItems != null)
            {
                foreach (ActiveConnection conn in e.OldItems)
                {
                    if (_tabs.Contains(conn))
                        _tabs.Remove(conn);
                }
            }

            // Optional: handle state transitions (e.g., Tab to Window)
            if (e.Action == NotifyCollectionChangedAction.Replace && e.NewItems != null)
            {
                foreach (ActiveConnection conn in e.NewItems)
                {
                    if (conn.ViewState != ConnectionViewState.Tab && _tabs.Contains(conn))
                        _tabs.Remove(conn);
                }
            }
        }

        public void CloseTab(ActiveConnection connection)
        {
            // Remove the connection from the registry, which will update tabs automatically
            _connectionWindowManager.RemoveConnection(connection.Id);
        }

        public void PopoutTab(ActiveConnection connection)
        {
            // Change view state and let the managers update their collections/views
            connection.ViewState = ConnectionViewState.Window;
            // Optionally: invoke logic to open the new hosted window immediately
        }

    }
}
