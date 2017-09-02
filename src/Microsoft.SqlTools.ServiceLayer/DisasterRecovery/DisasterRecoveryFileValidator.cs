//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.FileBrowser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    /// <summary>
    /// Validate selected file paths for backup/restore operations
    /// </summary>
    public static class DisasterRecoveryFileValidator
    {
        private const string localSqlServer = "(local)";
        private const string localMachineName = ".";

        public static bool ValidatePaths(FileBrowserValidateEventArgs args)
        {
            bool result = true;
            string errorMessage = "";
            DbConnection dbConn = null;
            ConnectionInfo connInfo;

            ConnectionService.Instance.TryFindConnection(args.OwnerUri, out connInfo);
            if (connInfo != null)
            {
                connInfo.TryGetConnection(Connection.ConnectionType.Default, out dbConn);
                SqlConnection conn = ReliableConnectionHelper.GetAsSqlConnection(dbConn);
                ServerConnection serverConnection = new ServerConnection(conn);

                bool isLocal = false;
                if (string.Compare(GetMachineName(serverConnection.ServerInstance), Environment.MachineName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    isLocal = true;
                }

                foreach (string filePath in args.FilePaths)
                {
                    bool IsFolder = false;
                    bool Existing = IsPathExisting(serverConnection, filePath, ref IsFolder);

                    if (Existing)
                    {
                        if (IsFolder)
                        {
                            errorMessage = string.Format(SR.BackupPathIsFolderError, filePath);
                            result = false;
                            break;
                        }
                    }
                    else
                    {
                        // If the file path doesn't exist, check if the folder exists
                        string folderPath = PathWrapper.GetDirectoryName(filePath);
                        if (isLocal)
                        {
                            if (!string.IsNullOrEmpty(folderPath) && !Directory.Exists(folderPath))
                            {
                                errorMessage = string.Format(SR.InvalidBackupPathError, folderPath);
                                result = false;
                                break;
                            }
                        }
                        else
                        {
                            bool isFolderOnRemote = true;
                            bool existsOnRemote = IsPathExisting(serverConnection, folderPath, ref isFolderOnRemote);
                            if (!existsOnRemote)
                            {
                                errorMessage = string.Format(SR.InvalidBackupPathError, folderPath);
                                result = false;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                result = false;
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                throw new FileBrowserException(errorMessage);
            }

            return result;
        }

        #region private methods

        private static bool IsPathExisting(ServerConnection serverConnection, string path, ref bool IsFolder)
        {
            Enumerator en = null;
            DataSet ds = new DataSet();
            ds.Locale = CultureInfo.InvariantCulture;
            Request req = new Request();

            en = new Enumerator();
            req.Urn = "Server/File[@FullName='" + Urn.EscapeString(path) + "']";
            ds = en.Process(serverConnection, req);
            int iCount = ds.Tables[0].Rows.Count;

            if (iCount > 0)
            {
                IsFolder = !(Convert.ToBoolean(ds.Tables[0].Rows[0]["IsFile"], CultureInfo.InvariantCulture));
                return true;
            }

            IsFolder = false;
            return false;
        }

        private static string GetMachineName(string sqlServerName)
        {
            string machineName = "";
            if (sqlServerName != null)
            {
                // special case (local) which is accepted SQL(MDAC) but by OS
                if ((sqlServerName.ToLowerInvariant().Trim() == localSqlServer) || (sqlServerName.ToLowerInvariant().Trim() == localMachineName))
                {
                    machineName = System.Environment.MachineName;
                }
                else
                {
                    machineName = sqlServerName;
                    if (sqlServerName.Trim().Length != 0)
                    {
                        // [0] = machine, [1] = instance
                        return sqlServerName.Split('\\')[0];
                    }
                }
            }

            return machineName;
        }

        #endregion
    }
}
