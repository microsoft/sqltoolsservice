//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Kusto.ServiceLayer.DisasterRecovery.Contracts;

namespace Microsoft.Kusto.ServiceLayer.DisasterRecovery.RestoreOperation
{
    /// <summary>
    /// Backup set Information
    /// </summary>
    public class BackupSetInfo
    {
        public const string BackupComponentPropertyName = "Component";
        public const string NamePropertyName = "Name";
        public const string BackupTypePropertyName = "Type";
        public const string ServerNamePropertyName = "Server";
        public const string DatabaseNamePropertyName = "Database";
        public const string PositionPropertyName = "Position";
        public const string FirstLsnPropertyName = "FirstLSN";
        public const string LastLsnPropertyName = "LastLSN";
        public const string CheckpointLsnPropertyName = "CheckpointLSN";
        public const string FullLsnPropertyName = "FullLSN";
        public const string StartDatePropertyName = "StartDate";
        public const string FinishDatePropertyName = "FinishDate";
        public const string SizePropertyName = "Size";
        public const string UserNamePropertyName = "UserName";
        public const string ExpirationPropertyName = "Expiration";

        private Dictionary<string, LocalizedPropertyInfo> properties;

        public BackupSetInfo(Dictionary<string, LocalizedPropertyInfo> properties)
        {
            this.properties = properties;
        }

        /// <summary>
        /// Backup type (Full, Transaction Log, Differential ...)
        /// </summary>
        public string BackupType
        {
            get
            {
                return GetPropertyValueAsString(BackupTypePropertyName);
            }
        }

        /// <summary>
        /// Backup set properties
        /// </summary>
        public ReadOnlyDictionary<string, LocalizedPropertyInfo> Properties
        {
            get
            {
                return new ReadOnlyDictionary<string, LocalizedPropertyInfo>(this.properties);
            }
        }

        /// <summary>
        /// Convert properties to array
        /// </summary>
        /// <returns></returns>
        public LocalizedPropertyInfo[] ConvertPropertiesToArray()
        {
            return this.properties == null ? new LocalizedPropertyInfo[] { } : this.properties.Values.ToArray();
        }

        /// <summary>
        /// Creates new BackupSet info
        /// </summary>
        /// <returns></returns>
        public static BackupSetInfo Create(Restore restore, Server server)
        {
            BackupSet backupSet = restore.BackupSet;
            Dictionary<string, LocalizedPropertyInfo> properties = new Dictionary<string, LocalizedPropertyInfo>();

            string bkSetComponent;
            string bkSetType;
            CommonUtilities.GetBackupSetTypeAndComponent(backupSet.BackupSetType, out bkSetType, out bkSetComponent);

            if (server.Version.Major > 8 && backupSet.IsCopyOnly)
            {
                bkSetType += SR.RestoreCopyOnly;
            }

            properties.Add(NamePropertyName, new LocalizedPropertyInfo
            {
                PropertyName = NamePropertyName,
                PropertyValue = backupSet.Name,
                PropertyDisplayName = SR.RestoreBackupSetName
            });
            properties.Add(BackupComponentPropertyName, new LocalizedPropertyInfo
            {
                PropertyName = BackupComponentPropertyName,
                PropertyValue = bkSetComponent,
                PropertyDisplayName = SR.RestoreBackupSetType
            });
            properties.Add(BackupTypePropertyName, new LocalizedPropertyInfo
            {
                PropertyName = BackupTypePropertyName,
                PropertyValue = bkSetType,
                PropertyDisplayName = SR.RestoreBackupSetComponent
            });
            properties.Add(ServerNamePropertyName, new LocalizedPropertyInfo
            {
                PropertyName = ServerNamePropertyName,
                PropertyValue = backupSet.ServerName,
                PropertyDisplayName = SR.RestoreBackupSetServer
            });
            properties.Add(DatabaseNamePropertyName, new LocalizedPropertyInfo
            {
                PropertyName = DatabaseNamePropertyName,
                PropertyValue = backupSet.DatabaseName,
                PropertyDisplayName = SR.RestoreBackupSetDatabase
            });
            properties.Add(PositionPropertyName, new LocalizedPropertyInfo
            {
                PropertyName = PositionPropertyName,
                PropertyValueDisplayName = Convert.ToString(backupSet.Position, CultureInfo.CurrentCulture),
                PropertyValue = backupSet.Position,
                PropertyDisplayName = SR.RestoreBackupSetPosition
            });
            properties.Add(FirstLsnPropertyName, new LocalizedPropertyInfo
            {
                PropertyName = FirstLsnPropertyName,
                PropertyValue = backupSet.FirstLsn,
                PropertyDisplayName = SR.RestoreBackupSetFirstLsn
            });
            properties.Add(LastLsnPropertyName, new LocalizedPropertyInfo
            {
                PropertyName = LastLsnPropertyName,
                PropertyValue = backupSet.LastLsn,
                PropertyDisplayName = SR.RestoreBackupSetLastLsn
            });
            properties.Add(FullLsnPropertyName, new LocalizedPropertyInfo
            {
                PropertyName = FullLsnPropertyName,
                PropertyValue = backupSet.DatabaseBackupLsn,
                PropertyDisplayName = SR.RestoreBackupSetFullLsn
            });
            properties.Add(CheckpointLsnPropertyName, new LocalizedPropertyInfo
            {
                PropertyName = CheckpointLsnPropertyName,
                PropertyValue = backupSet.CheckpointLsn,
                PropertyDisplayName = SR.RestoreBackupSetCheckpointLsn
            });
            properties.Add(StartDatePropertyName, new LocalizedPropertyInfo
            {
                PropertyName = StartDatePropertyName,
                PropertyValue = backupSet.BackupStartDate,
                PropertyDisplayName = SR.RestoreBackupSetStartDate
            });
            properties.Add(FinishDatePropertyName, new LocalizedPropertyInfo
            {
                PropertyName = FinishDatePropertyName,
                PropertyValue = backupSet.BackupFinishDate,
                PropertyDisplayName = SR.RestoreBackupSetFinishDate
            });
            properties.Add(SizePropertyName, new LocalizedPropertyInfo
            {
                PropertyName = SizePropertyName,
                PropertyValue = backupSet.BackupSize,
                PropertyDisplayName = SR.RestoreBackupSetSize,
            });
            properties.Add(UserNamePropertyName, new LocalizedPropertyInfo
            {
                PropertyName = UserNamePropertyName,
                PropertyValue = backupSet.UserName,
                PropertyDisplayName = SR.RestoreBackupSetUserName,
            });
            properties.Add(ExpirationPropertyName, new LocalizedPropertyInfo
            {
                PropertyName = ExpirationPropertyName,
                PropertyValue = backupSet.ExpirationDate,
                PropertyDisplayName = SR.RestoreBackupSetExpiration,
            });
            properties.Add(DatabaseFileInfo.IdPropertyName, new LocalizedPropertyInfo
            {
                PropertyName = DatabaseFileInfo.IdPropertyName,
                PropertyValue = backupSet.BackupSetGuid
            });

            return new BackupSetInfo(properties);
        }

        public string GetPropertyValueAsString(string propertyName)
        {
            LocalizedPropertyInfo propertyValue = null;
            if(!string.IsNullOrEmpty(propertyName) && Properties != null)
            {
                Properties.TryGetValue(propertyName, out propertyValue);
            }
            return propertyValue.PropertyValue != null ? propertyValue.PropertyValue.ToString() : string.Empty;
        }
    }
}
