//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.Collections;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{
    /// <summary>
    /// Provides database info for backup/recovery. Extends the default DatabaseInfo
    /// </summary>
    public class ExtendedDatabaseInfo: DatabaseInfo
    {
        public string RecoveryModel { get; set; }
        public ArrayList LatestBackups { get; set; }
        public string DefaultBackupFolder { get; set; }

        public ExtendedDatabaseInfo()
        {            
        }
    }
}
