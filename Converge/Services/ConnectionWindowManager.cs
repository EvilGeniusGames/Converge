using System;
using System.Collections.Generic;
using Converge.Models;

namespace Converge.Services
{
    public class ConnectionWindowManager : IConnectionWindowManager
    {
        // Public read-only access to the connection registry
        public IReadOnlyDictionary<Guid, ActiveConnection> ActiveConnections => _activeConnections;

        // Internal storage for active connections
        private readonly Dictionary<Guid, ActiveConnection> _activeConnections;

        public ConnectionWindowManager()
        {
            _activeConnections = new Dictionary<Guid, ActiveConnection>();
        }

        // Example method to add a connection
        public void AddConnection(ActiveConnection connection)
        {
            if (connection != null && !_activeConnections.ContainsKey(connection.Id))
            {
                _activeConnections[connection.Id] = connection;
            }
        }

        // Example method to remove a connection
        public void RemoveConnection(Guid id)
        {
            _activeConnections.Remove(id);
        }
    }
}
