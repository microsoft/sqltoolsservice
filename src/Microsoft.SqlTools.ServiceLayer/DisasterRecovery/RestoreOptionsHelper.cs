//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    public class RestoreOptionsHelper
    {
        internal const string KeepReplication = "keepReplication";
        internal const string ReplaceDatabase = "replaceDatabase";
        internal const string SetRestrictedUser = "setRestrictedUser";
        internal const string RecoveryState = "recoveryState";
        internal const string BackupTailLog = "backupTailLog";
        internal const string DefaultBackupTailLog = "defaultBackupTailLog";
        internal const string TailLogBackupFile = "tailLogBackupFile";
        internal const string DefaultTailLogBackupFile = "defaultTailLogBackupFile";
        internal const string TailLogWithNoRecovery = "tailLogWithNoRecovery";
        internal const string CloseExistingConnections = "closeExistingConnections";
        internal const string RelocateDbFiles = "relocateDbFiles";
        internal const string DataFileFolder = "dataFileFolder";
        internal const string DefaultDataFileFolder = "defaultDataFileFolder";
        internal const string LogFileFolder = "logFileFolder";
        internal const string DefaultLogFileFolder = "defaultLogFileFolder";
        internal const string SessionId = "sessionId";
        internal const string BackupFilePaths = "backupFilePaths";
        internal const string TargetDatabaseName = "targetDatabaseName";
        internal const string SourceDatabaseName = "sourceDatabaseName";
        internal const string SelectedBackupSets = "selectedBackupSets";
        internal const string StandbyFile = "standbyFile";
        internal const string DefaultStandbyFile = "defaultStandbyFile";

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
                    GroupName = "Restore options"
                },
                new ServiceOption
                {
                    Name = RestoreOptionsHelper.ReplaceDatabase,
                    DisplayName = "ReplaceDatabase",
                    Description = "Overwrite the existing database (WITH REPLACE)",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    GroupName = "Restore options"
                },
                new ServiceOption
                {
                    Name = RestoreOptionsHelper.SetRestrictedUser,
                    DisplayName = "SetRestrictedUser",
                    Description = "Restrict access to the restored database (WITH RESTRICTED_USER)",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    GroupName = "Restore options"
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
                    }
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
                    GroupName = "Restore database files as"
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
    }
}
