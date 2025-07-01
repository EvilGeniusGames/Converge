using System;
using System.Collections.ObjectModel;
using Converge.Models;

namespace Converge.Services
{
    public interface IConnectionWindowManager
    {
        ObservableCollection<ActiveConnection> ActiveConnections { get; }
        void AddConnection(ActiveConnection connection);
        void RemoveConnection(Guid id);
    }
}
