//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text;
using System.Xml;
using System.Threading;
using System.IO;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Data.SqlClient;
using System.Collections;
using System.Reflection;
using System.Globalization;
using SMO = Microsoft.SqlServer.Management.Smo;

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


    /// <summary>
    /// Summary description for CUtils.
    /// </summary>
    internal class CUtils
    {

        private const int ObjectPermissionsDeniedErrorNumber = 229;
        private const int ColumnPermissionsDeniedErrorNumber = 230;

        public CUtils()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        public static void UseMaster(SMO.Server server)
        {
            server.ConnectionContext.ExecuteNonQuery("use master");
        }

        /// <summary>
        /// Get a SMO Server object that is connected to the connection
        /// </summary>
        /// <param name="ci">Conenction info</param>
        /// <returns>Smo Server object for the connection</returns>
        public static Microsoft.SqlServer.Management.Smo.Server GetSmoServer(IManagedConnection mc)
        {
            SqlOlapConnectionInfoBase ci = mc.Connection;
            if (ci == null)
            {
                throw new ArgumentNullException("ci");
            }

            SMO.Server server = null;

            // see what type of connection we have been passed
            SqlConnectionInfoWithConnection ciWithCon = ci as SqlConnectionInfoWithConnection;

            if (ciWithCon != null)
            {
                server = new SMO.Server(ciWithCon.ServerConnection);
            }
            else
            {
                SqlConnectionInfo sqlCi = ci as SqlConnectionInfo;
                if (sqlCi != null)
                {
                    server = new SMO.Server(new ServerConnection(sqlCi));
                }
            }

            if (server == null)
            {
                throw new InvalidOperationException();
            }
            return server;

        }

        public static int GetServerVersion(SMO.Server server)
        {
            return server.Information.Version.Major;
        }

        /// <summary>
        /// Determines the oldest date based on the type of time units and the number of time units
        /// </summary>
        /// <param name="numUnits"></param>
        /// <param name="typeUnits"></param>
        /// <returns></returns>
        public static DateTime GetOldestDate(int numUnits, TimeUnitType typeUnits)
        {
            DateTime result = DateTime.Now;

            switch (typeUnits)
            {
                case TimeUnitType.Week:
                    {
                        result = (DateTime.Now).AddDays(-1 * 7 * numUnits);
                        break;
                    }
                case TimeUnitType.Month:
                    {
                        result = (DateTime.Now).AddMonths(-1 * numUnits);
                        break;
                    }
                case TimeUnitType.Year:
                    {
                        result = (DateTime.Now).AddYears(-1 * numUnits);
                        break;
                    }
                default:
                    {
                        result = (DateTime.Now).AddDays(-1 * numUnits);
                        break;
                    }
            }

            return result;
        }

        public static string TokenizeXml(string s)
        {
            if (null == s) return String.Empty;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in s)
            {
                switch (c)
                {
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '&':
                        sb.Append("&amp;");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Tries to get the SqlException out of an Enumerator exception
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static SqlException GetSqlException(Exception e)
        {
            SqlException sqlEx = null;
            Exception exception = e;
            while (exception != null)
            {
                sqlEx = exception as SqlException;
                if (null != sqlEx)
                {
                    break;
                }
                exception = exception.InnerException;
            }
            return sqlEx;
        }

        /// <summary>
        /// computes the name of the machine based on server's name (as returned by smoServer.Name)
        /// </summary>
        /// <param name="sqlServerName">name of server ("",".","Server","Server\Instance",etc)</param>
        /// <returns>name of the machine hosting sql server instance</returns>
        public static string GetMachineName(string sqlServerName)
        {
            System.Diagnostics.Debug.Assert(sqlServerName != null);

            string machineName = sqlServerName;
            if (sqlServerName.Trim().Length != 0)
            {
                // [0] = machine, [1] = instance (if any)
                return sqlServerName.Split('\\')[0];
            }
            else
            {
                // we have default instance of default machine
                return machineName;
            }
        }

        /// <summary>
        /// Determines if a SqlException is Permission denied exception
        /// </summary>
        /// <param name="sqlException"></param>
        /// <returns></returns>
        public static bool IsPermissionDeniedException(SqlException sqlException)
        {
            bool isPermDenied = false;
            if (null != sqlException.Errors)
            {
                foreach (SqlError sqlError in sqlException.Errors)
                {
                    int errorNumber = GetSqlErrorNumber(sqlError);

                    if ((ObjectPermissionsDeniedErrorNumber == errorNumber) ||
                        (ColumnPermissionsDeniedErrorNumber == errorNumber))
                    {
                        isPermDenied = true;
                        break;
                    }
                }
            }
            return isPermDenied;
        }

        /// <summary>
        /// Returns the error number of a sql exeception
        /// </summary>
        /// <param name="sqlerror"></param>
        /// <returns></returns>
        public static int GetSqlErrorNumber(SqlError sqlerror)
        {
            return sqlerror.Number;
        }

        /// <summary>
        /// Function doubles up specified character in a string
        /// </summary>
        /// <param name="s"></param>
        /// <param name="cEsc"></param>
        /// <returns></returns>
        public static String EscapeString(string s, char cEsc)
        {
            StringBuilder sb = new StringBuilder(s.Length * 2);
            foreach (char c in s)
            {
                sb.Append(c);
                if (cEsc == c)
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Function doubles up ']' character in a string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static String EscapeStringCBracket(string s)
        {
            return CUtils.EscapeString(s, ']');
        }

        /// <summary>
        /// Function doubles up '\'' character in a string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static String EscapeStringSQuote(string s)
        {
            return CUtils.EscapeString(s, '\'');
        }

        /// <summary>
        /// Function removes doubled up specified character from a string
        /// </summary>
        /// <param name="s"></param>
        /// <param name="cEsc"></param>
        /// <returns></returns>
        public static String UnEscapeString(string s, char cEsc)
        {
            StringBuilder sb = new StringBuilder(s.Length);
            bool foundBefore = false;
            foreach (char c in s)
            {
                if (cEsc == c) // character to unescape
                {
                    if (foundBefore) // skip second occurrence
                    {
                        foundBefore = false;
                    }
                    else // set the flag to skip next time around
                    {
                        sb.Append(c);
                        foundBefore = true;
                    }
                }
                else
                {
                    sb.Append(c);
                    foundBefore = false;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Function removes doubled up ']' character from a string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static String UnEscapeStringCBracket(string s)
        {
            return CUtils.UnEscapeString(s, ']');
        }

        /// <summary>
        /// Function removes doubled up '\'' character from a string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static String UnEscapeStringSQuote(string s)
        {
            return CUtils.UnEscapeString(s, '\'');
        }

        /// <summary>
        /// Get the windows login name with the domain portion in all-caps
        /// </summary>
        /// <param name="windowsLoginName">The windows login name</param>
        /// <returns>The windows login name with the domain portion in all-caps</returns>
        public static string CanonicalizeWindowsLoginName(string windowsLoginName)
        {
            string result;
            int lastBackslashIndex = windowsLoginName.LastIndexOf("\\", StringComparison.Ordinal);

            if (-1 != lastBackslashIndex)
            {
                string domainName = windowsLoginName.Substring(0, lastBackslashIndex).ToUpperInvariant();
                string afterDomain = windowsLoginName.Substring(lastBackslashIndex);

                result = String.Concat(domainName, afterDomain);
            }
            else
            {
                result = windowsLoginName;
            }

            return result;

        }
    }

    /// <summary>
    /// Enum of time units types ( used in cleaning up history based on age )
    /// </summary>
    internal enum TimeUnitType
    {
        Day,
        Week,
        Month,
        Year
    }

    /// <summary>
    /// Object used to populate default language in 
    /// database and user dialogs.
    /// </summary>
    internal class LanguageDisplay
    {
        private SMO.Language language;

        public string LanguageAlias
        {
            get
            {
                return language.Alias;
            }
        }

        public SMO.Language Language
        {
            get
            {
                return language;
            }
        }

        public LanguageDisplay(SMO.Language language)
        {
            this.language = language;
        }

        public override string ToString()
        {
            return language.Alias;
        }
    }
}
