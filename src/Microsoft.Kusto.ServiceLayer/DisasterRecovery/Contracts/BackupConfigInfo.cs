//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{
    /// <summary>
    /// Provides database info for backup.
    /// </summary>
    public class BackupConfigInfo
    {
        /// <summary>
        /// Gets or sets recovery model of a database
        /// </summary>
        public string RecoveryModel { get; set; }

        /// <summary>
        /// Gets or sets the default backup folder
        /// </summary>
        public string DefaultBackupFolder { get; set; }

        /// <summary>
        /// Gets or sets backup encryptors
        /// </summary>
        public List<BackupEncryptor> BackupEncryptors { get; set; }

        /// <summary>
        /// Ctor
        /// </summary>
        public BackupConfigInfo()
        {
        }
    }
}
