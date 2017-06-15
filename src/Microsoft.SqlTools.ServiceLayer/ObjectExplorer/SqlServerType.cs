//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer
{
    /// <summary>
    /// Server Types
    /// </summary>
    public enum SqlServerType
    {
        Unknown,
        Sql2005,
        Sql2008,
        Sql2012,
        Sql2014,
        Sql2016,
        Sql2017,
        AzureV12
    }

    /// <summary>
    /// Includes helper functions for server version and type
    /// </summary>
    public class ServerVersionHelper
    {
        /// <summary>
        /// Converts a server type to ValidForFlag
        /// </summary>
        public static ValidForFlag GetValidForFlag(SqlServerType serverType, Database database = null)
        {
            return GetValidForFlag(serverType, database != null && database.IsSqlDw);
        }

        /// <summary>
        /// Returns true if the given valid for flag is not set or it includes the server version
        /// </summary>
        /// <param name="serverVersion">Server version</param>
        /// <param name="validFor">Valid for flag</param>
        /// <returns></returns>
        public static bool IsValidFor(ValidForFlag serverVersion, ValidForFlag validFor)
        {
            return validFor == ValidForFlag.None || validFor.HasFlag(serverVersion);
        }

        /// <summary>
        /// Converts a server type to ValidForFlag
        /// </summary>
        public static ValidForFlag GetValidForFlag(SqlServerType serverType, bool isSqlDw)
        {
            ValidForFlag validforFlag = ValidForFlag.All;
            if (Enum.TryParse<ValidForFlag>(serverType.ToString(), out validforFlag))
            {
                if (isSqlDw && serverType == SqlServerType.AzureV12)
                {
                    validforFlag = ValidForFlag.SqlDw;
                }
                else
                {
                    //TODO: not supporting SQL DW for on prem 
                }
                return validforFlag;
            }
            return ValidForFlag.All;
        }

        /// <summary>
        /// Creates a server type from the server version
        /// </summary>
        public static SqlServerType CalculateServerType(ServerInfo serverInfo)
        {
            SqlServerType serverType = SqlServerType.Unknown;
            string serverVersion = serverInfo.ServerVersion;

            if (serverInfo.IsCloud)
            {
                serverType = SqlServerType.AzureV12;
            }
            else if (!string.IsNullOrWhiteSpace(serverVersion))
            {
                if (serverVersion.StartsWith("9", StringComparison.Ordinal) ||
                    serverVersion.StartsWith("09", StringComparison.Ordinal))
                {
                    serverType = SqlServerType.Sql2005;
                }
                else if (serverVersion.StartsWith("10", StringComparison.Ordinal))
                {
                    serverType = SqlServerType.Sql2008; // and 2008R2
                }
                else if (serverVersion.StartsWith("11", StringComparison.Ordinal))
                {
                    serverType = SqlServerType.Sql2012;
                }
                else if (serverVersion.StartsWith("12", StringComparison.Ordinal))
                {
                    serverType = SqlServerType.Sql2014;
                }
                else if (serverVersion.StartsWith("13", StringComparison.Ordinal))
                {
                    serverType = SqlServerType.Sql2016;
                }
                else if (serverVersion.StartsWith("14", StringComparison.Ordinal))
                {
                    serverType = SqlServerType.Sql2017;
                }
            }

            return serverType;
        }
    }
}
