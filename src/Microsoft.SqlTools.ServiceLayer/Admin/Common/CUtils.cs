//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Text;
using System.Xml;
//using System.Drawing;
//using System.Windows.Forms;
using System.Threading;
using System.IO;
//using Microsoft.NetEnterpriseServers;
//using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using Microsoft.SqlServer.Management.Common;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Diagnostics;
//using Microsoft.SqlServer.Management.SqlMgmt;
using System.Data.SqlClient;
// using System.Management;
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


        //public Bitmap LoadBitmap(string szBitmapName)
        //{
        //    Bitmap bmp = null;
        //    Stream s = null;
        //    string strQualifiedName;

        //    strQualifiedName = typeof(CUtils).Namespace + ".Images." + szBitmapName;

        //    s = typeof(CUtils).Assembly.GetManifestResourceStream(strQualifiedName);

        //    if (s != null)
        //    {
        //        bmp = new Bitmap(s);
        //        return bmp;
        //    }

        //    return null;
        //}

        //public Icon LoadIcon(string strName)
        //{

        //    Icon ico = null;
        //    Stream s = null;
        //    string strQualifiedName;

        //    strQualifiedName = typeof(CUtils).Namespace + ".Images." + strName;

        //    s = typeof(CUtils).Assembly.GetManifestResourceStream(strQualifiedName);

        //    if (s != null)
        //    {
        //        int iconSize = DpiUtil.GetScaledImageSize();
        //        ico = new Icon(s, iconSize, iconSize);
        //        return ico;
        //    }

        //    return null;
        //}

        //public void LoadAddIcon(ImageList imageList, string strName)
        //{
        //    Icon ico = null;
        //    Stream s = null;
        //    string strQualifiedName;

        //    strQualifiedName = typeof(CUtils).Namespace + ".Images." + strName;

        //    s = typeof(CUtils).Module.Assembly.GetManifestResourceStream(strQualifiedName);

        //    if (s != null)
        //    {
        //        try
        //        {
        //            ico = new Icon(s, 16, 16);
        //            imageList.Images.Add(ico);
        //        }
        //        finally
        //        {
        //            if (ico != null)
        //                ico.Dispose();
        //        }
        //    }
        //}

        public static void UseMaster(SMO.Server server)
        {
            server.ConnectionContext.ExecuteNonQuery("use master");
        }


        ///// <summary>
        ///// returns height of my border (depending on its style)
        ///// </summary>
        //public static int GetBorderHeight(BorderStyle style)
        //{
        //    if (style == BorderStyle.FixedSingle)
        //    {
        //        return SystemInformation.BorderSize.Height;
        //    }
        //    else if (style == BorderStyle.Fixed3D)
        //    {
        //        return SystemInformation.Border3DSize.Height;
        //    }
        //    else
        //    {
        //        return 0;
        //    }
        //}

        //public static int GetBorderWidth(BorderStyle style)
        //{
        //    if (style == BorderStyle.FixedSingle)
        //    {
        //        return SystemInformation.BorderSize.Width;
        //    }
        //    else if (style == BorderStyle.Fixed3D)
        //    {
        //        return SystemInformation.Border3DSize.Width;
        //    }
        //    else
        //    {
        //        return 0;
        //    }
        //}

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
        /// validates current value of given numeric control. Shows message if the value is not valid
        /// </summary>
        /// <param name="numControl"></param>
        /// <param name="errMessageToShow"></param>
        /// <returns>true if control's value is valid, false otherwise</returns>
        //public static bool ValidateNumeric(NumericUpDown numControl, string errMessageToShow, bool displayException)
        //{
        //    try
        //    {
        //        int curValue = int.Parse(numControl.Text, System.Globalization.CultureInfo.CurrentCulture);
        //        if (curValue < numControl.Minimum || curValue > numControl.Maximum)
        //        {
        //            if (true == displayException)
        //            {
        //                ExceptionMessageBox box = new ExceptionMessageBox();

        //                box.Caption = SRError.SQLWorkbench;
        //                box.Message = new Exception(errMessageToShow);
        //                box.Symbol = ExceptionMessageBoxSymbol.Error;
        //                box.Buttons = ExceptionMessageBoxButtons.OK;
        //                box.Options = ExceptionMessageBoxOptions.RightAlign;
        //                box.Show(null);
        //            }

        //            try
        //            {
        //                numControl.Value = Convert.ToDecimal(numControl.Tag, System.Globalization.CultureInfo.CurrentCulture);
        //                numControl.Update();
        //                numControl.Text = Convert.ToString(numControl.Value, System.Globalization.CultureInfo.CurrentCulture);
        //                numControl.Refresh();

        //            }
        //            catch
        //            {

        //            }
        //            numControl.Focus();
        //            return false;
        //        }

        //        return true;
        //    }
        //    catch
        //    {
        //        if (true == displayException)
        //        {
        //            ExceptionMessageBox box = new ExceptionMessageBox();

        //            box.Caption = SRError.SQLWorkbench;
        //            box.Message = new Exception(errMessageToShow);
        //            box.Symbol = ExceptionMessageBoxSymbol.Error;
        //            box.Buttons = ExceptionMessageBoxButtons.OK;
        //            box.Options = ExceptionMessageBoxOptions.RightAlign;
        //            box.Show(null);
        //        }

        //        numControl.Focus();
        //        return false;
        //    }
        //}

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
        /// Helper method to convert DMTF format to DateTime from WMI property value
        /// ManagementDateTimeConverter.ToDateTime() does not adjust time to UTC offset,
        /// hence additional step to adjust datetime.
        /// </summary>
        /// <param name="dateTimeInDMTFFormat"></param>
        /// <returns></returns>
        //public static DateTime GetDateTimeFromDMTFTime(string dateTimeInDMTFFormat)
        //{
        //    string[] dateTimeInfo = dateTimeInDMTFFormat.Split(new char[] { '+', '-' });
        //    DateTime dateTime = ManagementDateTimeConverter.ToDateTime(dateTimeInDMTFFormat);
           
        //    TimeSpan timeSpan = TimeSpan.FromMinutes(Convert.ToDouble(dateTimeInfo[1]));
        //    if (dateTimeInDMTFFormat.Contains("+"))
        //    {
        //        dateTime = dateTime - timeSpan;
        //    }
        //    else
        //    {
        //        dateTime = dateTime + timeSpan;
        //    }
        //    return dateTime;
        //}

        /// <summary>
        /// Helper method to sort ManagementObjectCollection based ArchiveNumber property
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        //public static ArrayList Sort(ManagementObjectCollection collection)
        //{

        //    ArrayList array = new ArrayList();
        //    array.AddRange(collection);
        //    ArchiveNoComparer comparer = new ArchiveNoComparer();
        //    array.Sort(comparer);
        //    return array;
        //}

        /// <summary>
        /// Helper function to execute WQL
        /// </summary>
        /// <param name="wmiNamespace"></param>
        /// <param name="wql"></param>
        /// <returns></returns>
        //public static ManagementObjectCollection ExecuteWQL(WmiSqlMgmtConnectionInfo wmiCi, string wql)
        //{
        //    ObjectQuery qry = new ObjectQuery(wql);
        //    ManagementScope scope = new ManagementScope(wmiCi.Namespace, wmiCi.ConnectionOptions);
        //    scope.Connect();
        //    ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, qry);
        //    return searcher.Get();
        //}

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

        /// <summary>
        /// Launches object picker and gets the user selected login
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="server"></param>
        /// <param name="errorMsgToShowForTooManyLogins"></param>
        /// <returns>Returns null in case Object picker doesn't returns any loginName</returns>
        //public static string GetWindowsLoginNameFromObjectPicker(object sender,
        //                                                            Smo.Server server,
        //                                                            string errorMsgToShowForTooManyLogins)
        //{
        //    string loginName = null;

        //    ObjectPickerWrapper.TargetMachine = server.Information.NetName;
        //    ObjectPickerWrapper.SingleObjectSelection = true;

        //    ObjectPickerWrapper.GetUsersList(sender);

        //    int userCount = ObjectPickerWrapper.UsersList.Count;

        //    // if the user selected one NT login, set the edit control text to the selected login
        //    if (1 == userCount)
        //    {
        //        loginName = ObjectPickerWrapper.UsersList[0].ToString();

        //        if (loginName.Length != 0)
        //        {
        //            loginName = CanonicalizeWindowsLoginName(loginName);
        //        }
        //    }
        //    // if the user selected more than one login, display an error
        //    else if (1 < userCount)
        //    {
        //        SqlManagementUserControl sm = sender as SqlManagementUserControl;
        //        if (sm != null)
        //        {
        //            sm.DisplayExceptionMessage(new Exception(errorMsgToShowForTooManyLogins));
        //        }
        //        else
        //        {
        //            throw new InvalidOperationException(errorMsgToShowForTooManyLogins);
        //        }
        //    }

        //    return loginName;
        //}

        /// <summary>
        /// Determines how a feature should behave (e.g. enabled or disabled) for a SQL instance's SKU/edition. 
        /// </summary>
        /// <param name="server">A SMO Server object connected to a local or remote SQL instance</param>
        /// <param name="setting">The setting to check (one of the feature constants used in settings.dat)</param>
        /// <returns>The value of the setting (e.g. SqlbootConst.SKU_YES, SqlbootConst.VALUE_UNLIMITED, etc)</returns>
        //public static uint QueryRemoteSqlProductValue(ServerConnection serverConnection, uint setting)
        //{
        //    if (serverConnection == null)
        //    {
        //        throw new ArgumentNullException("serverConnection");
        //    }

        //    // The instance could be remote, so we use GetSettingValueForSKUAbsolute because it allows us to 
        //    // query the client-side SQLBOOT.DLL to ask what the setting's value would be for the server's SKU. 
        //    // (Most other SQLBOOT APIs can only be used for locally-installed instances.) 
        //    // First we must retrieve the server's edition ID (SKU ID). 
        //    int editionId = (int)serverConnection.ExecuteScalar("SELECT SERVERPROPERTY('EditionId') AS EditionId");

        //    // Then ask SQLBOOT what the setting's value should be for that SKU. 
        //    uint value = Sqlboot.GetSettingValueForSKUAbsolute(setting, unchecked((uint)editionId));
        //    return value;
        //}
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
    /// class to sort files based on archivenumber
    /// </summary>
    //internal class ArchiveNoComparer : IComparer
    //{
    //    public int Compare(Object x, Object y)
    //    {
    //        ManagementObject lhs = x as ManagementObject;
    //        ManagementObject rhs = y as ManagementObject;

    //        if (null == lhs || null == rhs)
    //        {
    //            throw new ArgumentException("Object is not of type ManagementObject");
    //        }

    //        UInt32 l = Convert.ToUInt32(lhs.Properties["archivenumber"].Value);
    //        UInt32 r = Convert.ToUInt32(rhs.Properties["archivenumber"].Value);

    //        int retVal = l.CompareTo(r);

    //        return retVal;
    //    }
    //}

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








