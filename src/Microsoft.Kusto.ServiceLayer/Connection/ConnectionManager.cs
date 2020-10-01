using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.Kusto.ServiceLayer.Connection
{
    [Export(typeof(IConnectionManager))]
    public class ConnectionManager : IConnectionManager
    {
        /// <summary>
        /// Map from script URIs to ConnectionInfo objects
        /// </summary>
        private readonly ConcurrentDictionary<string, ConnectionInfo> _ownerToConnectionMap;

        public ConnectionManager()
        {
            _ownerToConnectionMap = new ConcurrentDictionary<string, ConnectionInfo>();
        }
        
        // Attempts to link a URI to an actively used connection for this URI
        public bool TryFindConnection(string ownerUri, out ConnectionInfo connectionInfo)
        {
            return _ownerToConnectionMap.TryGetValue(ownerUri, out connectionInfo);
        }

        public bool Exists(string ownerUri)
        {
            return _ownerToConnectionMap.ContainsKey(ownerUri);
        }

        public void Set(string ownerUri, ConnectionInfo connectionInfo)
        {
            _ownerToConnectionMap[ownerUri] = connectionInfo;
        }

        public void Remove(string ownerUri)
        {
            _ownerToConnectionMap.Remove(ownerUri, out _);
        }
    }
}