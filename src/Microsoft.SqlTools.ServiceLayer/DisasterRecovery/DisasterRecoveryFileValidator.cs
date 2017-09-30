//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.FileBrowser;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    /// <summary>
    /// Validate selected file paths for backup/restore operations
    /// </summary>
    public static class DisasterRecoveryFileValidator
    {
        internal const string LocalSqlServer = "(local)";
        internal const string LocalMachineName = ".";

        public static bool ValidatePaths(FileBrowserValidateEventArgs args, out string errorMessage)
        {
            errorMessage = string.Empty;
            bool result = true;
            SqlConnection connection = null;

            if (args != null)
            {
                ConnectionInfo connInfo;
                ConnectionService.Instance.TryFindConnection(args.OwnerUri, out connInfo);
                if (connInfo != null)
                {
                    DbConnection dbConnection = null;
                    connInfo.TryGetConnection(Connection.ConnectionType.Default, out dbConnection);
                    if (dbConnection != null)
                    {
                        connection = ReliableConnectionHelper.GetAsSqlConnection(dbConnection);
                    }
                }

                if (connection != null)
                {
                    bool isLocal = false;
                    if (string.Compare(GetMachineName(connection.DataSource), Environment.MachineName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        isLocal = true;
                    }

                    foreach (string filePath in args.FilePaths)
                    {
                        bool isFolder;
                        bool existing = IsPathExisting(connection, filePath, out isFolder);

                        if (existing)
                        {
                            if (isFolder)
                            {
                                errorMessage = SR.BackupPathIsFolderError;
                                break;
                            }
                        }
                        else
                        {
                            if (args.ServiceType == FileValidationServiceConstants.Backup)
                            {
                                // If the file path doesn't exist, check if the folder exists
                                string folderPath = PathWrapper.GetDirectoryName(filePath);
                                if (isLocal)
                                {
                                    if (!string.IsNullOrEmpty(folderPath) && !Directory.Exists(folderPath))
                                    {
                                        errorMessage = SR.InvalidBackupPathError;
                                        break;
                                    }
                                }
                                else
                                {
                                    bool isFolderOnRemote;
                                    bool existsOnRemote = IsPathExisting(connection, folderPath, out isFolderOnRemote);
                                    if (!existsOnRemote)
                                    {
                                        errorMessage = SR.InvalidBackupPathError;
                                        break;
                                    }
                                }
                            }
                            else if (args.ServiceType == FileValidationServiceConstants.Restore)
                            {
                                errorMessage = SR.InvalidBackupPathError;
                                break;
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                result = false;
            }

            return result;
        }

        #region private methods

        internal static bool IsPathExisting(SqlConnection connection, string path, out bool isFolder)
        {
            Request req = new Request
            {
                Urn = "Server/File[@FullName='" + Urn.EscapeString(path) + "']",
                Fields = new[] { "IsFile" }
            };

            Enumerator en = new Enumerator();
            bool isExisting = false;
            isFolder = false;

            using (DataSet ds = en.Process(connection, req))
            {
                if (FileBrowserBase.IsValidDataSet(ds))
                {
                    isFolder = !(Convert.ToBoolean(ds.Tables[0].Rows[0]["IsFile"], CultureInfo.InvariantCulture));
                    isExisting = true;
                }
            }

            return isExisting;
        }

        internal static string GetMachineName(string sqlServerName)
        {
            string machineName = string.Empty;
            if (sqlServerName != null)
            {
                string serverName = sqlServerName.ToLowerInvariant().Trim();
                if ((serverName == LocalSqlServer) || (serverName == LocalMachineName))
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
