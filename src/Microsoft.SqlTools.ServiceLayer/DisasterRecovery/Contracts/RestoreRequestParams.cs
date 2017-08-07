//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{
    /// <summary>
    /// Restore request parameters
    /// </summary>
    public class RestoreParams : GeneralRequestDetails
    {
        /// <summary>
        /// Restore session id. The parameter is optional and if passed, an existing plan will be used
        /// </summary>
        internal string SessionId
        {
            get
            {
                return GetOptionValue<string>(RestoreOptionsHelper.SessionId);
            }
            set
            {
                SetOptionValue(RestoreOptionsHelper.SessionId, value);
            }
        }

        /// <summary>
        /// The Uri to find the connection to do the restore operations
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Comma delimited list of backup files
        /// </summary>
        internal string BackupFilePaths
        {
            get
            {
                return GetOptionValue<string>(RestoreOptionsHelper.BackupFilePaths);
            }
            set
            {
                SetOptionValue(RestoreOptionsHelper.BackupFilePaths, value);
            }
        }

        /// <summary>
        /// Target Database name to restore to
        /// </summary>
        internal string TargetDatabaseName
        {
            get
            {
                return GetOptionValue<string>(RestoreOptionsHelper.TargetDatabaseName);
            }
            set
            {
                SetOptionValue(RestoreOptionsHelper.TargetDatabaseName, value);
            }
        }

        /// <summary>
        /// Source Database name to restore from
        /// </summary>
        internal string SourceDatabaseName
        {
            get
            {
                return GetOptionValue<string>(RestoreOptionsHelper.SourceDatabaseName);
            }
            set
            {
                SetOptionValue(RestoreOptionsHelper.SourceDatabaseName, value);
            }
        }

        /// <summary>
        /// If set to true, the db files will be relocated to default data location in the server
        /// </summary>
        internal bool RelocateDbFiles
        {
            get
            {
                return GetOptionValue<bool>(RestoreOptionsHelper.RelocateDbFiles);
            }
            set
            {
                SetOptionValue(RestoreOptionsHelper.RelocateDbFiles, value);
            }
        }

        /// <summary>
        /// If set to true, the backup files will be used to create restore plan otehrwise the source database name will be used
        /// </summary>
        internal bool ReadHeaderFromMedia
        {
            get
            {
                //Default is true for now for backward compatibility
                return Options.ContainsKey(RestoreOptionsHelper.ReadHeaderFromMedia) ? GetOptionValue<bool>(RestoreOptionsHelper.ReadHeaderFromMedia) : true;
            }
            set
            {
                SetOptionValue(RestoreOptionsHelper.ReadHeaderFromMedia, value);
            }
        }

        /// <summary>
        /// Ids of the backup set to restore
        /// </summary>
        internal string[] SelectedBackupSets
        {
            get
            {
                return GetOptionValue<string[]>(RestoreOptionsHelper.SelectedBackupSets);
            }
            set
            {
                SetOptionValue(RestoreOptionsHelper.SelectedBackupSets, value);
            }
        }
    }

}
