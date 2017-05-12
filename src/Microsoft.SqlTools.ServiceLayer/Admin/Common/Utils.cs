//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

//extern alias VSShell2;
//extern alias VSShell2Iop;
using System;
using System.Reflection;
using System.Globalization;
using Microsoft.SqlServer.Management.Diagnostics;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
//using VSShell2Iop.Microsoft.VisualStudio.Shell.Interop;
//using VSShell2.Microsoft.VisualStudio.Shell;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Internal reusable helpers
    /// </summary>
    internal class Utils
    {
        /// <summary>
        /// only static methods
        /// </summary>
        private Utils() { }

        /// <summary>
        /// returns all instances of the given custom attribute on a given object
        /// </summary>
        /// <param name="objectToGetAttributeFrom"></param>
        /// <param name="customAttribute"></param>
        /// <returns></returns>
        public static Attribute GetCustomAttribute(object objectToGetAttributeFrom, Type customAttribute)
        {         
            //first, see if the object implemented this interface to override standard behavior
            System.Reflection.ICustomAttributeProvider attribProvider = objectToGetAttributeFrom as System.Reflection.ICustomAttributeProvider;
            if (attribProvider == null)
            {
                //if not, get it from its type
                attribProvider = (System.Reflection.ICustomAttributeProvider)objectToGetAttributeFrom.GetType();
            }

            object[] attribs = attribProvider.GetCustomAttributes(customAttribute, true);
            if (attribs != null && attribs.Length > 0)
            {
                //NOTE: important that we'll always use the first one in collection.
                //Our implementation of ICustomAttributeProvider knows about that and
                //relies on this behavior
                return attribs[0] as Attribute;
            }

            return null;
        }


        /// <summary>
        /// called to create SqlConnectionInfo out of the given CDataContainer object
        /// </summary>
        /// <param name="dc"></param>
        /// <returns></returns>
        public static SqlConnectionInfo GetSqlConnectionInfoFromDataContainer(CDataContainer dc)
        {
            if (dc != null)
            {
                // we may have been given conneciton information by the object explorer. in which case there is no need
                // to build it ourselves.
                SqlConnectionInfo result = dc.ConnectionInfo as SqlConnectionInfo;
                if (result == null)
                {
                    throw new InvalidOperationException();
                }

                return result;
            }
            else
            {
                return null;
            }
        }

        public static int InitialTreeViewWidth
        {
            get
            {
                return 175;
            }
        }

        /// <summary>
        /// Try to set the CLR thread name. 
        /// Will not throw if the name is already set.
        /// </summary>
        /// <param name="name"></param>
        public static void TrySetThreadName(String name)
        {
            try
            {
                System.Threading.Thread.CurrentThread.Name = name;
            }
            catch (InvalidOperationException)
            { }
        }

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
            bool isXTPSupported = false;

            if (server.ConnectionContext.ExecuteScalar("SELECT SERVERPROPERTY('IsXTPSupported')") != DBNull.Value)
            {
                isXTPSupported = server.IsXTPSupported;
            }

            return isXTPSupported;
        }

        /// <summary>
        /// Returns true if given database has memory optimized filegroup on given server.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="dbName"></param>
        /// <returns></returns>
        public static bool HasMemoryOptimizedFileGroup(SMO.Server server, string dbName)
        {
            bool hasMemoryOptimizedFileGroup = false;

            if (server.ServerType != DatabaseEngineType.SqlAzureDatabase)
            {
                string query = string.Format(CultureInfo.InvariantCulture,
                                                            "select top 1 1 from [{0}].sys.filegroups where type = 'FX'",
                                                            CUtils.EscapeString(dbName, ']'));
                if (server.ConnectionContext.ExecuteScalar(query) != null)
                {
                    hasMemoryOptimizedFileGroup = true;
                }
            }

            return hasMemoryOptimizedFileGroup;
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

        public static string MakeSqlBracket(string s)
        {
            return "[" + s.Replace("]", "]]") + "]";
        }
    }

    /// <summary>
    /// Public reusable helpers
    /// </summary>
    public class SqlMgmtUtils
    {
        /// <summary>
        /// only static methods
        /// </summary>
        private SqlMgmtUtils() { }

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
