//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlServer.Management.Common;
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
        Sql2019,
        Sql2022,
        AzureV12,
        SqlOnDemand,
        AzureSqlDWGen3
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
            // If either the flag is not set or if the serverVersion has a default value of "all", allow the check
            // Otherwise, actually do the comparison of the flags
            return validFor == ValidForFlag.None || serverVersion == ValidForFlag.All || validFor.HasFlag(serverVersion);
        }

        /// <summary>
        /// Converts a server type to ValidForFlag
        /// </summary>
        public static ValidForFlag GetValidForFlag(SqlServerType serverType, bool isSqlDw)
        {
            ValidForFlag validforFlag = ValidForFlag.All;
            if (Enum.TryParse<ValidForFlag>(serverType.ToString(), out validforFlag))
            {
                if ((isSqlDw && serverType == SqlServerType.AzureV12) || serverType == SqlServerType.AzureSqlDWGen3)
                {
                    validforFlag = ValidForFlag.SqlDw;
                }
                else if (serverType == SqlServerType.SqlOnDemand)
                {
                    validforFlag = ValidForFlag.SqlOnDemand;
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
            string serverVersion = serverInfo.ServerVersion;

            if (serverInfo.EngineEditionId == 11)
            {
                return SqlServerType.SqlOnDemand;
            }
            else if (serverInfo.IsCloud)
            {
                if (serverInfo.EngineEditionId == (int)DatabaseEngineEdition.SqlDataWarehouse 
                    && serverVersion.StartsWith("12", StringComparison.Ordinal))
                {
                    return SqlServerType.AzureSqlDWGen3;
                }
                else
                {
                    return SqlServerType.AzureV12;
                }
            }
            else if (!string.IsNullOrWhiteSpace(serverVersion))
            {
                if (serverVersion.StartsWith("9", StringComparison.Ordinal) ||
                    serverVersion.StartsWith("09", StringComparison.Ordinal))
                {
                    return SqlServerType.Sql2005;
                }
                else if (serverVersion.StartsWith("10", StringComparison.Ordinal))
                {
                    return SqlServerType.Sql2008; // and 2008R2
                }
                else if (serverVersion.StartsWith("11", StringComparison.Ordinal))
                {
                    return SqlServerType.Sql2012;
                }
                else if (serverVersion.StartsWith("12", StringComparison.Ordinal))
                {
                    return SqlServerType.Sql2014;
                }
                else if (serverVersion.StartsWith("13", StringComparison.Ordinal))
                {
                    return SqlServerType.Sql2016;
                }
                else if (serverVersion.StartsWith("14", StringComparison.Ordinal))
                {
                    return SqlServerType.Sql2017;
                }
                else if (serverVersion.StartsWith("15", StringComparison.Ordinal))
                {
                    return SqlServerType.Sql2019;
                }
                else if (serverVersion.StartsWith("16", StringComparison.Ordinal))
                {
                    return SqlServerType.Sql2022;
                }
                else
                {
                    // vNext case - default to latest version
                    return SqlServerType.Sql2022;
                }
            }
            return SqlServerType.Unknown;
        }
    }
}
