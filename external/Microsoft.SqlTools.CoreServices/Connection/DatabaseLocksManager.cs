//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.CoreServices.LanguageServices;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.SqlTools.CoreServices.Connection
{
    public class DatabaseLocksManager: IDisposable
    {
        internal DatabaseLocksManager(int waitToGetFullAccess)
        {
            this.waitToGetFullAccess = waitToGetFullAccess;
        }

        private static DatabaseLocksManager instance = new DatabaseLocksManager(DefaultWaitToGetFullAccess);

        public static DatabaseLocksManager Instance
        {
            get
            {
                return instance;
            }
        }

        public ConnectionServiceCore ConnectionService { get; set; }

        private Dictionary<string, ManualResetEvent> databaseAccessEvents = new Dictionary<string, ManualResetEvent>();
        private object databaseAccessLock = new object();
        public const int DefaultWaitToGetFullAccess = 10000;
        public int waitToGetFullAccess = DefaultWaitToGetFullAccess;

        private ManualResetEvent GetResetEvent(string serverName, string databaseName)
        {
            string key = GenerateKey(serverName, databaseName);
            ManualResetEvent resetEvent = null;
            lock (databaseAccessLock)
            {
                if (!databaseAccessEvents.TryGetValue(key, out resetEvent))
                {
                    resetEvent = new ManualResetEvent(true);
                    databaseAccessEvents.Add(key, resetEvent);
                }
            }

            return resetEvent;
        }

        public bool GainFullAccessToDatabase(string serverName, string databaseName)
        {
            /*
             * TODO: add the lock so not two process can get full access at the same time
            ManualResetEvent resetEvent = GetResetEvent(serverName, databaseName);
            if (resetEvent.WaitOne(this.waitToGetFullAccess))
            {
                resetEvent.Reset();

                foreach (IConnectedBindingQueue item in ConnectionService.ConnectedQueues)
                {
                    item.CloseConnections(serverName, databaseName);
                }
                return true;
            }
            else
            {
                throw new DatabaseFullAccessException($"Waited more than {waitToGetFullAccess} milli seconds for others to release the lock");
            }
            */
            foreach (IConnectedBindingQueue item in ConnectionService.ConnectedQueues)
            {
                item.CloseConnections(serverName, databaseName, DefaultWaitToGetFullAccess);
            }
            return true;

        }

        public bool ReleaseAccess(string serverName, string databaseName)
        {
            /*
            ManualResetEvent resetEvent = GetResetEvent(serverName, databaseName);

            foreach (IConnectedBindingQueue item in ConnectionService.ConnectedQueues)
            {
                item.OpenConnections(serverName, databaseName);
            }
            
            resetEvent.Set();
            */
            foreach (IConnectedBindingQueue item in ConnectionService.ConnectedQueues)
            {
                item.OpenConnections(serverName, databaseName, DefaultWaitToGetFullAccess);
            }
            return true;
            
        }

        private string GenerateKey(string serverName, string databaseName)
        {
            return $"{serverName.ToLowerInvariant()}-{databaseName.ToLowerInvariant()}";
        }

        public void Dispose()
        {
            foreach (var resetEvent in databaseAccessEvents)
            {
                resetEvent.Value.Dispose();
            }
        }
    }
}
