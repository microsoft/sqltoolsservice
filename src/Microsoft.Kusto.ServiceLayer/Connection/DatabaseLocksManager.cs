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
        private static readonly DatabaseLocksManager instance = new DatabaseLocksManager();

        public static DatabaseLocksManager Instance
        {
            get
            {
                return instance;
            }
        }

        public ConnectionService ConnectionService { get; set; }

        private readonly Dictionary<string, ManualResetEvent> _databaseAccessEvents = new Dictionary<string, ManualResetEvent>();

        public void Dispose()
        {
            foreach (var resetEvent in _databaseAccessEvents)
            {
                resetEvent.Value.Dispose();
            }
        }
    }
}
