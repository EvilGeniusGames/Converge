using System;
using System.Collections.Generic;
using Converge.Models;

namespace Converge.Services
{
    public interface IConnectionWindowManager
    {
        IReadOnlyDictionary<Guid, ActiveConnection> ActiveConnections { get; }
        void AddConnection(ActiveConnection connection);
        void RemoveConnection(Guid id);
        // Add other necessary methods as your design matures
    }
}
