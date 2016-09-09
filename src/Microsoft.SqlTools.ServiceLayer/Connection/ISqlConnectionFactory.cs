//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Data;

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
        IDbConnection CreateSqlConnection(string connectionString);
    }
}
