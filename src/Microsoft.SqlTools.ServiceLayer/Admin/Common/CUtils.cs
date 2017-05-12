//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Text;
using System.Xml;
using System.Threading;
using System.IO;
using Microsoft.SqlServer.Management.Common;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Diagnostics;
using System.Data.SqlClient;
using System.Collections;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
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
