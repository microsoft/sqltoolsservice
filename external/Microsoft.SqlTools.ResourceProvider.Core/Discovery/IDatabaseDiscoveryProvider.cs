//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;

namespace Microsoft.SqlTools.ResourceProvider.Core
{
    /// <summary>
    /// A discovery provider capable of finding databases for a given server type and category. 
    /// For example: finding SQL Server databases in Azure, or on the local network. 
    /// Implementing classes must add a <see cref="ExportableAttribute" /> 
    /// to the class in order to be found by the extension manager, 
    /// and to define the type and category supported
    /// </summary>
    public interface IDatabaseDiscoveryProvider : IExportable
    {
        /// <summary>
        /// Returns the databases for given server name. 
        /// </summary> 
        Task<ServiceResponse<DatabaseInstanceInfo>> GetDatabaseInstancesAsync(string serverName, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the databases for given connection info. 
        /// The connection info should be used to make the connection for getting databases not the account manager
        /// </summary> 
        //Task<ServiceResponse<DatabaseInstanceInfo>> GetDatabaseInstancesAsync(UIConnectionInfo uiConnectionInfo, CancellationToken cancellationToken);

        /// <summary>
        /// the event to raise when a database is found
        /// </summary>
        event EventHandler<DatabaseInfoEventArgs> DatabaseFound;
    }
}
