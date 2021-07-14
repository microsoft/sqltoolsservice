//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.Kusto.ServiceLayer.Utility
{
    public class DatabaseUtils
    {
        /// <summary>
        /// Check if the database is a system database
        /// </summary>
        /// <param name="databaseName">the name of database</param>
        /// <returns>return true if the database is a system database</returns>
        public static bool IsSystemDatabaseConnection(string databaseName)
        {
            return (string.IsNullOrWhiteSpace(databaseName) ||
                string.Compare(databaseName, CommonConstants.MasterDatabaseName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(databaseName, CommonConstants.MsdbDatabaseName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(databaseName, CommonConstants.ModelDatabaseName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(databaseName, CommonConstants.TempDbDatabaseName, StringComparison.OrdinalIgnoreCase) == 0);
        }
    }
}
