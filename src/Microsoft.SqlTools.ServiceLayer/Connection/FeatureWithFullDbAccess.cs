//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Any operation that needs full access to databas should implement this interface.
    /// Make sure to call GainAccessToDatabaseAsync before the operation and ReleaseAccessToDatabaseAsync after
    /// </summary>
    public interface IFeatureWithFullDbAccess
    {
        /// <summary>
        /// Database Lock Manager
        /// </summary>
        DatabaseLocksManager LockedDatabaseManager { get; set; }

        /// <summary>
        /// Makes sure the feature has fill access to the database
        /// </summary>
        Task<bool> GainAccessToDatabaseAsync();

        /// <summary>
        /// Release the access to db
        /// </summary>
        Task<bool> ReleaseAccessToDatabaseAsync();

        /// <summary>
        /// Server name
        /// </summary>
        string ServerName { get; }

        /// <summary>
        /// Database name
        /// </summary>
        string DatabaseName { get; }
    }

}
