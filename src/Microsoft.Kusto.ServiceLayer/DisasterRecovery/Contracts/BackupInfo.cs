//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.Kusto.ServiceLayer.DisasterRecovery.Contracts
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

        /// <summary>
        /// Gets or sets a Boolean property value that determines whether a media is formatted as the first step of the backup operation. 
        /// </summary>
        public bool FormatMedia { get; set; }

        /// <summary>
        /// Gets or sets a Boolean property value that determines whether the devices associated with a backup operation are initialized as part of the backup operation.
        /// </summary>
        public bool Initialize { get; set; }

        /// <summary>
        /// Gets or sets Boolean property that determines whether the tape header is read.
        /// </summary>
        public bool SkipTapeHeader { get; set; }

        /// <summary>
        /// Gets or sets the name used to identify a particular media set.
        /// </summary>
        public string MediaName { get; set; }

        /// <summary>
        /// Gets or sets a textual description of the medium that contains a backup set.
        /// </summary>
        public string MediaDescription { get; set; }

        /// <summary>
        /// Gets or sets a Boolean property value that determines whether a checksum value is calculated during backup or restore operations. 
        /// </summary>
        public bool Checksum { get; set; }

        /// <summary>
        /// Gets or sets a Boolean property value that determines whether the backup or restore continues after a checksum error occurs.
        /// </summary>
        public bool ContinueAfterError { get; set; }

        /// <summary>
        /// Gets or sets a Boolean property value that determines whether to truncate the database log.
        /// </summary>
        public bool LogTruncation { get; set; }

        /// <summary>
        /// Gets or sets a Boolean property value that determines whether to backup the tail of the log
        /// </summary>
        public bool TailLogBackup { get; set; }

        /// <summary>
        /// Gets or sets a textual description for a particular backup set. 
        /// </summary>
        public string BackupSetDescription { get; set; }

        /// <summary>
        /// Gets or sets the number of days that must elapse before a backup set can be overwritten. 
        /// </summary>
        public int RetainDays { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the backup set expires and the backup data is no longer considered relevant.
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the backup compression option.
        /// This should be converted to BackupCompressionOptions when setting it to Backup object.
        /// </summary>
        public int CompressionOption { get; set; }

        /// <summary>
        /// Gets or sets a Boolean property that determines whether verify is required.
        /// </summary>
        public bool VerifyBackupRequired { get; set; }

        /// <summary>
        /// Specifies the algorithm type used for backup encryption.
        /// This should be converted to BackupEncryptionAlgorithm when creating BackupEncryptionOptions object.
        /// </summary>
        public int EncryptionAlgorithm { get; set; }

        /// <summary>
        /// Specifies the encryptor type used to encrypt an encryption key.
        /// This should be converted to BackupEncryptorType when creating BackupEncryptionOptions object.
        /// </summary>
        public int EncryptorType { get; set; }

        /// <summary>
        /// Gets or sets the name of the encryptor.
        /// </summary>
        public string EncryptorName { get; set; }

    }
}
