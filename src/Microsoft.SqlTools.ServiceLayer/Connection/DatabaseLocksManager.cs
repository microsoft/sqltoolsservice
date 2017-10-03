//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    public class DatabaseLocksManager
    {
        internal DatabaseLocksManager()
        {
        }

        private static DatabaseLocksManager instance = new DatabaseLocksManager();

        public static DatabaseLocksManager Instance
        {
            get
            {
                return instance;
            }
        }
        private Dictionary<string, List<IDatabaseLockConnection>> _connections = new Dictionary<string, List<IDatabaseLockConnection>>();
        private object lockObject = new object();

        public void AddConnection(string serverName, string databaseName, IDatabaseLockConnection connection)
        {
            string key = GenerateKey(serverName, databaseName);

            lock (this.lockObject)
            {
                List<IDatabaseLockConnection> currentList;
                if (_connections.TryGetValue(key, out currentList))
                {
                    currentList.Add(connection);
                }
                else
                {
                    currentList = new List<IDatabaseLockConnection>();
                    currentList.Add(connection);
                    _connections.Add(key, currentList);
                }
            }
        }

        public void RemoveConnection(string serverName, string databaseName, IDatabaseLockConnection connection)
        {
            string key = GenerateKey(serverName, databaseName);
            lock(this.lockObject)
            {
                List<IDatabaseLockConnection> currentList;
                if (_connections.TryGetValue(key, out currentList))
                {
                    if (connection.IsConnctionOpen)
                    {
                        connection.Disconnect();
                    }
                    currentList.Remove(connection);
                }
            }
        }

        internal ReadOnlyCollection<IDatabaseLockConnection> GetLocks(string serverName, string databaseName)
        {
            string key = GenerateKey(serverName, databaseName);
            ReadOnlyCollection<IDatabaseLockConnection> readOnLyList = null;
            lock (lockObject)
            {
                List<IDatabaseLockConnection> currentList;
                if (_connections.TryGetValue(key, out currentList))
                {
                    readOnLyList = new ReadOnlyCollection<IDatabaseLockConnection>(currentList);
                }
            }

            return readOnLyList;
        }

        public void ReleaseLocks(string serverName, string databaseName)
        {
            ReadOnlyCollection<IDatabaseLockConnection> readOnLyList = GetLocks(serverName, databaseName);

            if (readOnLyList != null)
            {
                foreach (var connection in readOnLyList)
                {
                    if (connection.IsConnctionOpen && connection.CanTemporaryClose)
                    {
                        connection.Disconnect();
                    }
                }
            }
        }

        public void RegainLocks(string serverName, string databaseName)
        {
            ReadOnlyCollection<IDatabaseLockConnection> readOnLyList = GetLocks(serverName, databaseName);

            if (readOnLyList != null)
            {
                foreach (var connection in readOnLyList)
                {
                    if (!connection.IsConnctionOpen)
                    {
                        connection.Connect();
                    }
                }
            }
        }

        private string GenerateKey(string serverName, string databaseName)
        {
            return $"{serverName.ToLowerInvariant()}-{databaseName.ToLowerInvariant()}";
        }
    }
}
