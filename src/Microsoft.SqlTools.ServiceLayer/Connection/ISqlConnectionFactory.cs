//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Interface for the SQL Connection factory
    /// </summary>
    public interface ISqlConnectionFactory
    {
        /// <summary>
        /// Create a new SQL Connection object
        /// </summary>
        /// <param name="retryProvider">Optional retry provider to handle errors in a special way</param>
        DbConnection CreateSqlConnection(string connectionString, string azureAccountToken, SqlRetryLogicBaseProvider retryProvider = null);
    }
}
