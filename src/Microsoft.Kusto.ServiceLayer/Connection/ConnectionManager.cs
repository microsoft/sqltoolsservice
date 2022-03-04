//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Concurrent;
using System.Composition;

namespace Microsoft.Kusto.ServiceLayer.Connection
{
    [Export(typeof(IConnectionManager))]
    public class ConnectionManager : IConnectionManager
    {
        /// <summary>
        /// Map from script URIs to ConnectionInfo objects
        /// </summary>
        private ConcurrentDictionary<string, ConnectionInfo> _ownerToConnectionMap;

        public ConnectionManager()
        {
            _ownerToConnectionMap = new ConcurrentDictionary<string, ConnectionInfo>();
        }

        public bool TryGetValue(string ownerUri, out ConnectionInfo info)
        {
            return _ownerToConnectionMap.TryGetValue(ownerUri, out info);
        }

        public bool ContainsKey(string ownerUri)
        {
            return _ownerToConnectionMap.ContainsKey(ownerUri);
        }

        public bool TryAdd(string ownerUri, ConnectionInfo connectionInfo)
        {
            return _ownerToConnectionMap.TryAdd(ownerUri, connectionInfo);
        }

        public bool TryRemove(string ownerUri)
        {
            return _ownerToConnectionMap.TryRemove(ownerUri, out _);
        }
    }
}