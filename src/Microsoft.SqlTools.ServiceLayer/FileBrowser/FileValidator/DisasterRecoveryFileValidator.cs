//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.IO;
using Microsoft.Data.Tools.DataSets;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.FileValidator
{
    /// <summary>
    /// Validate selected file paths for backup/restore operations
    /// </summary>
    public class DisasterRecoveryFileValidator : IFileValidator
    {
        private ServerConnection serverConnection = null;
        private bool isForRestore = false;
        private const string localSqlServer = "(local)";
        private const string localMachineName = ".";

        /// <summary>
        /// Constructor
        /// </summary>
        public DisasterRecoveryFileValidator(SqlConnection sqlConnection, bool isForRestore)
        {
            this.serverConnection = new ServerConnection(sqlConnection);
            this.isForRestore = isForRestore;
        }

        public bool ValidatePaths(string[] filePaths, out string errorMessage)
        {
            bool result = true;
            errorMessage = "";

            bool isLocal = false;
            if (string.Compare(GetMachineName(this.serverConnection.ServerInstance), Environment.MachineName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                isLocal = true;
            }

            foreach (string filePath in filePaths)
            {
                bool IsFolder = false;
                bool Existing = IsPathExisting(filePath, ref IsFolder);

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
                    /* TODO: add logic for restore  */

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
                        bool existsOnRemote = IsPathExisting(folderPath, ref isFolderOnRemote);
                        if (!existsOnRemote)
                        {
                            errorMessage = string.Format(SR.InvalidBackupPathError, folderPath);
                            result = false;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        #region private methods

        private bool IsPathExisting(string path, ref bool IsFolder)
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

        private string GetMachineName(string sqlServerName)
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
