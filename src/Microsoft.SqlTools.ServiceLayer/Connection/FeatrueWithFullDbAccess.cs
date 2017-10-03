//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Any operation that needs full access to databas should implement this interface.
    /// Make sure to call GainAccessToDatabase before the operation and ReleaseAccessToDatabase after
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
        void GainAccessToDatabase();

        /// <summary>
        /// Release the access to db
        /// </summary>
        void ReleaseAccessToDatabase();

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
