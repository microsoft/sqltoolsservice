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
        /// Full/Differential/Log
        /// </summary>
        public string BackupType { get; set; }

        /// <summary>
        /// Database or Files/Filegroups
        /// </summary>
        public string BackupComponent { get; set; }

        /// <summary>
        /// Disk or URL
        /// </summary>
        public string BackupDevice { get; set; }

        /// <summary>
        /// List of backup destination paths
        /// </summary>
        public Dictionary<string, int> BackupPathList { get; set; }
    }
}
