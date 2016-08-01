//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ConnectionServices.Contracts
{
    /// <summary>
    /// Interface for the SQL Connection factory
    /// </summary>
    public interface ISqlConnectionFactory
    {
        /// <summary>
        /// Create a new SQL Connection object
        /// </summary>
        ISqlConnection CreateSqlConnection(string connectionString);
    }
}
