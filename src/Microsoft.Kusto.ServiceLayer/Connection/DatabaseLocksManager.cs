//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Kusto.ServiceLayer.Connection
{
    public class DatabaseLocksManager: IDisposable
    {
        internal DatabaseLocksManager(int waitToGetFullAccess)
        {
        }

        private static DatabaseLocksManager instance = new DatabaseLocksManager(DefaultWaitToGetFullAccess);

        public static DatabaseLocksManager Instance
        {
            get
            {
                return instance;
            }
        }

        public ConnectionService ConnectionService { get; set; }

        private Dictionary<string, ManualResetEvent> databaseAccessEvents = new Dictionary<string, ManualResetEvent>();
        public const int DefaultWaitToGetFullAccess = 10000;

        public void Dispose()
        {
            foreach (var resetEvent in databaseAccessEvents)
            {
                resetEvent.Value.Dispose();
            }
        }
    }
}
