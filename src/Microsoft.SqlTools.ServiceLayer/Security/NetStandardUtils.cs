//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Globalization;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// NetStandard compatible helpers
    /// </summary>
#if NETCOREAPP2_0
    public class Utils
#else
    internal partial class Utils
#endif
    {
        private Utils() { }

        public static bool IsKatmaiOrLater(int version)
        {
            return (10 <= version);
        }

        public static bool IsKjOrLater(ServerVersion version)
        {
            return (version.Major > 10
                    || (version.Major == 10 && version.Minor >= 50));
        }

        public static bool IsSql11OrLater(ServerVersion version)
        {
            return IsSql11OrLater(version.Major);
        }

        public static bool IsSql11OrLater(int versionMajor)
        {
            return (versionMajor >= 11);
        }

        public static bool IsSql12OrLater(ServerVersion version)
        {
            return IsSql12OrLater(version.Major);
        }

        public static bool IsSql12OrLater(int versionMajor)
        {
            return (versionMajor >= 12);
        }

        public static bool IsSql13OrLater(ServerVersion version)
        {
            return IsSql13OrLater(version.Major);
        }

        public static bool IsSql13OrLater(int versionMajor)
        {
            return (versionMajor >= 13);
        }

        public static bool IsSql14OrLater(ServerVersion version)
        {
            return IsSql14OrLater(version.Major);
        }

        public static bool IsSql14OrLater(int versionMajor)
        {
            return (versionMajor >= 14);
        }

        public static bool IsSql15OrLater(ServerVersion version)
        {
            return IsSql15OrLater(version.Major);
        }

        public static bool IsSql15OrLater(int versionMajor)
        {
            return (versionMajor >= 15);
        }


        /// <summary>
        /// Check if the version is SQL 2019 CU4 or later.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        /// <remarks>
        /// SQL2019 CU3 is going to be 4023; CU4 is going to be 4033
        /// SQL2019 CU4 (before the snap to the release branch) is 4028.
        /// </remarks>
        public static bool IsSql15OCU4OrLater(Version version)
        {
            return(version >= new Version(15, 0, 4028));
        }

        /// <summary>
        /// Check if the version is SQL 2016 SP1 or later.
        /// </summary>
        /// <param name="version"></param>
        /// <returns>true if the version is SQL 2016 SP1 or later, false otherwise</returns>
        public static bool IsSql13SP1OrLater(Version version)
        {
            return (version >= new Version(13, 0, 3510));
        }

        public static bool IsXTPSupportedOnServer(SMO.Server server)
        {
            if(server.DatabaseEngineEdition == DatabaseEngineEdition.SqlOnDemand)
            {
                return false;
            }
            bool isXTPSupported = false;

            if (server.ConnectionContext.ExecuteScalar("SELECT SERVERPROPERTY('IsXTPSupported')") != DBNull.Value)
            {
                isXTPSupported = server.IsXTPSupported;
            }

            return isXTPSupported;
        }

        public static bool IsPolybasedInstalledOnServer(SMO.Server server)
        {
            bool isPolybaseInstalled = false;

            if (server.IsSupportedProperty("IsPolyBaseInstalled"))
            {
                isPolybaseInstalled = server.IsPolyBaseInstalled;
            }

            return isPolybaseInstalled;
        }

        /// <summary>
        /// Returns true if current user has given permission on given server.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="permissionName"></param>
        /// <returns></returns>
        public static bool HasPermissionOnServer(SMO.Server server, string permissionName)
        {
            return Convert.ToBoolean(server.ConnectionContext.ExecuteScalar(
                                                    string.Format(CultureInfo.InvariantCulture,
                                                    "SELECT HAS_PERMS_BY_NAME(null, null, '{0}');",
                                                    permissionName)));
        }

        public static bool FilestreamEnabled(SMO.Server svr)
        {
            bool result = false;
            if (svr != null)
            {
                if (IsKatmaiOrLater(svr.Information.Version.Major)
                    && svr.ServerType != DatabaseEngineType.SqlAzureDatabase) //Azure doesn't support filestream
                {
                    if (svr.Configuration.FilestreamAccessLevel.RunValue != 0)
                    {
                        result = true;
                    }
                }
            }

            return result;
        }

        public static bool IsYukonOrAbove(SMO.Server server)
        {
            return server.Version.Major >= 9;
        }

        public static bool IsBelowYukon(SMO.Server server)
        {
            return server.Version.Major < 9;
        }

        /// <summary>
        /// Some calendars, such as the UmAlQuraCalendar, support an upper date range that is earlier than MaxValue. 
        /// In these cases, trying to access MaxValue in variable assignments or formatting and parsing operations can throw 
        /// an ArgumentOutOfRangeException. Rather than retrieving the value of DateTime.MaxValue, you can retrieve the value 
        /// of the specified culture's latest valid date value from the 
        /// System.Globalization.CultureInfo.DateTimeFormat.Calendar.MaxSupportedDateTime property. 
        /// http://msdn.microsoft.com/en-us/library/system.datetime.maxvalue(v=VS.90).aspx
        /// </summary>
        /// <returns></returns>
        public static DateTime GetMaxCultureDateTime()
        {
            CultureInfo currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            return currentCulture.DateTimeFormat.Calendar.MaxSupportedDateTime;
        }

        public static string MakeSqlBracket(string s)
        {
            return "[" + s.Replace("]", "]]") + "]";
        }

        /// <summary>
        /// Returns whether the server is in AS Azure
        /// </summary>
        /// <param name="serverName"></param>
        /// <returns></returns>
        public static bool IsASAzure(string serverName)
        {
            return !string.IsNullOrEmpty(serverName) && serverName.StartsWith("asazure://", StringComparison.OrdinalIgnoreCase);
        }
    }
}
