//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.AzureBlob;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.DisasterRecovery
{
    class BackupRestoreUrlTests
    {
        /// <summary>
        /// Create simple backup test
        /// </summary>
        [Test]
        public void CreateBackupToUrlTest()
        {
            DisasterRecoveryService service = new DisasterRecoveryService();
            string databaseName = "SqlToolsService_TestBackupToUrl_" + new Random().Next(10000000, 99999999);

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName))
            {
                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
                using (DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true))
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
                {
                    ServerConnection serverConn = new ServerConnection(sqlConn);
                    Server server = new Server(serverConn);
                    SharedAccessSignatureCreator sasCreator = new SharedAccessSignatureCreator(server);
                    AzureBlobConnectionSetting azureBlobConnection = TestAzureBlobConnectionService.Intance.Settings;
                    sasCreator.CreateSqlSASCredential(azureBlobConnection.AccountName, azureBlobConnection.AccountKey, azureBlobConnection.BlobContainerUri, "");
                    string backupPath = GetAzureBlobBackupPath(databaseName);

                    BackupInfo backupInfo = CreateDefaultBackupInfo(databaseName,
                        BackupType.Full,
                        new List<string>() { backupPath },
                        new Dictionary<string, int>() { { backupPath, (int)DeviceType.Url } });
                    BackupOperation backupOperation = CreateBackupOperation(service, liveConnection.ConnectionInfo.OwnerUri, backupInfo, helper.DataContainer, sqlConn);

                    // Backup the database
                    service.PerformBackup(backupOperation);
                    
                    VerifyAndCleanAzureBlobBackup(databaseName);
                }
            }
        }

        private BackupInfo CreateDefaultBackupInfo(string databaseName, BackupType backupType, List<string> backupPathList, Dictionary<string, int> backupPathDevices)
        {
            BackupInfo backupInfo = new BackupInfo();
            backupInfo.BackupComponent = (int)BackupComponent.Database;
            backupInfo.BackupDeviceType = (int)BackupDeviceType.Url;
            backupInfo.BackupPathDevices = backupPathDevices;
            backupInfo.BackupPathList = backupPathList;
            backupInfo.BackupsetName = "default_backup";
            backupInfo.BackupType = (int)backupType;
            backupInfo.DatabaseName = databaseName;
            backupInfo.SelectedFileGroup = null;
            backupInfo.SelectedFiles = "";
            return backupInfo;
        }

        private string GetAzureBlobBackupPath(string databaseName)
        {
            AzureBlobConnectionSetting azureBlobConnection = TestAzureBlobConnectionService.Intance.Settings;
            return azureBlobConnection.BlobContainerUri + "/" + databaseName + ".bak";
        }

        private void VerifyAndCleanAzureBlobBackup(string databaseName)
        {
            AzureBlobConnectionSetting azureBlobConnection = TestAzureBlobConnectionService.Intance.Settings;
            string blobUri = GetAzureBlobBackupPath(databaseName);
            string accountKey = azureBlobConnection.AccountKey;
            string accountName = azureBlobConnection.AccountName;
            bool result = BlobDropIfExists(blobUri, accountName, accountKey);
            Assert.True(result, "Backup doesn't exists on Azure blob storage");
        }

        public static bool BlobDropIfExists(string blobUri, string accountName, string accountKey)
        {
            BlobClient client = new BlobClient(new Uri(blobUri), new StorageSharedKeyCredential(accountName, accountKey));
            return client.DeleteIfExists();
        }

        private BackupOperation CreateBackupOperation(DisasterRecoveryService service, string uri, BackupInfo backupInfo, CDataContainer dataContainer, SqlConnection sqlConn)
        {
            var backupParams = new BackupParams
            {
                OwnerUri = uri,
                BackupInfo = backupInfo,
            };

            return service.CreateBackupOperation(dataContainer, sqlConn, backupParams.BackupInfo);
        }
    }
}
