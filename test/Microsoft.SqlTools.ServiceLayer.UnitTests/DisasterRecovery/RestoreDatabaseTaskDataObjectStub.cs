//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DisasterRecovery
{
    public class RestoreDatabaseTaskDataObjectStub : IRestoreDatabaseTaskDataObject
    {
        public string DataFilesFolder { get; set; }

        public string DefaultDataFileFolder { get; set; }

        public bool RelocateAllFiles { get; set; }
        public string LogFilesFolder { get; set; }

        public string DefaultLogFileFolder { get; set; }

        public List<DbFile> DbFiles { get; set; }

        public RestoreOptions RestoreOptions { get; set; }

        public bool IsTailLogBackupPossible { get;  set; }

        public bool IsTailLogBackupWithNoRecoveryPossible { get;  set; }

        public bool TailLogWithNoRecovery { get; set; }
        public string TailLogBackupFile { get; set; }

        public RestorePlan RestorePlan { get;  set; }

        public bool CloseExistingConnections { get; set; }
        public RestoreParams RestoreParams { get; set; }
        public bool BackupTailLog { get; set; }

        public string DefaultStandbyFile { get; set; }

        public string DefaultTailLogbackupFile { get; set; }

        public string DefaultSourceDbName { get; set; }
        public string SourceDatabaseName { get; set; }

        public List<string> SourceDbNames { get; set; }

        public bool CanChangeTargetDatabase { get; set; }

        public string DefaultTargetDbName { get; set; }

        public string TargetDatabaseName { get; set; }
    }
}
