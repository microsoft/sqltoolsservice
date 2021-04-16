//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.SqlTools.Hosting.Contracts.Connection;

namespace Microsoft.Kusto.ServiceLayer.Connection
{
    /// <summary>
    /// Interface for the SQL Connection factory
    /// </summary>
    public interface IDataSourceConnectionFactory
    {
        /// <summary>
        /// Create a new SQL Connection object
        /// </summary>
        ReliableDataSourceConnection CreateDataSourceConnection(ConnectionDetails connectionDetails, string ownerUri);
    }
}
