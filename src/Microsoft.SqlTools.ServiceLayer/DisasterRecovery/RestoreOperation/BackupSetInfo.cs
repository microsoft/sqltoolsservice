//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation
{
    /// <summary>
    /// Backup set Information
    /// </summary>
    public class BackupSetInfo
    {
        /// <summary>
        /// Backup type (Full, Transaction Log, Differential ...)
        /// </summary>
        public string BackupType { get; set; }

        /// <summary>
        /// Backup component (Database, File, Log ...)
        /// </summary>
        public string BackupComponent { get; set; }
    }
}
