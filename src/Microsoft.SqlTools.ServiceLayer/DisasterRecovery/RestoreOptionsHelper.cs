//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    public class RestoreOptionsHelper
    {
        internal const string KeepReplication = "keepReplication";
        internal const string ReplaceDatabase = "replaceDatabase";
        internal const string SetRestrictedUser = "setRestrictedUser";
        internal const string RecoveryState = "recoveryState";
        internal const string BackupTailLog = "backupTailLog";
        internal const string TailLogBackupFile = "tailLogBackupFile";
        internal const string TailLogWithNoRecovery = "tailLogWithNoRecovery";
        internal const string CloseExistingConnections = "closeExistingConnections";
        internal const string RelocateDbFiles = "relocateDbFiles";
        internal const string DataFileFolder = "dataFileFolder";
        internal const string LogFileFolder = "logFileFolder";
        internal const string SessionId = "sessionId";
        internal const string BackupFilePaths = "backupFilePaths";
        internal const string TargetDatabaseName = "targetDatabaseName";
        internal const string SourceDatabaseName = "sourceDatabaseName";
        internal const string SelectedBackupSets = "selectedBackupSets";
        internal const string StandbyFile = "standbyFile";
        internal const string SourceDatabaseNamesWithBackupSets = "sourceDatabaseNamesWithBackupSets";
        internal const string ReadHeaderFromMedia = "readHeaderFromMedia";

        /// <summary>
        /// Creates the options metadata available for restore operations
        /// </summary>
        /// <returns></returns>
        public static ServiceOption[] CreateRestoreOptions()
        {
            ServiceOption[] options = new ServiceOption[]
            {

                new ServiceOption
                {
                    Name = RestoreOptionsHelper.KeepReplication,
                    DisplayName = "Keep Replication",
                    Description = "Preserve the replication settings (WITH KEEP_REPLICATION)",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    GroupName = "Restore options",
                    DefaultValue = "false"
                },
                new ServiceOption
                {
                    Name = RestoreOptionsHelper.ReplaceDatabase,
                    DisplayName = "ReplaceDatabase",
                    Description = "Overwrite the existing database (WITH REPLACE)",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    GroupName = "Restore options",
                    DefaultValue = "false"
                },
                new ServiceOption
                {
                    Name = RestoreOptionsHelper.SetRestrictedUser,
                    DisplayName = "SetRestrictedUser",
                    Description = "Restrict access to the restored database (WITH RESTRICTED_USER)",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    GroupName = "Restore options",
                    DefaultValue = "false"
                },
                new ServiceOption
                {
                    Name = RestoreOptionsHelper.RecoveryState,
                    DisplayName = "Recovery State",
                    Description = "Recovery State",
                    ValueType = ServiceOption.ValueTypeCategory,
                    IsRequired = false,
                    GroupName = "Restore options",
                    CategoryValues = new CategoryValue[]
                    {
                        new CategoryValue
                        {
                            Name = "WithRecovery",
                            DisplayName = "RESTORE WITH RECOVERTY"
                        },
                        new CategoryValue
                        {
                            Name = "WithNoRecovery",
                            DisplayName = "RESTORE WITH NORECOVERTY"
                        },
                        new CategoryValue
                        {
                            Name = "WithStandBy",
                            DisplayName = "RESTORE WITH STANDBY"
                        }
                    },
                    DefaultValue = "WithRecovery"
                },
                new ServiceOption
                {
                    Name = RestoreOptionsHelper.StandbyFile,
                    DisplayName = "Standby file",
                    Description = "Standby file",
                    ValueType = ServiceOption.ValueTypeString,
                    IsRequired = false,
                    GroupName = "Restore options"
                },
                new ServiceOption
                {
                    Name = RestoreOptionsHelper.BackupTailLog,
                    DisplayName = "Backup Tail Log",
                    Description = "Take tail-log backup before restore",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    DefaultValue = "true",
                    GroupName = "Tail-Log backup"
                },
                new ServiceOption
                {
                    Name = RestoreOptionsHelper.BackupTailLog,
                    DisplayName = "Backup Tail Log",
                    Description = "Take tail-log backup before restore",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    DefaultValue = "true",
                    GroupName = "Tail-Log backup"
                },
                new ServiceOption
                {
                    Name = RestoreOptionsHelper.TailLogBackupFile,
                    DisplayName = "Tail Log Backup File",
                    Description = "Tail Log Backup File",
                    ValueType = ServiceOption.ValueTypeString,
                    IsRequired = false,
                    GroupName = "Tail-Log backup"
                },
                new ServiceOption
                {
                    Name = RestoreOptionsHelper.TailLogWithNoRecovery,
                    DisplayName = "Tail Log With NoRecovery",
                    Description = "Leave source database in the restoring state(WITH NORECOVERTY)",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    GroupName = "Tail-Log backup"
                },
                new ServiceOption
                {
                    Name = RestoreOptionsHelper.CloseExistingConnections,
                    DisplayName = "Close Existing Connections",
                    Description = "Close existing connections to destination database",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    GroupName = "Server connections"
                },
                new ServiceOption
                {
                    Name = RestoreOptionsHelper.RelocateDbFiles,
                    DisplayName = "Relocate all files",
                    Description = "Relocate all files",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    GroupName = "Restore database files as",
                    DefaultValue = "false"
                },
                new ServiceOption
                {
                    Name = RestoreOptionsHelper.DataFileFolder,
                    DisplayName = "Data file folder",
                    Description = "Data file folder",
                    ValueType = ServiceOption.ValueTypeString,
                    IsRequired = false,
                    GroupName = "Restore database files as"
                },
                new ServiceOption
                {
                    Name = RestoreOptionsHelper.LogFileFolder,
                    DisplayName = "Log file folder",
                    Description = "Log file folder",
                    ValueType = ServiceOption.ValueTypeString,
                    IsRequired = false,
                    GroupName = "Restore database files as"
                }
            };

            return options;
        }

        internal static Dictionary<string, RestorePlanDetailInfo> CreateRestorePlanOptions(IRestoreDatabaseTaskDataObject restoreDataObject)
        {
            Validate.IsNotNull(nameof(restoreDataObject), restoreDataObject);

            Dictionary<string, RestorePlanDetailInfo> options = new Dictionary<string, RestorePlanDetailInfo>();
            string databaseName = restoreDataObject.RestorePlan == null ? string.Empty : restoreDataObject.RestorePlan.DatabaseName;
            //Files

            // Default Data folder path in the target server
            options.Add(RestoreOptionsHelper.DataFileFolder, RestorePlanDetailInfo.Create(
               name: RestoreOptionsHelper.DataFileFolder,
               currentValue: restoreDataObject.DataFilesFolder,
               defaultValue: restoreDataObject.DefaultDataFileFolder,
               isReadOnly: !restoreDataObject.RelocateAllFiles,
               isVisible: true
               ));

            // Default log folder path in the target server
            options.Add(RestoreOptionsHelper.LogFileFolder, RestorePlanDetailInfo.Create(
              name: RestoreOptionsHelper.LogFileFolder,
              currentValue: restoreDataObject.LogFilesFolder,
              defaultValue: restoreDataObject.DefaultLogFileFolder,
              isReadOnly: !restoreDataObject.RelocateAllFiles,
              isVisible: true
              ));

            // Relocate all files
            options.Add(RestoreOptionsHelper.RelocateDbFiles, RestorePlanDetailInfo.Create(
              name: RestoreOptionsHelper.RelocateDbFiles,
              currentValue: restoreDataObject.RelocateAllFiles,
              defaultValue: false,
              isReadOnly: restoreDataObject.DbFiles.Count == 0,
              isVisible: true
              ));


            //Options

            //With Replace
            options.Add(RestoreOptionsHelper.ReplaceDatabase, RestorePlanDetailInfo.Create(
                name: RestoreOptionsHelper.ReplaceDatabase,
                currentValue: restoreDataObject.RestoreOptions.ReplaceDatabase,
                defaultValue: false,
                isReadOnly: false,
                isVisible: true
                ));

            //Keep replication
            options.Add(RestoreOptionsHelper.KeepReplication, RestorePlanDetailInfo.Create(
                name: RestoreOptionsHelper.KeepReplication,
                currentValue: restoreDataObject.RestoreOptions.KeepReplication,
                defaultValue: false,
                isReadOnly: restoreDataObject.RestoreOptions.RecoveryState == DatabaseRecoveryState.WithNoRecovery,
                isVisible: true
                ));

            //Restricted user
            options.Add(RestoreOptionsHelper.SetRestrictedUser, RestorePlanDetailInfo.Create(
                name: RestoreOptionsHelper.SetRestrictedUser,
                currentValue: restoreDataObject.RestoreOptions.SetRestrictedUser,
                defaultValue: false,
                isReadOnly: false,
                isVisible: true
                ));

            //State recovery
            options.Add(RestoreOptionsHelper.RecoveryState, RestorePlanDetailInfo.Create(
                name: RestoreOptionsHelper.RecoveryState,
                currentValue: restoreDataObject.RestoreOptions.RecoveryState.ToString(),
                defaultValue: DatabaseRecoveryState.WithRecovery.ToString(),
                isReadOnly: false,
                isVisible: true
                ));

            // stand by file path for when RESTORE WITH STANDBY is selected
            options.Add(RestoreOptionsHelper.StandbyFile, RestorePlanDetailInfo.Create(
               name: RestoreOptionsHelper.StandbyFile,
               currentValue: restoreDataObject.RestoreOptions.StandByFile,
               defaultValue: restoreDataObject.GetDefaultStandbyFile(databaseName),
               isReadOnly: restoreDataObject.RestoreOptions.RecoveryState != DatabaseRecoveryState.WithStandBy,
               isVisible: true
               ));

            // Tail-log backup
            // TODO:These methods are internal in SMO. after making them public, they can be removed from RestoreDatabaseTaskDataObject
            bool isTailLogBackupPossible = restoreDataObject.IsTailLogBackupPossible(databaseName);
            bool isTailLogBackupWithNoRecoveryPossible = restoreDataObject.IsTailLogBackupWithNoRecoveryPossible(databaseName);

            options.Add(RestoreOptionsHelper.BackupTailLog, RestorePlanDetailInfo.Create(
                name: RestoreOptionsHelper.BackupTailLog,
                currentValue: restoreDataObject.BackupTailLog,
                defaultValue: isTailLogBackupPossible,
                isReadOnly: !isTailLogBackupPossible,
                isVisible: true
                ));

            options.Add(RestoreOptionsHelper.TailLogBackupFile, RestorePlanDetailInfo.Create(
                name: RestoreOptionsHelper.TailLogBackupFile,
                currentValue: restoreDataObject.TailLogBackupFile,
                defaultValue: restoreDataObject.GetDefaultTailLogbackupFile(databaseName),
                isReadOnly: !isTailLogBackupPossible,
                isVisible: true
                ));

            options.Add(RestoreOptionsHelper.TailLogWithNoRecovery, RestorePlanDetailInfo.Create(
                name: RestoreOptionsHelper.TailLogWithNoRecovery,
                currentValue: restoreDataObject.TailLogWithNoRecovery,
                defaultValue: isTailLogBackupWithNoRecoveryPossible,
                isReadOnly: !isTailLogBackupWithNoRecoveryPossible,
                isVisible: true
                ));
        

            //TODO: make the method public in SMO bool canDropExistingConnections = restoreDataObject.RestorePlan.CanDropExistingConnections(this.Data.RestorePlanner.DatabaseName);
            options.Add(RestoreOptionsHelper.CloseExistingConnections, RestorePlanDetailInfo.Create(
              name: RestoreOptionsHelper.CloseExistingConnections,
              currentValue: restoreDataObject.CloseExistingConnections,
              defaultValue: false,
              isReadOnly: false, //TODO: !canDropExistingConnections
              isVisible: true
              ));

            return options;
        }
        /// <summary>
        /// Add options to restore plan response
        /// </summary>
        internal static void AddOptions(RestorePlanResponse response, RestoreDatabaseTaskDataObject restoreDataObject)
        {
            Validate.IsNotNull(nameof(response), response);
            Validate.IsNotNull(nameof(restoreDataObject), restoreDataObject);
            Validate.IsNotNull(nameof(restoreDataObject.RestorePlanner), restoreDataObject.RestorePlanner);


            var options = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDataObject);

            foreach (var item in options)
            {
                response.PlanDetails.Add(item.Key, item.Value);
            }
        }

        internal static T GetOptionValue<T>(string optionkey, Dictionary<string, RestorePlanDetailInfo> optionsMetadata, IRestoreDatabaseTaskDataObject restoreDataObject)
        {
            RestorePlanDetailInfo optionMetadata = null;
            if(optionsMetadata.TryGetValue(optionkey, out optionMetadata))
            {
                if (!optionMetadata.IsReadOnly)
                {
                    return restoreDataObject.RestoreParams.GetOptionValue<T>(optionkey);
                }
                else
                {
                    return (T)Convert.ChangeType(optionMetadata.DefaultValue, typeof(T));
                }
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// Load options in restore plan
        /// </summary>
        internal static void UpdateOptionsInPlan(IRestoreDatabaseTaskDataObject restoreDataObject)
        {
            var options = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDataObject);

            //Files
            restoreDataObject.LogFilesFolder = GetOptionValue<string>(RestoreOptionsHelper.LogFileFolder, options, restoreDataObject);
            restoreDataObject.DataFilesFolder = GetOptionValue<string>(RestoreOptionsHelper.DataFileFolder, options, restoreDataObject);
            restoreDataObject.RelocateAllFiles = GetOptionValue<bool>(RestoreOptionsHelper.RelocateDbFiles, options, restoreDataObject);

            //Options
            object databaseRecoveryState;

            string recoveryState = GetOptionValue<string>(RestoreOptionsHelper.RecoveryState, options, restoreDataObject);
            if (Enum.TryParse(typeof(DatabaseRecoveryState), recoveryState, out databaseRecoveryState))
            {
                restoreDataObject.RestoreOptions.RecoveryState = (DatabaseRecoveryState)databaseRecoveryState;
            }
            restoreDataObject.RestoreOptions.KeepReplication = GetOptionValue<bool>(RestoreOptionsHelper.KeepReplication, options, restoreDataObject);
            restoreDataObject.RestoreOptions.ReplaceDatabase = GetOptionValue<bool>(RestoreOptionsHelper.ReplaceDatabase, options, restoreDataObject);
            restoreDataObject.RestoreOptions.SetRestrictedUser = GetOptionValue<bool>(RestoreOptionsHelper.SetRestrictedUser, options, restoreDataObject);
            restoreDataObject.RestoreOptions.StandByFile = GetOptionValue<string>(RestoreOptionsHelper.StandbyFile, options, restoreDataObject);

           

            restoreDataObject.BackupTailLog = GetOptionValue<bool>(RestoreOptionsHelper.BackupTailLog, options, restoreDataObject);
            restoreDataObject.TailLogBackupFile = GetOptionValue<string>(RestoreOptionsHelper.TailLogBackupFile, options, restoreDataObject);
            restoreDataObject.TailLogWithNoRecovery = GetOptionValue<bool>(RestoreOptionsHelper.TailLogWithNoRecovery, options, restoreDataObject);

            restoreDataObject.CloseExistingConnections = GetOptionValue<bool>(RestoreOptionsHelper.CloseExistingConnections, options, restoreDataObject);
        }
    }
}
