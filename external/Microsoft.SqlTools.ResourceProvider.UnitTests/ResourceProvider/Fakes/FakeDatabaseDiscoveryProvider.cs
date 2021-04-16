//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes
{
    /// <summary>
    /// A fake database discovery class which generates db names for 5 seconds or until it gets canceled 
    /// </summary>
    public class FakeDatabaseDiscoveryProvider : IDatabaseDiscoveryProvider
    {
        private TimeSpan _timeout = TimeSpan.FromSeconds(5);        
        
        public IExportableMetadata Metadata { get; set; }
        public ExportableStatus Status { get; }
        IExportableMetadata IExportable.Metadata
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        ExportableStatus IExportable.Status
        {
            get { throw new NotImplementedException(); }
        }

        public Task<ServiceResponse<DatabaseInstanceInfo>> GetDatabaseInstancesAsync(string serverName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        //public Task<ServiceResponse<DatabaseInstanceInfo>> GetDatabaseInstancesAsync(UIConnectionInfo uiConnectionInfo, CancellationToken cancellationToken)
        //{
        //    return Task.Factory.StartNew(() => GetDatabaseInstances(uiConnectionInfo, cancellationToken), cancellationToken);
        //}

        //private ServiceResponse<DatabaseInstanceInfo> GetDatabaseInstances(UIConnectionInfo uiConnectionInfo, CancellationToken cancellationToken)
        //{
        //    List<DatabaseInstanceInfo> databases = new List<DatabaseInstanceInfo>();
        //    DateTime startTime = DateTime.UtcNow;
        //    while (!cancellationToken.IsCancellationRequested)
        //    {
        //        DateTime now = DateTime.UtcNow;
        //        if (now.Subtract(startTime).TotalMilliseconds >= _timeout.TotalMilliseconds)
        //        {
        //            break;
        //        }
        //        databases.Add(new DatabaseInstanceInfo(ServerDefinition.Default, uiConnectionInfo.ServerName, uiConnectionInfo.ServerName + "" + Guid.NewGuid().ToString()));
        //    }

        //    return new ServiceResponse<DatabaseInstanceInfo>(databases);
        //}

        private static void TimerCallback(object state)
        {

        }

        private void OnDatabaseFound(DatabaseInstanceInfo databaseInfo)
        {
            if (DatabaseFound != null)
            {
                DatabaseFound(this, new DatabaseInfoEventArgs() { Database = databaseInfo });
            }
        }

        public void SetServiceProvider(IMultiServiceProvider provider)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<DatabaseInfoEventArgs> DatabaseFound;
    }
}
