//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.Data.Tools.DataSets;
using Microsoft.SqlServer.Management.Smo;
using System.IO;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser
{
    public enum ServiceTypes
    {
        Backup,
        Restore,
    }

    public interface IFileValidator
    {
        bool ValidatePaths(string[] filePaths, out string errorMessage);
    }

    public class BackupFileValidator: IFileValidator
    {
        private ServerConnection serverConnection = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public BackupFileValidator(ServerConnection serverConnection)
        {
            this.serverConnection = serverConnection;
        }

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

        private const string constLocalSqlServer = "(local)";
        private const string constLocalMachineName = ".";
        private string GetMachineName(string sqlServerName)
        {
            string machineName = "";
            if (sqlServerName != null)
            {
                // special case (local) which is accepted SQL(MDAC) but by OS
                if ((sqlServerName.ToLowerInvariant().Trim() == constLocalSqlServer) || (sqlServerName.ToLowerInvariant().Trim() == constLocalMachineName))
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

        public bool ValidatePaths(string[] filePaths, out string errorMessage)
        {
            bool result = true;
            errorMessage = "";

            bool isLocal = false;
            if (string.Compare(GetMachineName(this.serverConnection.ServerInstance), Environment.MachineName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                isLocal = true;
            }

            foreach (string path in filePaths)
            {
                bool IsFolder = false;
                bool Existing = IsPathExisting(path, ref IsFolder);

                if (Existing)
                {
                    if (IsFolder)
                    {
                        // SRError.ErrorBackupPathIsFolder
                        result = false;
                        break;
                    }
                }
                else
                {
                    /* TODO: add logic for restore  */

                    // If the file path doesn't exist, check if the folder exists
                    string folderPath = PathWrapper.GetDirectoryName(path);
                    if (isLocal)
                    {
                        if (!string.IsNullOrEmpty(folderPath) && !Directory.Exists(folderPath))
                        {
                            //SRError.ErrorBackupInvalidPath
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
                            //SRError.ErrorBackupInvalidPath
                            result = false;
                            break;
                        }
                    }
                }
            }

            return result;

        }
    }
}
