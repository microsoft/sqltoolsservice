//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.ConnectionServices.Contracts
{
    /// <summary>
    /// Interface for the SQL Connection wrapper
    /// </summary>
    public interface ISqlConnection : IDbConnection
    {
        ///// <summary>
        ///// Open a connection to the provided connection string
        ///// </summary>
        ///// <param name="connectionString"></param>
        //void OpenDatabaseConnection(string connectionString);

        //IEnumerable<string> GetServerObjects();

        string DataSource { get; }

        string ServerVersion { get; }

        void ClearPool();

        Task OpenAsync();

        Task OpenAsync(CancellationToken token);
    }
}
