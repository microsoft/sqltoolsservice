﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    /// <summary>
    /// Interface for backup operations
    /// </summary>
    public interface IBackupOperation: IScriptableTaskOperation
    {
        /// <summary>
        /// Initialize 
        /// </summary>
        /// <param name="dataContainer"></param>
        /// <param name="sqlConnection"></param>
        void Initialize(CDataContainer dataContainer, SqlConnection sqlConnection);

        /// <summary>
        /// Return database metadata for backup
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        BackupConfigInfo CreateBackupConfigInfo(string databaseName);

        /// <summary>
        /// Set backup input properties
        /// </summary>
        /// <param name="input"></param>
        void SetBackupInput(BackupInfo input);
    }
}
