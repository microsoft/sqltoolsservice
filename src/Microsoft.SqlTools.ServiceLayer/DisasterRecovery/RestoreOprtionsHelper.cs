//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    public class RestoreOprtionsHelper
    {
        internal const string KeepReplication = "KeepReplication";
        internal const string ReplaceDatabase = "ReplaceDatabase";
        internal const string SetRestrictedUser = "SetRestrictedUser";
        internal const string RecoveryState = "RecoveryState";
        internal const string BackupTailLog = "BackupTailLog";
        internal const string TailLogBackupFile = "TailLogBackupFile";
        internal const string TailLogWithNoRecovery = "TailLogWithNoRecovery";
        internal const string CloseExistingConnections = "CloseExistingConnections";

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
                    Name = RestoreOprtionsHelper.KeepReplication,
                    DisplayName = "Keep Replication",
                    Description = "Preserve the replication settings (WITH KEEP_REPLICATION)",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    GroupName = "Restore options"
                },
                new ServiceOption
                {
                    Name = RestoreOprtionsHelper.ReplaceDatabase,
                    DisplayName = "ReplaceDatabase",
                    Description = "Overwrite the existing database (WITH REPLACE)",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    GroupName = "Restore options"
                },
                new ServiceOption
                {
                    Name = RestoreOprtionsHelper.SetRestrictedUser,
                    DisplayName = "SetRestrictedUser",
                    Description = "Restrict access to the restored database (WITH RESTRICTED_USER)",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    GroupName = "Restore options"
                },
                new ServiceOption
                {
                    Name = RestoreOprtionsHelper.RecoveryState,
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
                    Name = RestoreOprtionsHelper.BackupTailLog,
                    DisplayName = "Backup Tail Log",
                    Description = "Take tail-log backup before restore",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    DefaultValue = "true",
                    GroupName = "Tail-Log backup"
                },
                new ServiceOption
                {
                    Name = RestoreOprtionsHelper.BackupTailLog,
                    DisplayName = "Backup Tail Log",
                    Description = "Take tail-log backup before restore",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    DefaultValue = "true",
                    GroupName = "Tail-Log backup"
                },
                new ServiceOption
                {
                    Name = RestoreOprtionsHelper.TailLogBackupFile,
                    DisplayName = "Tail Log Backup File",
                    Description = "Tail Log Backup File",
                    ValueType = ServiceOption.ValueTypeString,
                    IsRequired = false,
                    GroupName = "Tail-Log backup"
                },
                new ServiceOption
                {
                    Name = RestoreOprtionsHelper.TailLogWithNoRecovery,
                    DisplayName = "Tail Log With NoRecovery",
                    Description = "Leave source database in the restoring state(WITH NORECOVERTY)",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    GroupName = "Tail-Log backup"
                },
                new ServiceOption
                {
                    Name = RestoreOprtionsHelper.CloseExistingConnections,
                    DisplayName = "Close Existing Connections",
                    Description = "Close existing connections to destination database",
                    ValueType = ServiceOption.ValueTypeBoolean,
                    IsRequired = false,
                    GroupName = "Server connections"
                }
            };

            return options;
        }
    }
}
