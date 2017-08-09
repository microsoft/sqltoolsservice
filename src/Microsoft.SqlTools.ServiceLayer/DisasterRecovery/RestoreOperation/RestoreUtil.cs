//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Data.Tools.DataSets;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation
{
    public class RestoreUtil
    {
        public RestoreUtil(Server server)
        {
            this.server = server;
            this.excludedDB = new List<string> { "master", "tempdb" };
        }

        /// <summary>
        /// Current sql server instance
        /// </summary>
        private readonly Server server;
        private readonly IList<string> excludedDB;

        public List<string> GetTargetDbNamesForPageRestore()
        {
            List<string> databaseNames = new List<string>();
            foreach (Database db in this.server.Databases)
            {
                if (!this.excludedDB.Contains(db.Name) &&
                     (db.Status == DatabaseStatus.Normal || db.Status == DatabaseStatus.Suspect || db.Status == DatabaseStatus.EmergencyMode) &&
                     db.RecoveryModel == RecoveryModel.Full)
                {
                    databaseNames.Add(db.Name);
                }
            }
            return databaseNames;
        }

        public List<string> GetTargetDbNames()
        {
            List<string> databaseNames = new List<string>();
            foreach (Database db in this.server.Databases)
            {
                if (!this.excludedDB.Contains(db.Name))
                {
                    databaseNames.Add(db.Name);
                }
            }
            return databaseNames;
        }

        internal DateTime GetServerCurrentDateTime()
        {
            DateTime dt = DateTime.MinValue;

            //TODO: the code is moved from ssms and used for restore differential backups
            //Uncomment when restore operation for differential backups is supported
            /*
            string query = "SELECT GETDATE()";
            DataSet dataset = this.server.ExecutionManager.ExecuteWithResults(query);
            if (dataset != null && dataset.Tables.Count > 0 && dataset.Tables[0].Rows.Count > 0)
            {
                dt = Convert.ToDateTime(dataset.Tables[0].Rows[0][0], SmoApplication.DefaultCulture);
            }
            */
            return dt;
        }

       
        /// <summary>
        /// Queries msdb for source database names
        /// </summary>
        public List<String> GetSourceDbNames()
        {
            List<string> databaseNames = new List<string>();
            Request req = new Request();
            req.Urn = "Server/BackupSet";
            req.Fields = new string[1];
            req.Fields[0] = "DatabaseName";
            req.OrderByList = new OrderBy[1];
            req.OrderByList[0] = new OrderBy();
            req.OrderByList[0].Field = "DatabaseName";
            req.OrderByList[0].Dir = OrderBy.Direction.Asc;
            DataTable dt = GetEnumeratorData(req);
            string last = "";
            foreach (DataRow row in dt.Rows)
            {
                string dbName = Convert.ToString(row["DatabaseName"], System.Globalization.CultureInfo.InvariantCulture);
                if (!this.excludedDB.Contains(dbName) && !dbName.Equals(last))
                {
                    bool found = false;
                    foreach (string str in databaseNames)
                    {
                        if (string.Compare(str, dbName, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found == false)
                    {
                        databaseNames.Add(dbName);
                    }
                }
                last = dbName;
            }
            return databaseNames;
        }

        /// <summary>
        /// make enumerator data request
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        internal DataTable GetEnumeratorData(Request req)
        {
            return new Enumerator().Process(this.server.ConnectionContext.SqlConnectionObject, req);
        }

        /// <summary>
        /// Reads backup file header to get source database names
        /// If valid credential name is not provided for URL throws exception while executing T-sql statement
        /// </summary>
        /// <param name="bkdevList">List of backup device items</param>
        /// <param name="credential">Optional Sqlserver credential name to read backup header from URL</param>
        public List<String> GetSourceDbNames(ICollection<BackupDeviceItem> bkdevList, string credential = null)
        {
            List<string> databaseNames = new List<string>();
            foreach (BackupDeviceItem bkdev in bkdevList)
            {
                // use the Restore public API to do the Restore Headeronly query
                Restore res = new Restore();
                res.CredentialName = credential;
                res.Devices.Add(bkdev);
                
                DataTable dt = res.ReadBackupHeader(this.server);
                if (dt != null)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dr != null && !(dr["DatabaseName"] is DBNull))
                        {
                            string dbName = (string)dr["DatabaseName"];
                            bool found = false;
                            foreach (string str in databaseNames)
                            {
                                if (StringComparer.OrdinalIgnoreCase.Compare(str, dbName) == 0)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (found == false)
                            {
                                databaseNames.Add(dbName);
                            }
                        }
                    }
                }
            }
            return databaseNames;
        }

        public string GetNewPhysicalRestoredFileName(string filePathParam, string dbName, bool isNewDatabase, string type, ref int fileIndex)
        {
            if (string.IsNullOrEmpty(filePathParam))
            {
                return string.Empty;
            }

            string result = string.Empty;
            string filePath = filePathParam;
            int idx = filePath.LastIndexOf('\\');
            string folderPath = filePath.Substring(0, idx);

            string fileName = filePath.Substring(idx + 1);
            idx = fileName.LastIndexOf('.');
            string fileExtension = fileName.Substring(idx + 1);

            bool isFolder = true;
            bool isValidPath = IsDestinationPathValid(folderPath, ref isFolder);

            if (!isValidPath || !isFolder)
            {
                if (type != RestoreConstants.Log)
                {
                    folderPath = server.Settings.DefaultFile;
                    if (folderPath.Length == 0)
                    {
                        folderPath = server.Information.MasterDBPath;
                    }
                }
                else
                {
                    folderPath = server.Settings.DefaultLog;
                    if (folderPath.Length == 0)
                    {
                        folderPath = server.Information.MasterDBLogPath;
                    }
                }
            }
            else
            {
                if (!isNewDatabase)
                {
                    return filePathParam;
                }
            }

            if (!isNewDatabase)
            {
                result = folderPath + "\\" + dbName + "." + fileExtension;
            }
            else
            {
                if (0 != string.Compare(fileExtension, "mdf", StringComparison.OrdinalIgnoreCase))
                {
                    result = folderPath + "\\" + dbName + "_" + Convert.ToString(fileIndex, System.Globalization.CultureInfo.InvariantCulture) + "." + fileExtension;
                    fileIndex++;
                }
                else
                {
                    result = folderPath + "\\" + dbName + "." + fileExtension;
                }
            }

            return result;
        }

        public bool IsDestinationPathValid(string path, ref bool isFolder)
        {
            Enumerator en = null;
            DataTable dt;
            Request req = new Request();

            en = new Enumerator();
            req.Urn = "Server/File[@FullName='" + Urn.EscapeString(path) + "']";
            dt = en.Process(this.server.ConnectionContext.SqlConnectionObject, req);

            if (dt.Rows.Count > 0)
            {
                isFolder = !(Convert.ToBoolean(dt.Rows[0]["IsFile"], System.Globalization.CultureInfo.InvariantCulture));
                return true;
            }
            else
            {
                isFolder = false;
                return false;
            }
        }

        /// <summary>
        /// Returns a list of database files
        /// </summary>
        /// <param name="db">SMO database</param>
        /// <returns>a list of database files</returns>
        public List<DbFile> GetDbFiles(Database db)
        {
            List<DbFile> ret = new List<DbFile>();
            if (db == null)
            {
                return ret;
            }
            char fileType = '\0';
            foreach (FileGroup fg in db.FileGroups)
            {
                if ((fg.FileGroupType == FileGroupType.FileStreamDataFileGroup) || (fg.FileGroupType == FileGroupType.MemoryOptimizedDataFileGroup))
                {
                    fileType = DbFile.FileStreamFileType;
                }
                else
                {
                    fileType = DbFile.RowFileType;
                }
                foreach (DataFile f in fg.Files)
                {
                    DbFile dbFile = new DbFile(f.Name, fileType, f.FileName);
                    ret.Add(dbFile);
                }
            }
            foreach (LogFile f in db.LogFiles)
            {
                DbFile dbFile = new DbFile(f.Name, DbFile.LogFileType, f.FileName);
                ret.Add(dbFile);
            }
            return ret;
        }

        //TODO: the code is moved from ssms and used for other typs of restore operation
        //Uncomment when restore operation for those types are supported
        /*
        public List<DbFile> GetDbFiles(BackupSet bkSet)
        {
            List<DbFile> ret = new List<DbFile>();
            if (bkSet == null || bkSet.BackupMediaSet == null || bkSet.BackupMediaSet.BackupMediaList.Count() < 1)
            {
                return ret;
            }
            DataSet dataset = bkSet.FileList;
            if (dataset != null && dataset.Tables.Count > 0)
            {
                string logicalName = null;
                string physicalName = null;
                char type = '\0';
                foreach (DataRow dr in dataset.Tables[0].Rows)
                {
                    if (!(dr["LogicalName"] is DBNull))
                    {
                        logicalName = (string)dr["LogicalName"];
                    }
                    if (!(dr["PhysicalName"] is DBNull))
                    {
                        physicalName = (string)dr["PhysicalName"];
                    }
                    if (!(dr["Type"] is DBNull))
                    {
                        // The data type of Type in a list obtained from RESTORE FILELISTONLY is char(1).
                        string temp = (string)dr["Type"];
                        if (!String.IsNullOrEmpty(temp))
                        {
                            type = temp[0];
                        }
                    }
                    if (!String.IsNullOrEmpty(logicalName) && !String.IsNullOrEmpty(physicalName) && (type != '\0'))
                    {
                        DbFile dbFile = new DbFile(logicalName, type, physicalName);
                        ret.Add(dbFile);
                    }
                }
            }
            return ret;
        }
        */

        /// <summary>
        /// Returns a list of database files in all the backup devices in the Restore object
        /// </summary>
        public List<DbFile> GetDbFiles(Restore restore)
        {
            List<DbFile> ret = new List<DbFile>();
            if (restore == null || restore.Devices == null || restore.Devices.Count < 1)
            {
                return ret;
            }
            // Using the Restore public API to do the Restore FilelistOnly
            Restore res = new Restore();
            res.CredentialName = restore.CredentialName;
            res.Devices.Add(restore.Devices[0]);
            res.FileNumber = restore.FileNumber;
            DataTable datatable = res.ReadFileList(this.server);
            if (datatable != null && datatable.Rows.Count > 0)
            {
                string logicalName = null;
                string physicalName = null;
                char type = '\0';
                foreach (DataRow dr in datatable.Rows)
                {
                    if (!(dr["LogicalName"] is DBNull))
                    {
                        logicalName = (string)dr["LogicalName"];
                    }
                    if (!(dr["PhysicalName"] is DBNull))
                    {
                        physicalName = (string)dr["PhysicalName"];
                    }
                    if (!(dr["Type"] is DBNull))
                    {
                        // The data type of Type in a list obtained from RESTORE FILELISTONLY is char(1).
                        string temp = (string)dr["Type"];
                        if (!String.IsNullOrEmpty(temp))
                        {
                            type = temp[0];
                        }
                    }
                    if (!String.IsNullOrEmpty(logicalName) && !String.IsNullOrEmpty(physicalName) && (type != '\0'))
                    {
                        DbFile dbFile = new DbFile(logicalName, type, physicalName);
                        ret.Add(dbFile);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Set credential name in the restore objects which have a backup set in Microsoft Azure
        /// From sql16, default credential is SAS credential so no explict credential needed for restore object. 
        /// </summary>
        /// <param name="restorePlan">Restore plan created for the restore operation</param>
        /// <param name="credentialName">Sql server credential name</param>
        public void AddCredentialNameForUrlBackupSet(RestorePlan restorePlan, string credentialName)
        {
            if (string.IsNullOrEmpty(credentialName) || restorePlan == null || restorePlan.RestoreOperations == null)
            {
                return;
            }
            if (restorePlan.Server.VersionMajor >= 13) // for sql16, default backup/restore URL will use SAS
            {
                return;
            }
            // If any of the backup media in the restore object is in URL, we assign the credential name to the CredentialName property of the Restore object
            foreach (Restore res in restorePlan.RestoreOperations)
            {
                
                if (res.BackupSet != null && res.BackupSet.BackupMediaSet != null && res.BackupSet.BackupMediaSet.BackupMediaList != null)
                {
                    foreach (BackupMedia bkMedia in res.BackupSet.BackupMediaSet.BackupMediaList)
                    {
                        if (bkMedia != null && bkMedia.MediaType == DeviceType.Url)
                        {
                            res.CredentialName = credentialName;
                            break;
                        }
                    }
                }
                
                if (res.Devices != null)
                {
                    foreach (BackupDeviceItem bkDevice in res.Devices)
                    {
                        if (bkDevice.DeviceType == DeviceType.Url)
                        {
                            res.CredentialName = credentialName;
                            break;
                        }
                    }
                }
            }
            // If the backup file to which the tail log is going to be backed up is a file in Microsoft Azure, 
            // we assign the credential name to the Credential Name property of the Backup object
            if (restorePlan.TailLogBackupOperation != null && restorePlan.TailLogBackupOperation.Devices != null)
            {
                foreach (BackupDeviceItem bkdevItem in restorePlan.TailLogBackupOperation.Devices)
                {
                    if (bkdevItem != null && bkdevItem.DeviceType == DeviceType.Url)
                    {
                        restorePlan.TailLogBackupOperation.CredentialName = credentialName;
                        break;
                    }
                }
            }
        }

        internal string GetDefaultDataFileFolder()
        {
            string ret = this.server.Settings.DefaultFile;
            if (string.IsNullOrEmpty(ret))
            {
                ret = this.server.Information.MasterDBPath;
            }

            ret = ret.TrimEnd(server.PathSeparator[0]);
            return ret;
        }

        internal string GetDefaultLogFileFolder()
        {
            string ret = this.server.Settings.DefaultLog;
            if (string.IsNullOrEmpty(ret))
            {
                ret = this.server.Information.MasterDBLogPath;
            }

            ret = ret.TrimEnd(server.PathSeparator[0]);
            return ret;
        }

        internal string GetDefaultBackupFolder()
        {
            string ret = this.server.Settings.BackupDirectory;
            ret = ret.TrimEnd(server.PathSeparator[0]);
            return ret;
        }

        internal string GetDefaultTailLogbackupFile(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return string.Empty;
            }
            var folderpath = GetDefaultBackupFolder();
            var filename = SanitizeFileName(databaseName) + "_LogBackup_" + GetServerCurrentDateTime().ToString("yyyy-MM-dd_HH-mm-ss") + ".bak";
            return PathWrapper.Combine(folderpath, filename);
        }


        /// <summary>
        /// Returns a default location for tail log backup
        /// If the first backup media is from Microsoft Azure, a Microsoft Azure url for the Tail log backup file is returned
        /// </summary>
        internal string GetDefaultTailLogbackupFile(string databaseName, RestorePlan restorePlan)
        {
            if (string.IsNullOrEmpty(databaseName) || restorePlan == null)
            {
                return string.Empty;
            }
            if (restorePlan.TailLogBackupOperation != null && restorePlan.TailLogBackupOperation.Devices != null)
            {
                restorePlan.TailLogBackupOperation.Devices.Clear();
            }
            string folderpath = string.Empty;
            BackupMedia firstBackupMedia = this.GetFirstBackupMedia(restorePlan);
            string filename = this.SanitizeFileName(databaseName) + "_LogBackup_" + this.GetServerCurrentDateTime().ToString("yyyy-MM-dd_HH-mm-ss") + ".bak";
            if (firstBackupMedia != null && firstBackupMedia.MediaType == DeviceType.Url)
            {
                // the uri will use the same container as the container of the first backup media
                Uri uri;
                if (Uri.TryCreate(firstBackupMedia.MediaName, UriKind.Absolute, out uri))
                {
                    UriBuilder uriBuilder = new UriBuilder();
                    uriBuilder.Scheme = uri.Scheme;
                    uriBuilder.Host = uri.Host;
                    if (uri.AbsolutePath.Length > 0)
                    {
                        string[] parts = uri.AbsolutePath.Split('/');
                        string newPath = string.Join("/", parts, 0, parts.Length - 1);
                        if (newPath.EndsWith("/"))
                        {
                            newPath = newPath.Substring(0, newPath.Length - 1);
                        }
                        uriBuilder.Host = uriBuilder.Host + newPath;
                    }
                    uriBuilder.Path = filename;
                    string urlFilename = uriBuilder.Uri.AbsoluteUri;
                    if (restorePlan.TailLogBackupOperation != null && restorePlan.TailLogBackupOperation.Devices != null)
                    {
                        restorePlan.TailLogBackupOperation.Devices.Add(new BackupDeviceItem(urlFilename, DeviceType.Url));
                    }
                    return urlFilename;
                }
            }
            folderpath = this.GetDefaultBackupFolder();
            if (restorePlan.TailLogBackupOperation != null && restorePlan.TailLogBackupOperation.Devices != null)
            {
                restorePlan.TailLogBackupOperation.Devices.Add(new BackupDeviceItem(PathWrapper.Combine(folderpath, filename), DeviceType.File));
            }
            return PathWrapper.Combine(folderpath, filename);
        }

        internal string GetDefaultStandbyFile(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return string.Empty;
            }
            var folderpath = GetDefaultBackupFolder();
            var filename = SanitizeFileName(databaseName) + "_RollbackUndo_" + GetServerCurrentDateTime().ToString("yyyy-MM-dd_HH-mm-ss") + ".bak";
            return PathWrapper.Combine(folderpath, filename);
        }

        //TODO: the code is moved from ssms and used for other typs of restore operation
        //Uncomment when restore operation for those types are supported
        /*
        internal DateTime GetLastBackupDate(DatabaseRestorePlanner planner)
        {
            BackupSetCollection bkSetColl = planner.BackupSets;
            if (bkSetColl.backupsetList.Count > 0)
            {
                return bkSetColl.backupsetList[bkSetColl.backupsetList.Count - 1].BackupStartDate;
            }
            return DateTime.MinValue;
        }
        */

        /// <summary>
        /// Sanitizes the name of the file.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        internal string SanitizeFileName(string name)
        {
            char[] result = name.ToCharArray();
            string illegalCharacters = "\\/:*?\"<>|";

            int resultLength = result.GetLength(0);
            int illegalLength = illegalCharacters.Length;

            for (int resultIndex = 0; resultIndex < resultLength; resultIndex++)
            {
                for (int illegalIndex = 0; illegalIndex < illegalLength; illegalIndex++)
                {
                    if (result[resultIndex] == illegalCharacters[illegalIndex])
                    {
                        result[resultIndex] = '_';
                    }
                }
            }
            return new string(result);
        }

        //TODO: the code is moved from ssms and used for other typs of restore operation
        //Uncomment when restore operation for those types are supported
        /*
        internal void MarkDuplicateSuspectPages(List<SuspectPageTaskDataObject> suspectPageObjList)
        {
            List<SuspectPageTaskDataObject> newList = new List<SuspectPageTaskDataObject>(suspectPageObjList);
            newList.Sort();
            newList[0].IsDuplicate = false;
            for (int i = 1; i < newList.Count; i++)
            {
                if (newList[i].CompareTo(newList[i - 1]) == 0)
                {
                    newList[i].IsDuplicate = true;
                    newList[i - 1].IsDuplicate = true;
                }
                else
                {
                    newList[i].IsDuplicate = false;
                }
            }
        }
        */

        /*
    internal void VerifyChecksumWorker(RestorePlan plan, IBackgroundOperationContext backgroundContext, EventHandler cancelEventHandler)
    {
        if (plan == null || plan.RestoreOperations.Count() == 0)
        {
            return;
        }
        backgroundContext.IsCancelable = true;
        backgroundContext.CancelRequested += cancelEventHandler;
        try
        {
            foreach (Restore res in plan.RestoreOperations)
            {
                if (!backgroundContext.IsCancelRequested && res.backupSet != null)
                {
                    StringBuilder bkMediaNames = new StringBuilder();
                    foreach (BackupDeviceItem item in res.Devices)
                    {
                        backgroundContext.Status = SR.Verifying + ":" + item.Name;
                        try
                        {
                            // Use the Restore public API to do the Restore VerifyOnly query
                            Restore restore = new Restore();
                            restore.CredentialName = res.CredentialName;
                            restore.Devices.Add(item);
                            if (!res.SqlVerify(this.server))
                            {
                                throw new Exception(SR.BackupDeviceItemVerificationFailed(item.Name));
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(SR.BackupDeviceItemVerificationFailed(item.Name), ex);
                        }
                    }
                }
            }
        }
        finally
        {
            backgroundContext.CancelRequested -= cancelEventHandler;
        }
    }
    */

        private BackupMedia GetFirstBackupMedia(RestorePlan restorePlan)
        {
            /*
            if (restorePlan == null || restorePlan.RestoreOperations == null || restorePlan.RestoreOperations.Count == 0)
            {
                return null;
            }
            Restore res = restorePlan.RestoreOperations[0];
            if (res == null || res.backupSet == null || res.backupSet.backupMediaSet == null || res.backupSet.backupMediaSet.BackupMediaList == null || res.backupSet.backupMediaSet.BackupMediaList.ToList().Count == 0)
            {
                return null;
            }
            return res.backupSet.backupMediaSet.BackupMediaList.ToList()[0];
            */
            return null;
        }
    }

    /// <summary>
    /// A class representing a database file
    /// </summary>
    public class DbFile
    {
        public DbFile(string logicalName, char type, string physicalName)
        {
            this.logicalName = logicalName;
            this.physicalName = physicalName;
            if (type != '\0')
            {
                this.dbFileType = type;
            }
            this.PhysicalNameRelocate = physicalName;
        }

        // Database file types
        // When restoring backup, the engine returns the following file type values.
        public const char RowFileType = 'D';
        public const char LogFileType = 'L';
        public const char FullTextCatalogFileType = 'F';
        public const char FileStreamFileType = 'S';

        private string logicalName;
        public string LogicalName
        {
            get { return logicalName; }
        }

        private string physicalName;
        public string PhysicalName
        {
            get { return physicalName; }
        }

        internal char dbFileType;

        /// <summary>
        /// Returns the database file type string to be displayed in the dialog
        /// </summary>                
        public string DbFileType
        {
            get
            {
                string value = string.Empty;
                switch (dbFileType)
                {
                    case DbFile.RowFileType:
                        value = "RowData";//TODO SR.RowData;
                        break;
                    case DbFile.LogFileType:
                        value = "Log";// SR.Log;
                        break;
                    case DbFile.FileStreamFileType:
                        value = "FileStream";// SR.FileStream;
                        break;
                    case DbFile.FullTextCatalogFileType:
                        value = "FullTextCatlog";// SR.FullTextCatlog;
                        break;
                }
                return value;
            }
        }

        public string PhysicalNameRelocate;
    }
}
