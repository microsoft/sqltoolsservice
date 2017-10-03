//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.SqlClient;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.FileBrowser;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.DisasterRecovery
{
    /// <summary>
    /// Integration tests for disaster recovery file validator
    /// </summary>
    public class DisasterRecoveryFileValidatorTests
    {
        [Fact]
        public void ValidateDefaultBackupFullFilePath()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
            DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
            SqlConnection sqlConn = LiveConnectionHelper.GetLiveTestConnectionService().OpenSqlConnection(liveConnection.ConnectionInfo);
            string backupPath = Path.Combine(GetDefaultBackupFolderPath(helper.DataContainer, sqlConn), "master.bak");

            string message;
            bool result = DisasterRecoveryFileValidator.ValidatePaths(new FileBrowserValidateEventArgs
            {
                ServiceType = FileValidationServiceConstants.Backup,
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                FilePaths = new string[] { backupPath }
            }, out message);

            Assert.True(result);
            Assert.Empty(message);
        }

        [Fact]
        public void ValidateDefaultBackupFolderPath()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
            DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
            SqlConnection sqlConn = LiveConnectionHelper.GetLiveTestConnectionService().OpenSqlConnection(liveConnection.ConnectionInfo);
            string backupPath = GetDefaultBackupFolderPath(helper.DataContainer, sqlConn);

            bool isFolder;
            bool result = DisasterRecoveryFileValidator.IsPathExisting(sqlConn, backupPath, out isFolder);
            Assert.True(isFolder);
            Assert.True(result);
        }

        [Fact]
        public void ValidatorShouldReturnFalseForInvalidPath()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
            DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
    
            string message;
            bool result = DisasterRecoveryFileValidator.ValidatePaths(new FileBrowserValidateEventArgs
            {
                ServiceType = FileValidationServiceConstants.Backup,
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                FilePaths = new string[] { Guid.NewGuid().ToString() }
            }, out message);

            Assert.False(result);
            Assert.True(!string.IsNullOrEmpty(message));
        }

        #region private methods

        private string GetDefaultBackupFolderPath(CDataContainer dataContainer, SqlConnection sqlConn)
        {
            DisasterRecoveryService service = new DisasterRecoveryService();
            BackupConfigInfo backupConfigInfo = service.GetBackupConfigInfo(dataContainer, sqlConn, sqlConn.Database);
            return backupConfigInfo.DefaultBackupFolder;
        }

        #endregion

    }
}
