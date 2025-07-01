using System;
using System.Collections.ObjectModel;
using System.Linq;
using Converge.Models;

namespace Converge.Services
{
    public class ConnectionWindowManager : IConnectionWindowManager
    {
        public ObservableCollection<ActiveConnection> ActiveConnections { get; } = new();

        public void AddConnection(ActiveConnection connection)
        {
            if (connection != null && !ActiveConnections.Any(c => c.Id == connection.Id))
            {
                ActiveConnections.Add(connection);
            }
        }

        public void RemoveConnection(Guid id)
        {
            var conn = ActiveConnections.FirstOrDefault(c => c.Id == id);
            if (conn != null)
            {
                ActiveConnections.Remove(conn);
            }
        }
    }
}
