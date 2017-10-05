//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.Connection;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation
{
    public interface IRestoreDatabaseTaskDataObject
    {
        string DataFilesFolder { get; set; }
        string DefaultDataFileFolder { get; }
        bool RelocateAllFiles { get; set; }
        string LogFilesFolder { get; set; }
        string DefaultLogFileFolder { get; }
        List<DbFile> DbFiles { get; }
        string DefaultBackupFolder { get; }

        RestoreOptions RestoreOptions { get; }

        string DefaultStandbyFile { get; }
        bool IsTailLogBackupPossible { get; }
        bool IsTailLogBackupWithNoRecoveryPossible { get; }

        bool TailLogWithNoRecovery { get; set; }

        string TailLogBackupFile { get; set; }

        string DefaultTailLogbackupFile { get; }

        RestorePlan RestorePlan { get; }

        bool CloseExistingConnections { get; set; }

        RestoreParams RestoreParams { get; set; }

        bool BackupTailLog { get; set; }

        string DefaultSourceDbName { get; }

        string SourceDatabaseName { get; set; }

        List<String> SourceDbNames { get; }

        bool CanChangeTargetDatabase { get; }

        string DefaultTargetDbName { get; }
        string TargetDatabaseName { get; set; }

        bool CanDropExistingConnections { get; }


    }
    /// <summary>
    /// Includes the plan with all the data required to do a restore operation on server
    /// </summary>
    public class RestoreDatabaseTaskDataObject : SmoScriptableOperationWithFullDbAccess, IRestoreDatabaseTaskDataObject
    {

        private const char BackupMediaNameSeparator = ',';
        private DatabaseRestorePlanner restorePlanner;
        private string tailLogBackupFile;
        private BackupSetsFilterInfo backupSetsFilterInfo = new BackupSetsFilterInfo();
        private bool? isTailLogBackupPossible = false;
        private bool? isTailLogBackupWithNoRecoveryPossible = false;
        private string backupMediaList = string.Empty;
        private Server server;

        public RestoreDatabaseTaskDataObject(Server server, String databaseName)
        {
            PlanUpdateRequired = true;
            this.server = server;
            this.Util = new RestoreUtil(server);
            restorePlanner = new DatabaseRestorePlanner(server);

            if (String.IsNullOrEmpty(databaseName))
            {
                this.restorePlanner = new DatabaseRestorePlanner(server);
            }
            else
            {
                this.restorePlanner = new DatabaseRestorePlanner(server, databaseName);
                this.targetDbName = databaseName;
            }

            this.restorePlanner.TailLogBackupFile = this.Util.GetDefaultTailLogbackupFile(databaseName);
            this.restoreOptions = new RestoreOptions();
            //the server will send events in intervals of 5 percent
            this.restoreOptions.PercentCompleteNotification = 5;
        }

        /// <summary>
        /// Boolean indicating whether the relocate all files checkbox was checked
        /// </summary>
        public bool RelocateAllFiles { get; set; }
        
        /// <summary>
        /// Restore session id
        /// </summary>
        public string SessionId { get; set; }

        public string TargetDatabaseName
        {
            get
            {
                return string.IsNullOrEmpty(targetDbName) ? DefaultSourceDbName : targetDbName;
            }
            set
            {
                this.targetDbName = value;
            }
        }

        public bool IsValid
        {
            get
            {
                return this.Server != null && this.RestorePlanner != null && ActiveException == null;
            }
        }

        public RestoreParams RestoreParams { get; set; }

        /// <summary>
        /// Database names includes in the restore plan
        /// </summary>
        /// <returns></returns>
        public List<String> SourceDbNames
        {
            get
            {
                if (RestorePlanner.ReadHeaderFromMedia)
                {
                    return Util.GetSourceDbNames(this.restorePlanner.BackupMediaList, this.CredentialName);
                }
                else
                {
                    return Util.GetSourceDbNames();
                }
            }
        }

        /// <summary>
        /// Returns the db names that have backupsets
        /// </summary>
        /// <returns></returns>
        public List<String> GetDatabaseNamesWithBackupSets()
        {
            return Util.GetSourceDbNames();
        }

        public bool CanChangeTargetDatabase
        {
            get
            {
                return DatabaseUtils.IsSystemDatabaseConnection(Server.ConnectionContext.DatabaseName);
            }
        }

        public string DefaultTargetDbName
        {
            get
            {
                return Server.ConnectionContext.DatabaseName;
            }
        }


        /// <summary>
        /// Current sqlserver instance
        /// </summary>
        public override Server Server
        {
            get
            {
                return server;
            }
        }

        /// <summary>
        /// Recent exception that was thrown
        /// Displayed at the top of the dialog
        /// </summary>
        public Exception ActiveException { get; set; }

        public Exception CreateOrUpdateRestorePlanException { get; set; }

        /// <summary>
        /// Add a backup file to restore plan media list
        /// </summary>
        /// <param name="filePaths"></param>
        public void AddFiles(string filePaths)
        {
            backupMediaList = filePaths;
            PlanUpdateRequired = true;
            if (!string.IsNullOrWhiteSpace(filePaths))
            {
                string[] files = filePaths.Split(BackupMediaNameSeparator);
                files = files.Select(x => x.Trim()).ToArray();
                foreach (var file in files)
                {
                    if (!this.RestorePlanner.BackupMediaList.Any(x => x.Name == file))
                    {
                        this.RestorePlanner.BackupMediaList.Add(new BackupDeviceItem
                        {
                            DeviceType = DeviceType.File,
                            Name = file
                        });
                    }
                }

                var itemsToRemove = this.RestorePlanner.BackupMediaList.Where(x => !files.Contains(x.Name)).ToList();
                foreach (var item in itemsToRemove)
                {
                    this.RestorePlanner.BackupMediaList.Remove(item);
                }
            }
        }

        
        /// <summary>
        /// Returns the last backup taken
        /// </summary>
        /// <returns></returns>
        public string GetLastBackupTaken()
        {
            string lastBackup = string.Empty;
            int lastSelectedIndex = GetLastSelectedBackupSetIndex();
            BackupSet lastSelectedBackupSet = lastSelectedIndex >= 0 && this.RestorePlan.RestoreOperations != null && this.RestorePlan.RestoreOperations.Count > 0 ? 
                this.RestorePlan.RestoreOperations[lastSelectedIndex].BackupSet : null;
            if (this.RestorePlanner.RestoreToLastBackup &&
                lastSelectedBackupSet != null &&
                this.RestorePlan.RestoreOperations.Count > 0 &&
                lastSelectedBackupSet != null)
            {
                bool isTheLastOneSelected = lastSelectedIndex == this.RestorePlan.RestoreOperations.Count - 1;
                DateTime backupTime = lastSelectedBackupSet.BackupStartDate;
                string backupTimeStr = backupTime.ToLongDateString() + " " + backupTime.ToLongTimeString();
                lastBackup = isTheLastOneSelected ?
                    string.Format(CultureInfo.CurrentCulture, SR.TheLastBackupTaken, (backupTimeStr)) : backupTimeStr;
            }
            else if (GetFirstSelectedBackupSetIndex() == 0 && !this.RestorePlanner.RestoreToLastBackup)
            {
                lastBackup = this.CurrentRestorePointInTime.Value.ToLongDateString() +
                        " " + this.CurrentRestorePointInTime.Value.ToLongTimeString();
            }
            return lastBackup;

        }

        public override void Execute(TaskExecutionMode mode)
        {
            UpdateRestoreTaskObject();

            base.Execute(mode);
        }

        public ConnectionInfo ConnectionInfo { get; set; }

        public override string ServerName
        {
            get
            {
                if (this.ConnectionInfo != null)
                {
                    return this.ConnectionInfo.ConnectionDetails.ServerName;
                }

                return this.Server.Name;
            }
        }

        public override string DatabaseName
        {
            get
            {
                return TargetDatabaseName;
            }
        }

        /// <summary>
        /// Executes the restore operations
        /// </summary>
        public override void Execute()
        {
            try
            {
                if (IsValid && RestorePlan.RestoreOperations != null && RestorePlan.RestoreOperations.Any())
                {
                    // Restore Plan should be already created and updated at this point

                    RestorePlan restorePlan = GetRestorePlanForExecutionAndScript();

                    if (restorePlan != null && restorePlan.RestoreOperations.Count > 0)
                    {
                        restorePlan.PercentComplete += (object sender, PercentCompleteEventArgs e) =>
                        {
                            OnMessageAdded(new TaskMessage { Description = $"{e.Percent}%", Status = SqlTaskStatus.InProgress });
                        };
                        restorePlan.Execute();
                    }
                }
                else
                {
                    throw new InvalidOperationException(SR.RestoreNotSupported);
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (this.Server.ConnectionContext.IsOpen)
                {
                    this.Server.ConnectionContext.Disconnect();
                }
            }
        }

        /// <summary>
        /// Gets RestorePlan to perform restore and to script
        /// </summary>
        public RestorePlan GetRestorePlanForExecutionAndScript()
        {
            // One of the reasons the existing plan is not used and a new plan is created to execute is the order of the backupsets and
            // the last backupset is important. User can choose specific backupset in the plan to restore and
            // if the last backup set is not selected from the list, it means the item before that should be the real last item
            // with simply removing the item from the list, the order won't work correctlly. Smo considered the last item removed and 
            // doesn't consider the previous item as the last item. Creating a new list and adding backpsets is the only way to make it work
            // and the reason the last backup set is important is that it has to restore with recovery mode while the others are restored with no recovery

            this.ActiveException = null; //Clear any existing exceptions as the plan is getting recreated. 
            //Clear any existing exceptions as new plan is getting recreated.
            this.CreateOrUpdateRestorePlanException = null;
            bool tailLogBackup = this.RestorePlanner.BackupTailLog;
            if (this.PlanUpdateRequired)
            {
                this.RestorePlan = this.RestorePlanner.CreateRestorePlan(this.RestoreOptions);
                
                this.Util.AddCredentialNameForUrlBackupSet(this.RestorePlan, this.CredentialName);
            }
            RestorePlan rp = new RestorePlan(this.Server);
            rp.RestoreAction = RestoreActionType.Database;
            if (this.RestorePlan != null)
            {
                if (this.RestorePlan.TailLogBackupOperation != null && tailLogBackup)
                {
                    rp.TailLogBackupOperation = this.RestorePlan.TailLogBackupOperation;
                }
                int i = 0;
                foreach (Restore res in this.RestorePlan.RestoreOperations)
                {
                    if (this.backupSetsFilterInfo.IsBackupSetSelected(res.BackupSet))
                    {
                        rp.RestoreOperations.Add(res);
                    }
                    i++;
                }
            }
            this.SetRestorePlanProperties(rp);
            RestorePlanToExecute = rp;
            return rp;
        }

        /// <summary>
        /// For test purpose only. The restore plan that's used to execute
        /// </summary>
        internal RestorePlan RestorePlanToExecute { get; set; }

        /// <summary>
        /// Restore Util
        /// </summary>
        public RestoreUtil Util { get; set; }


        /// <summary>
        /// SMO database restore planner used to create a restore plan
        /// </summary>
        public DatabaseRestorePlanner RestorePlanner
        {
            get { return restorePlanner; }
        }

        public bool PlanUpdateRequired { get; private set; }

        /// <summary>
        /// File to backup tail log before doing the restore
        /// </summary>
        public string TailLogBackupFile
        {
            get { return tailLogBackupFile; }
            set
            {
                if (tailLogBackupFile == null || !tailLogBackupFile.Equals(value))
                {
                    this.RestorePlanner.TailLogBackupFile = value;
                    this.PlanUpdateRequired = true;
                    this.tailLogBackupFile = value;
                }
            }
        }

        private RestoreOptions restoreOptions;

        public RestoreOptions RestoreOptions
        {
            get { return restoreOptions; }
        }

        private string dataFilesFolder = string.Empty;

        /// <summary>
        /// Folder for all data files when relocate all files option is used
        /// </summary>
        public string DataFilesFolder
        {
            get
            {
                if (string.IsNullOrEmpty(this.dataFilesFolder))
                {
                    this.dataFilesFolder = this.DefaultDataFileFolder;
                }
                return this.dataFilesFolder;
            }
            set
            {
                if (this.dataFilesFolder == null || !this.dataFilesFolder.Equals(value))
                {
                    try
                    {
                        Uri pathUri;
                        bool fUriCreated = Uri.TryCreate(value, UriKind.Absolute, out pathUri);

                        if (fUriCreated && pathUri.Scheme == "https")
                        {
                            this.dataFilesFolder = value;
                        }
                        else
                        {
                            this.dataFilesFolder = PathWrapper.GetDirectoryName(value + PathWrapper.PathSeparatorFromServerConnection(Server.ConnectionContext));
                        }
                        if (string.IsNullOrEmpty(this.dataFilesFolder))
                        {
                            this.dataFilesFolder = this.Server.DefaultFile;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.ActiveException = ex;
                    }

                    this.UpdateDbFiles();
                }
            }
        }

        private string logFilesFolder = string.Empty;

        /// <summary>
        /// Folder for all log files when relocate all files option is used
        /// </summary>
        public string LogFilesFolder
        {
            get
            {
                if (string.IsNullOrEmpty(this.logFilesFolder))
                {
                    this.logFilesFolder = this.DefaultLogFileFolder;
                }
                return this.logFilesFolder;

            }
            set
            {
                if (this.logFilesFolder == null || !this.logFilesFolder.Equals(value))
                {
                    try
                    {
                        Uri pathUri;
                        bool fUriCreated = Uri.TryCreate(value, UriKind.Absolute, out pathUri);

                        if (fUriCreated && pathUri.Scheme == "https")
                        {
                            this.logFilesFolder = value;
                        }
                        else
                        {
                            this.logFilesFolder = PathWrapper.GetDirectoryName(value + PathWrapper.PathSeparatorFromServerConnection(Server.ConnectionContext));
                        }
                        if (string.IsNullOrEmpty(this.logFilesFolder))
                        {
                            this.logFilesFolder = Server.DefaultLog;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.ActiveException = ex;
                    }
                    this.UpdateDbFiles();
                }
            }
        }

        /// <summary>
        /// Determines whether [is tail log backup possible].
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if [is tail log backup possible]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsTailLogBackupPossible
        {
            get
            {
                if (!isTailLogBackupPossible.HasValue)
                {
                    if (this.Server.Version.Major < 9 || String.IsNullOrEmpty(this.restorePlanner.DatabaseName))
                    {
                        isTailLogBackupPossible = false;
                    }
                    else
                    {
                        Database db = this.Server.Databases[this.RestorePlanner.DatabaseName];
                        if (db == null)
                        {
                            isTailLogBackupPossible = false;
                        }
                        else
                        {
                            db.Refresh();
                            if (db.Status != DatabaseStatus.Normal && db.Status != DatabaseStatus.Suspect && db.Status != DatabaseStatus.EmergencyMode)
                            {
                                isTailLogBackupPossible = false;
                            }
                            else if (db.RecoveryModel == RecoveryModel.Full || db.RecoveryModel == RecoveryModel.BulkLogged)
                            {
                                isTailLogBackupPossible = true;
                            }
                            else
                            {
                                isTailLogBackupPossible = false;
                            }
                        }
                    }
                }

                return isTailLogBackupPossible.Value;
            }
        }

        /// <summary>
        /// Determines whether [is tail log backup with NORECOVERY possible].
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if [is tail log backup with NORECOVERY possible]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsTailLogBackupWithNoRecoveryPossible
        {
            get
            {
                if (!isTailLogBackupWithNoRecoveryPossible.HasValue)
                {
                    string databaseName = this.RestorePlanner.DatabaseName;
                    if (!IsTailLogBackupPossible)
                    {
                        isTailLogBackupWithNoRecoveryPossible = false;
                    }
                    else
                    {
                        Database db = this.Server.Databases[databaseName];
                        if (db == null)
                        {
                            isTailLogBackupWithNoRecoveryPossible = false;
                        }
                        else if (Server.Version.Major > 10 && db.DatabaseEngineType == DatabaseEngineType.Standalone && !String.IsNullOrEmpty(db.AvailabilityGroupName))
                        {
                            isTailLogBackupWithNoRecoveryPossible = false;
                        }
                        else if (db.DatabaseEngineType == DatabaseEngineType.Standalone && db.IsMirroringEnabled)
                        {
                            isTailLogBackupWithNoRecoveryPossible = false;
                        }
                        else
                        {
                            isTailLogBackupWithNoRecoveryPossible = true;
                        }
                    }
                }
                return isTailLogBackupWithNoRecoveryPossible.Value;
            }
        }

        public string DefaultStandbyFile
        {
            get
            {
                return Util.GetDefaultStandbyFile(this.RestorePlan != null ? this.RestorePlan.DatabaseName : string.Empty);
            }
        }

        public string DefaultTailLogbackupFile
        {
            get
            {
                return Util.GetDefaultTailLogbackupFile(this.RestorePlan != null ? this.RestorePlan.DatabaseName : string.Empty);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [prompt before each backup].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [prompt before each backup]; otherwise, <c>false</c>.
        /// </value>
        public bool PromptBeforeEachBackup { get; set; }

        private void UpdateDbFiles()
        {
            try
            {
                foreach (DbFile dbFile in this.DbFiles)
                {
                    string fileName = this.GetTargetDbFilePhysicalName(dbFile.PhysicalName);
                    if (!dbFile.DbFileType.Equals("Log"))
                    {
                        if (!string.IsNullOrEmpty(this.DataFilesFolder))
                        {
                            dbFile.PhysicalNameRelocate = PathWrapper.Combine(this.DataFilesFolder, fileName);
                        }
                        else
                        {
                            dbFile.PhysicalNameRelocate = fileName;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(this.LogFilesFolder))
                        {
                            dbFile.PhysicalNameRelocate = PathWrapper.Combine(this.LogFilesFolder, fileName);
                        }
                        else
                        {
                            dbFile.PhysicalNameRelocate = fileName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.ActiveException = ex;
            }
        }

        /// <summary>
        /// Updates the Restore folder location of those db files whose orginal directory location
        /// is not present in the destination computer.
        /// </summary>
        internal void UpdateDBFilesPhysicalRelocate()
        {
            foreach (DbFile item in DbFiles)
            {
                string fileName = this.GetTargetDbFilePhysicalName(item.PhysicalName);
                item.PhysicalNameRelocate = PathWrapper.Combine(PathWrapper.GetDirectoryName(item.PhysicalName),
                    fileName);
                Uri pathUri;
                bool fUriCreated = Uri.TryCreate(item.PhysicalNameRelocate, UriKind.Absolute, out pathUri);
                if ((!fUriCreated || pathUri.Scheme != Uri.UriSchemeHttps) &&
                    !Directory.Exists(Path.GetDirectoryName(item.PhysicalNameRelocate)))
                {
                    string directoryPath = string.Empty;
                    if (string.Compare(item.DbFileType, SR.Log, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        directoryPath = Util.GetDefaultLogFileFolder();
                    }
                    else
                    {
                        directoryPath = Util.GetDefaultDataFileFolder();
                    }

                    item.PhysicalNameRelocate = PathWrapper.Combine(directoryPath, fileName);
                }
            }
        }

        private List<DbFile> dbFiles = new List<DbFile>();

        /// <summary>
        /// List of files of the source database or in the backup file
        /// </summary>
        public List<DbFile> DbFiles
        {
            get { return dbFiles; }
        }

        internal RestorePlan restorePlan;

        /// <summary>
        /// Restore plan to do the restore
        /// </summary>
        public RestorePlan RestorePlan
        {
            get
            {
                return this.restorePlan;
            }
            internal set
            {
                this.restorePlan = value;
            }
        }

        /// <summary>
        /// The database being restored
        /// </summary>
        public string targetDbName = string.Empty;

        /// <summary>
        /// The database from the backup file used to restore to by default
        /// </summary>
        public string DefaultSourceDbName
        {
            get
            {
                var dbNames = SourceDbNames;
                string dbName = dbNames.FirstOrDefault();
                return dbName;
            }
        }

        /// <summary>
        /// Current value of source db name in the planner
        /// </summary>
        public string SourceDatabaseName
        {
            get
            {
                return this.RestorePlanner == null ? string.Empty : this.RestorePlanner.DatabaseName;
            }
            set
            {
                if(this.RestorePlanner != null)
                {
                    string currentDatabaseName = this.RestorePlanner.DatabaseName;
                    this.RestorePlanner.DatabaseName = value;

                    if (string.Compare(currentDatabaseName, value, StringComparison.InvariantCultureIgnoreCase) != 0)
                    {
                        ResetOptions();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [close existing connections].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [close existing connections]; otherwise, <c>false</c>.
        /// </value>
        public bool CloseExistingConnections { get; set; }

        /*
        private BackupTimeLine.TimeLineDuration timeLineDuration = BackupTimeLine.TimeLineDuration.Day;

        public BackupTimeLine.TimeLineDuration TimeLineDuration
        {
            get { return this.timeLineDuration; }
            set { this.timeLineDuration = value; }
        }
        */

        /// <summary>
        /// Sql server credential name used to restore from Microsoft Azure url
        /// </summary>
        internal string CredentialName = string.Empty;

        /// <summary>
        /// Azure container SAS policy
        /// </summary>
        internal string ContainerSharedAccessPolicy = string.Empty;

        /// <summary>
        /// Returns the physical name for the target Db file.
        /// It is the sourceDbName replaced with targetDbName in sourceFilename.
        /// If either sourceDbName or TargetDbName is empty, the source Db filename is returned.
        /// </summary>
        /// <param name="sourceDbFilePhysicalLocation">source DbFile physical location</param>
        /// <returns></returns>
        private string GetTargetDbFilePhysicalName(string sourceDbFilePhysicalLocation)
        {
            string fileName = Path.GetFileName(sourceDbFilePhysicalLocation);
            if (!string.IsNullOrEmpty(this.DefaultSourceDbName) && !string.IsNullOrEmpty(this.targetDbName))
            {
                string sourceFilename = fileName;
                fileName = sourceFilename.Replace(this.DefaultSourceDbName, this.targetDbName);
            }
            return fileName;
        }

        public IEnumerable<BackupSetInfo> GetBackupSetInfo()
        {
            List<BackupSetInfo> result = new List<BackupSetInfo>();
            foreach (Restore restore in RestorePlan.RestoreOperations)
            {
                BackupSetInfo backupSetInfo = BackupSetInfo.Create(restore, Server);
                result.Add(backupSetInfo);
            }

            return result;
        }

        /// <summary>
        /// List of selected backupsets
        /// </summary>
        public DatabaseFileInfo[] GetSelectedBakupSets()
        {
            List<DatabaseFileInfo> result = new List<DatabaseFileInfo>();
            IEnumerable<BackupSetInfo> backupSetInfos = GetBackupSetInfo();
            foreach (var backupSetInfo in backupSetInfos)
            {
                var item = new DatabaseFileInfo(backupSetInfo.ConvertPropertiesToArray());
                Guid backupSetGuid;
                if(!Guid.TryParse(item.Id, out backupSetGuid))
                {
                    backupSetGuid = Guid.Empty;
                }
                item.IsSelected = this.backupSetsFilterInfo.IsBackupSetSelected(backupSetGuid);
                result.Add(item);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Gets the files of the database
        /// </summary>
        public List<DbFile> GetDbFiles()
        {
            Database db = null;
            List<DbFile> ret = new List<DbFile>();
            try
            {
                if (!this.RestorePlanner.ReadHeaderFromMedia)
                {
                    db = this.Server.Databases[this.RestorePlanner.DatabaseName];
                }
                if (restorePlan != null && restorePlan.RestoreOperations.Count > 0)
                {
                    if (db != null && db.Status == DatabaseStatus.Normal)
                    {
                        ret = this.Util.GetDbFiles(db);
                    }
                    else
                    {
                        ret = this.Util.GetDbFiles(restorePlan.RestoreOperations[0]);
                    }
                }
            }
            catch(Exception ex )
            {
                Logger.Write(LogLevel.Normal, $"Failed to get restore db files. error: {ex.Message}");
            }
            return ret;
        }

        public string DefaultDataFileFolder
        {
            get
            {
                return Util.GetDefaultDataFileFolder();
            }
        }

        public string DefaultLogFileFolder
        {
            get
            {
                return Util.GetDefaultLogFileFolder();
            }
        }

        public string DefaultBackupFolder
        {
            get
            {
                return CommonUtilities.GetDefaultBackupFolder(this.server.ConnectionContext);
            }
        }

        internal RestorePlan CreateRestorePlan(DatabaseRestorePlanner planner, RestoreOptions restoreOptions)
        {
            this.CreateOrUpdateRestorePlanException = null;
            RestorePlan ret = null;

            try
            {
                ret = planner.CreateRestorePlan(restoreOptions);
                if (ret == null || ret.RestoreOperations.Count == 0)
                {
                    this.ActiveException = planner.GetBackupDeviceReadErrors();
                }
            }
            catch (Exception ex)
            {
                this.ActiveException = ex;
                this.CreateOrUpdateRestorePlanException = this.ActiveException;
            }
            finally
            {
            }
               
           
            return ret;
        }

        private void ResetOptions()
        {
            isTailLogBackupPossible = null;
            isTailLogBackupWithNoRecoveryPossible = null;
            bool isTailLogBackupPossibleValue = IsTailLogBackupPossible;
            bool isTailLogBackupWithNoRecoveryPossibleValue = isTailLogBackupPossibleValue;
        }

        public void CreateNewRestorePlan()
        {
            this.ActiveException = null; //Clear any existing exceptions as the plan is getting recreated. 
                                         //Clear any existing exceptions as new plan is getting recreated.
            this.CreateOrUpdateRestorePlanException = null;
            this.DbFiles.Clear();
            this.PlanUpdateRequired = false;
            this.restorePlan = null;
            if (String.IsNullOrEmpty(this.RestorePlanner.DatabaseName))
            {
                this.RestorePlan = new RestorePlan(this.Server);
                this.Util.AddCredentialNameForUrlBackupSet(this.RestorePlan, this.CredentialName);
            }
            else
            {
                this.RestorePlan = this.CreateRestorePlan(this.RestorePlanner, this.RestoreOptions);
                this.Util.AddCredentialNameForUrlBackupSet(this.restorePlan, this.CredentialName);

                if (this.ActiveException == null)
                {
                    this.dbFiles = this.GetDbFiles();

                    UpdateDBFilesPhysicalRelocate();
                   

                }
            }
            if (this.restorePlan == null)
            {
                this.RestorePlan = new RestorePlan(this.Server);
                this.Util.AddCredentialNameForUrlBackupSet(this.RestorePlan, this.CredentialName);
            }
        }

        public bool ShouldCreateNewPlan()
        {
            return RestorePlan == null || string.Compare(RestorePlanner.DatabaseName, this.RestoreParams.GetOptionValue<string>(RestoreOptionsHelper.SourceDatabaseName), StringComparison.InvariantCultureIgnoreCase) != 0 ||
                RestorePlanner.ReadHeaderFromMedia != this.RestoreParams.ReadHeaderFromMedia ||
                this.RelocateAllFiles != this.RestoreParams.GetOptionValue<bool>(RestoreOptionsHelper.RelocateDbFiles) ||
                string.Compare(this.backupMediaList, RestoreParams.BackupFilePaths, StringComparison.InvariantCultureIgnoreCase) != 0;
        }

        /// <summary>
        /// Updates restore plan
        /// </summary>
        public void UpdateRestorePlan()
        {
            RestoreOptionsHelper.UpdateOptionsInPlan(this);
            if (RelocateAllFiles)
            {
                UpdateDbFiles();
            }
            this.SetRestorePlanProperties(this.restorePlan);

            UpdateSelectedBackupSets();
        }

        /// <summary>
        /// Determine if restore plan of selected database does have Url
        /// </summary>
        private bool IfRestorePlanHasUrl()
        {
            return (restorePlan.RestoreOperations.Any(
                res => res.BackupSet.BackupMediaSet.BackupMediaList.Any(t => t.MediaType == DeviceType.Url)));
        }

            
        /// <summary>
        /// Sets restore plan properties
        /// </summary>
        private void SetRestorePlanProperties(RestorePlan rp)
        {
            if (rp == null || rp.RestoreOperations.Count < 1)
            {
                return;
            }
            rp.SetRestoreOptions(this.RestoreOptions);
            rp.CloseExistingConnections = this.CloseExistingConnections;
            if (this.TargetDatabaseName != null && !this.TargetDatabaseName.Equals(string.Empty))
            {
                rp.DatabaseName = TargetDatabaseName;
            }
            rp.RestoreOperations[0].RelocateFiles.Clear();
            foreach (DbFile dbFile in this.DbFiles)
            {
                // For XStore path, we don't want to try the getFullPath.
                string newPhysicalPath;
                Uri pathUri;
                bool uriCreated = Uri.TryCreate(dbFile.PhysicalNameRelocate, UriKind.Absolute, out pathUri);
                if (uriCreated && pathUri.Scheme == "https")
                {
                    newPhysicalPath = dbFile.PhysicalNameRelocate;
                }
                else
                {
                    newPhysicalPath = Path.GetFullPath(dbFile.PhysicalNameRelocate);
                }
                if (!dbFile.PhysicalName.Equals(newPhysicalPath))
                {
                    RelocateFile relocFile = new RelocateFile(dbFile.LogicalName, dbFile.PhysicalNameRelocate);
                    rp.RestoreOperations[0].RelocateFiles.Add(relocFile);
                }
            }
        }

        /// <summary>
        /// Bool indicating whether a tail log backup will be taken
        /// </summary>
        public bool BackupTailLog
        {
            get
            {
                return this.RestorePlanner.BackupTailLog;
            }
            set
            {
                if (this.RestorePlanner.BackupTailLog != value)
                {
                    this.RestorePlanner.BackupTailLog = value;
                    this.PlanUpdateRequired = true;
                }
            }
        }

        /// <summary>
        /// bool indicating whether the database will be left in restoring state
        /// </summary>
        public bool TailLogWithNoRecovery
        {
            get
            {
                return this.RestorePlanner.TailLogWithNoRecovery;
            }
            set
            {
                if (this.RestorePlanner.TailLogWithNoRecovery != value)
                {
                    this.RestorePlanner.TailLogWithNoRecovery = value;
                    this.PlanUpdateRequired = true;
                }
            }
        }

        public DateTime? CurrentRestorePointInTime
        {
            get
            {
                if (!IsAnyFullBackupSetSelected())
                {
                    return null;
                }
                for (int i = this.RestorePlan.RestoreOperations.Count - 1; i >= 0; i--)
                {
                    BackupSet backupSet = this.RestorePlan.RestoreOperations[i].BackupSet;
                    if (this.backupSetsFilterInfo.IsBackupSetSelected(backupSet))
                    {
                        if (backupSet == null
                            || (backupSet.BackupSetType == BackupSetType.Log &&
                                this.RestorePlan.RestoreOperations[i].ToPointInTime != null))
                        {
                            return this.RestorePlanner.RestoreToPointInTime;
                        }
                        return backupSet.BackupStartDate;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Returns the restore plan error message
        /// </summary>
        public override string ErrorMessage
        {
            get
            {
                if (ActiveException != null)
                {
                    return ActiveException.Message;
                }
                return string.Empty;
            }
        }


        private bool IsAnyFullBackupSetSelected()
        {
            bool isSelected = false;

            if (this.RestorePlan != null && this.RestorePlan.RestoreOperations.Any() && this.backupSetsFilterInfo.AnySelected)
            {
                var fullBackupSet = this.RestorePlan.RestoreOperations.FirstOrDefault(x => x.BackupSet.BackupSetType == BackupSetType.Database);
                isSelected = fullBackupSet != null && this.backupSetsFilterInfo.IsBackupSetSelected(fullBackupSet.BackupSet.BackupSetGuid);
            }

            return isSelected;
        }

        private int GetLastSelectedBackupSetIndex()
        {
            if (this.RestorePlan != null && this.RestorePlan.RestoreOperations.Any() && this.backupSetsFilterInfo.AnySelected)
            {
                for (int i = this.RestorePlan.RestoreOperations.Count -1; i >= 0; i--)
                {
                    BackupSet backupSet = this.RestorePlan.RestoreOperations[i].BackupSet;
                    if (this.backupSetsFilterInfo.IsBackupSetSelected(backupSet))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private int GetFirstSelectedBackupSetIndex()
        {
            if (this.RestorePlan != null && this.RestorePlan.RestoreOperations.Any() && this.backupSetsFilterInfo.AnySelected)
            {
                for (int i = 0; i < this.RestorePlan.RestoreOperations.Count - 1; i++)
                {
                    BackupSet backupSet = this.RestorePlan.RestoreOperations[i].BackupSet;
                    if (this.backupSetsFilterInfo.IsBackupSetSelected(backupSet))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }


        public void UpdateSelectedBackupSets()
        {
            this.backupSetsFilterInfo.Clear();
            var selectedBackupSetsFromClient = this.RestoreParams.SelectedBackupSets;

            if (this.RestorePlan != null && this.RestorePlan.RestoreOperations != null)
            {
                for (int index = 0; index < this.RestorePlan.RestoreOperations.Count; index++)
                {
                    BackupSet backupSet = this.RestorePlan.RestoreOperations[index].BackupSet;
                    if (backupSet != null)
                    {
                        // If the collection client sent is null, select everything; otherwise select the items that are selected in client
                        bool backupSetSelected = selectedBackupSetsFromClient == null || selectedBackupSetsFromClient.Any(x => BackUpSetGuidEqualsId(backupSet, x));


                        if (backupSetSelected)
                        {
                            AddBackupSetsToSelected(index, index);

                            //the last index - this will include tail-Log restore operation if present
                            //If the last item is selected, select all the items before that because the last item
                            //is tail-log
                            if (index == this.RestorePlan.RestoreOperations.Count - 1)
                            {
                                AddBackupSetsToSelected(0, index);
                            }

                            //If the second item is selected and it's a diff backup, the fist item (full backup) has to be selected
                            if (index == 1 && backupSet.BackupSetType == BackupSetType.Differential)
                            {
                                AddBackupSetsToSelected(0, 0);
                            }

                            //If the selected item is a log backup, select all the items before that
                            if (backupSet.BackupSetType == BackupSetType.Log)
                            {
                                AddBackupSetsToSelected(0, index);
                            }
                        }
                        else
                        {
                            // If the first item is not selected which is always the full backup, other backupsets cannot be selected
                            if (index == 0)
                            {
                                selectedBackupSetsFromClient = new string[] { };
                            }

                            //The a log is not selected, the logs after that cannot be selected
                            if (backupSet.BackupSetType == BackupSetType.Log)
                            {
                                break;
                            }

                            //If the second item is not selected and it's a diff backup 
                            if (index == 1 && backupSet.BackupSetType == BackupSetType.Differential)
                            {
                                if (this.Server.Version.Major < 9 || 
                                    (this.RestorePlan.RestoreOperations.Count >= 3 && 
                                    BackupSet.IsBackupSetsInSequence(this.RestorePlan.RestoreOperations[0].BackupSet, this.RestorePlan.RestoreOperations[2].BackupSet)))
                                {
                                    // only the item at index 1 won't be selected
                                }
                                else
                                {
                                    // nothing after index 1 should be selected
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool BackUpSetGuidEqualsId(BackupSet backupSet, string id)
        {
           return backupSet != null && string.Compare(backupSet.BackupSetGuid.ToString(), id, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private void AddBackupSetsToSelected(int from, int to)
        {
            if (this.RestorePlan != null && this.RestorePlan.RestoreOperations != null 
                && from < this.RestorePlan.RestoreOperations.Count && to < this.RestorePlan.RestoreOperations.Count)
            {
                for (int i = from; i <= to; i++)
                {
                    BackupSet backupSet = this.RestorePlan.RestoreOperations[i].BackupSet;
                    this.backupSetsFilterInfo.Add(backupSet);
                }
            }
        }

        /// <summary>
        /// Verifies the backup files location.
        /// </summary>
        internal void CheckBackupFilesLocation()
        {
            if (this.RestorePlan != null)
            {
                foreach (Restore restore in this.RestorePlan.RestoreOperations)
                {
                    if (restore.BackupSet != null)
                    {
                        restore.BackupSet.CheckBackupFilesExistence();
                    }
                }
            }
        }

        internal bool DbFilesLocationAreValid()
        {
            foreach (DbFile dbFile in this.DbFiles)
            {
                string newPhysicalPath = Path.GetFullPath(dbFile.PhysicalNameRelocate);
                if (string.Compare(dbFile.PhysicalName, dbFile.PhysicalNameRelocate, true) != 0)
                {
                    bool isValidFolder = false;
                    bool isValidPath = Util.IsDestinationPathValid(Path.GetDirectoryName(newPhysicalPath), ref isValidFolder);
                    if (!(isValidFolder && isValidPath))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Returns true if can close eixisting connections for give database
        /// </summary>
        public bool CanDropExistingConnections
        {
            get
            {
                if (RestorePlan != null && RestorePlanner != null)
                {
                    return RestorePlan.CanDropExistingConnections(RestorePlanner.DatabaseName);
                }
                return false;
            }
        }

        /// <summary>
        /// Cancels the restore operations
        /// </summary>
        public override void Cancel()
        {
            foreach (Restore restore in RestorePlan.RestoreOperations)
            {
                restore.Abort();
            }
        }

        /// <summary>
        /// Create a restore data object that includes the plan to do the restore operation
        /// </summary>
        /// <param name="requestParam"></param>
        /// <returns></returns>
        internal void UpdateRestoreTaskObject()
        {
            bool shouldCreateNewPlan = ShouldCreateNewPlan();

            if (!string.IsNullOrEmpty(RestoreParams.BackupFilePaths) && RestoreParams.ReadHeaderFromMedia)
            {
                AddFiles(RestoreParams.BackupFilePaths);
            }
            else
            {
                RestorePlanner.BackupMediaList.Clear();
            }
            RestorePlanner.ReadHeaderFromMedia = RestoreParams.ReadHeaderFromMedia;

            RestoreOptionFactory.Instance.SetAndValidate(RestoreOptionsHelper.SourceDatabaseName, this);
            RestoreOptionFactory.Instance.SetAndValidate(RestoreOptionsHelper.TargetDatabaseName, this);

            if (shouldCreateNewPlan)
            {
                CreateNewRestorePlan();
            }

            UpdateRestorePlan();

        }
    }
}
