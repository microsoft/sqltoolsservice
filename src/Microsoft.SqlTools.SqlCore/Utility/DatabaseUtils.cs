//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using Microsoft.SqlServer.Management.Smo;


namespace Microsoft.SqlTools.SqlCore.Utility
{
    public static class DatabaseUtils
    {
        /// <summary>
        /// Identifies Azure SQL developer containers by their server edition ID.
        /// </summary>
        public static bool IsAzureSqlDatabaseContainer(Server server)
        {
            object result = server.ConnectionContext.ExecuteScalar("SELECT CAST(SERVERPROPERTY('EditionID') AS bigint) AS EditionID;");
            return result != null && result != DBNull.Value &&
                Convert.ToInt64(result, CultureInfo.InvariantCulture) == -978196525;
        }

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
