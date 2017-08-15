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
        //The list of names service uses to sends restore options to client
        private static string[] optionNames = new string[] { KeepReplication, ReplaceDatabase , SetRestrictedUser, RecoveryState ,
            BackupTailLog , TailLogBackupFile, TailLogWithNoRecovery, CloseExistingConnections, RelocateDbFiles, DataFileFolder, LogFileFolder,
            StandbyFile,
        };
        //The key names of restore info in the resquest of response

        //Option name keepReplication
        internal const string KeepReplication = "keepReplication";

        //Option name replaceDatabase
        internal const string ReplaceDatabase = "replaceDatabase";

        //Option name setRestrictedUser
        internal const string SetRestrictedUser = "setRestrictedUser";

        //Option name recoveryState
        internal const string RecoveryState = "recoveryState";

        //Option name backupTailLog
        internal const string BackupTailLog = "backupTailLog";

        //Option name tailLogBackupFile
        internal const string TailLogBackupFile = "tailLogBackupFile";

        //Option name tailLogWithNoRecovery
        internal const string TailLogWithNoRecovery = "tailLogWithNoRecovery";

        //Option name closeExistingConnections
        internal const string CloseExistingConnections = "closeExistingConnections";

        //Option name relocateDbFiles
        internal const string RelocateDbFiles = "relocateDbFiles";

        //Option name dataFileFolder
        internal const string DataFileFolder = "dataFileFolder";

        //Option name logFileFolder
        internal const string LogFileFolder = "logFileFolder";

        //The key name to use to set the session id in the request
        internal const string SessionId = "sessionId";

        //The key name to use to set the backup file paths in the request
        internal const string BackupFilePaths = "backupFilePaths";

        //The key name to use to set the target database name in the request
        internal const string TargetDatabaseName = "targetDatabaseName";

        //The key name to use to set the source database name in the request
        internal const string SourceDatabaseName = "sourceDatabaseName";

        //The key name to use to set the selected backup sets in the request
        internal const string SelectedBackupSets = "selectedBackupSets";

        //The key name to use to set the standby file sets in the request
        internal const string StandbyFile = "standbyFile";

        //The key name to use to set source db names in restore response
        internal const string SourceDatabaseNamesWithBackupSets = "sourceDatabaseNamesWithBackupSets";

        //The key name to use to set in the requst. If set to true, the backup files will be used to restore otherwise the source database name 
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
            RestoreOptionFactory restoreOptionFactory = RestoreOptionFactory.Instance;

            //Create the options using the current values
            foreach (var optionKey in optionNames)
            {
                var optionInfo = restoreOptionFactory.CreateOptionInfo(optionKey, restoreDataObject);
                options.Add(optionKey, optionInfo);
            }

            // After all options are set verify them all again to set the read only 
            // Becuase some options can change the readnly mode of other options.( e.g Recovery state can affect StandBy to be readyonly)
            foreach (var optionKey in optionNames)
            {
                restoreOptionFactory.UpdateOption(optionKey, restoreDataObject, options[optionKey]);
            }
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

        /// <summary>
        /// Load options in restore plan
        /// </summary>
        internal static void UpdateOptionsInPlan(IRestoreDatabaseTaskDataObject restoreDataObject)
        {
            RestoreOptionFactory restoreOptionFactory = RestoreOptionFactory.Instance;


            foreach (var optionKey in optionNames)
            {
                restoreOptionFactory.SetValue(optionKey, restoreDataObject);
            }

            //After all options are set do a vaidation so any invalid option set to default
            foreach (var optionKey in optionNames)
            {
                string error = restoreOptionFactory.ValidateOption(optionKey, restoreDataObject);
                if (!string.IsNullOrEmpty(error))
                {
                    //TODO: we could send back the error message so client knows the option is set incorrectly
                }
            }
            
        }
    }
}
