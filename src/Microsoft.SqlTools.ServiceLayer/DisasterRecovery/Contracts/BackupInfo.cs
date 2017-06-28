//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{    
    public class BackupInfo
    {
        /// <summary>
        /// Name of the datbase to perfom backup
        /// </summary>
        public string DatabaseName { get; set; }
        
        /// <summary>
        /// Component to backup - Database or Files
        /// </summary>
        public int BackupComponent { get; set; }

        /// <summary>
        /// Type of backup - Full/Differential/Log
        /// </summary>
        public int BackupType { get; set; }

        /// <summary>
        /// Backup device - Disk, Url, etc.
        /// </summary>
        public int BackupDeviceType { get; set; }

        /// <summary>
        /// The text input of selected files
        /// </summary>
        public string SelectedFiles { get; set; }

        /// <summary>
        /// Backupset name
        /// </summary>
        public string BackupsetName { get; set; }

        /// <summary>
        /// List of selected file groups
        /// </summary>
        public Dictionary<string, string> SelectedFileGroup { get; set; }

        /// <summary>
        /// List of {key: backup path, value: device type}
        /// </summary>        
        public Dictionary<string, int> BackupPathDevices { get; set; }
        
        /// <summary>
        /// List of selected backup paths
        /// </summary>        
        public List<string> BackupPathList { get; set; }
        
        /// <summary>
        /// Indicates if the backup should be copy-only
        /// </summary>
        public bool IsCopyOnly { get; set; }
    }
}
