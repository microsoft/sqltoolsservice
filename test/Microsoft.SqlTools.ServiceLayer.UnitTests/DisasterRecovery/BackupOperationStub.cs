//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using System.Data.SqlClient;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DisasterRecovery
{
    /// <summary>
    /// Stub class that implements IBackupOperation
    /// </summary>
    public class BackupOperationStub : IBackupOperation
    {
        /// <summary>
        /// Initialize 
        /// </summary>
        /// <param name="dataContainer"></param>
        /// <param name="sqlConnection"></param>
        public void Initialize(CDataContainer dataContainer, SqlConnection sqlConnection)
        {
        }

        /// <summary>
        /// Return database metadata for backup
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public BackupConfigInfo GetBackupConfigInfo(string databaseName)
        {
            return null;
        }

        /// <summary>
        /// Set backup input properties
        /// </summary>
        /// <param name="input"></param>
        public void SetBackupInput(BackupInfo input)
        {
        }

        /// <summary>
        /// Execute backup
        /// </summary>
        public void PerformBackup()
        {
            Thread.Sleep(500);
        }

        /// <summary>
        /// Cancel backup
        /// </summary>
        public void CancelBackup()
        {
        }
    }
}
