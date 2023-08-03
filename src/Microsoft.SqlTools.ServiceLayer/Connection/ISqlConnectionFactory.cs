//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Data.Common;

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
        /// <param name="enableSeverlessRetryPolicy">Enable to use the RetryLogicProvider for handling instances of sleeping serverless databases taking time to wake up.</param>
        DbConnection CreateSqlConnection(string connectionString, string azureAccountToken, bool enableServerlessRetryPolicy = false);
    }
}
