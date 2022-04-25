//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.DisasterRecovery
{
    class BackupRestoreUrlTests
    {
        /// <summary>
        /// Create simple backup test
        /// </summary>
        [Test]
        public async Task BackupDatabaseToUrlAndRestoreFromUrlTest()
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
                    AzureBlobConnectionSetting azureBlobConnection = TestAzureBlobConnectionService.Instance.Settings;
                    sasCreator.CreateSqlSASCredential(azureBlobConnection.AccountName, azureBlobConnection.AccountKey, azureBlobConnection.BlobContainerUri, "");
                    string backupPath = GetAzureBlobBackupPath(databaseName);

                    BackupInfo backupInfo = CreateDefaultBackupInfo(databaseName,
                        BackupType.Full,
                        new List<string>() { backupPath },
                        new Dictionary<string, int>() { { backupPath, (int)DeviceType.Url } });
                    BackupOperation backupOperation = CreateBackupOperation(service, liveConnection.ConnectionInfo.OwnerUri, backupInfo, helper.DataContainer, sqlConn);

                    // Backup the database
                    service.PerformBackup(backupOperation);

                    testDb.Cleanup();
                }
            }

            await VerifyRestore(databaseName, true, TaskExecutionModeFlag.Execute, databaseName);

            VerifyAndCleanAzureBlobBackup(databaseName);
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
            AzureBlobConnectionSetting azureBlobConnection = TestAzureBlobConnectionService.Instance.Settings;
            return azureBlobConnection.BlobContainerUri + "/" + databaseName + ".bak";
        }

        private void VerifyAndCleanAzureBlobBackup(string databaseName)
        {
            AzureBlobConnectionSetting azureBlobConnection = TestAzureBlobConnectionService.Instance.Settings;
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

        private async Task<RestorePlanResponse> VerifyRestore(
            string sourceDbName = null,
            bool canRestore = true,
            TaskExecutionModeFlag executionMode = TaskExecutionModeFlag.None,
            string targetDatabase = null,
            string[] selectedBackupSets = null,
            Dictionary<string, object> options = null,
            Func<Database, bool> verifyDatabase = null,
            bool shouldFail = false)
        {
            string backUpFilePath = GetAzureBlobBackupPath(targetDatabase);

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, "master"))
            {
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", testDb.ConnectionString);

                RestoreDatabaseHelper service = new RestoreDatabaseHelper();

                // If source database is sepecified verfiy it's part of source db list
                if (!string.IsNullOrEmpty(sourceDbName))
                {
                    RestoreConfigInfoResponse configInfoResponse = service.CreateConfigInfoResponse(new RestoreConfigInfoRequestParams
                    {
                        OwnerUri = testDb.ConnectionString
                    });
                    IEnumerable<string> dbNames = configInfoResponse.ConfigInfo[RestoreOptionsHelper.SourceDatabaseNamesWithBackupSets] as IEnumerable<string>;
                    Assert.True(dbNames.Any(x => x == sourceDbName));
                }
                var request = new RestoreParams
                {
                    BackupFilePaths = backUpFilePath,
                    TargetDatabaseName = targetDatabase,
                    OwnerUri = testDb.ConnectionString,
                    SelectedBackupSets = selectedBackupSets,
                    SourceDatabaseName = sourceDbName,
                    DeviceType = (int)DeviceType.Url
                };
                request.Options[RestoreOptionsHelper.ReadHeaderFromMedia] = string.IsNullOrEmpty(backUpFilePath);

                if (options != null)
                {
                    foreach (var item in options)
                    {
                        if (!request.Options.ContainsKey(item.Key))
                        {
                            request.Options.Add(item.Key, item.Value);
                        }
                    }
                }

                var restoreDataObject = service.CreateRestoreDatabaseTaskDataObject(request, connectionResult.ConnectionInfo);
                restoreDataObject.ConnectionInfo = connectionResult.ConnectionInfo;
                var response = service.CreateRestorePlanResponse(restoreDataObject);

                Assert.NotNull(response);
                Assert.False(string.IsNullOrWhiteSpace(response.SessionId));
                Assert.AreEqual(response.CanRestore, canRestore);
                if (canRestore)
                {
                    Assert.True(response.DbFiles.Any());
                    if (string.IsNullOrEmpty(targetDatabase))
                    {
                        targetDatabase = response.DatabaseName;
                    }
                    Assert.AreEqual(response.DatabaseName, targetDatabase);
                    Assert.NotNull(response.PlanDetails);
                    Assert.True(response.PlanDetails.Any());
                    Assert.NotNull(response.PlanDetails[RestoreOptionsHelper.BackupTailLog]);
                    Assert.NotNull(response.PlanDetails[RestoreOptionsHelper.TailLogBackupFile]);
                    Assert.NotNull(response.PlanDetails[RestoreOptionsHelper.DataFileFolder]);
                    Assert.NotNull(response.PlanDetails[RestoreOptionsHelper.LogFileFolder]);
                    Assert.NotNull(response.PlanDetails[RestoreOptionsHelper.StandbyFile]);
                    Assert.NotNull(response.PlanDetails[RestoreOptionsHelper.StandbyFile]);

                    if (executionMode != TaskExecutionModeFlag.None)
                    {
                        try
                        {
                            request.SessionId = response.SessionId;
                            restoreDataObject = service.CreateRestoreDatabaseTaskDataObject(request);
                            Assert.AreEqual(response.SessionId, restoreDataObject.SessionId);
                            request.RelocateDbFiles = !restoreDataObject.DbFilesLocationAreValid();
                            restoreDataObject.Execute((TaskExecutionMode)Enum.Parse(typeof(TaskExecutionMode), executionMode.ToString()));

                            if (executionMode.HasFlag(TaskExecutionModeFlag.Execute))
                            {
                                Assert.True(restoreDataObject.Server.Databases.Contains(targetDatabase));

                                if (verifyDatabase != null)
                                {
                                    Assert.True(verifyDatabase(restoreDataObject.Server.Databases[targetDatabase]));
                                }

                                //To verify the backupset that are restored, verifying the database is a better options.
                                //Some tests still verify the number of backup sets that are executed which in some cases can be less than the selected list
                                if (verifyDatabase == null && selectedBackupSets != null)
                                {
                                    Assert.AreEqual(selectedBackupSets.Count(), restoreDataObject.RestorePlanToExecute.RestoreOperations.Count());
                                }
                            }
                            if (executionMode.HasFlag(TaskExecutionModeFlag.Script))
                            {
                                Assert.False(string.IsNullOrEmpty(restoreDataObject.ScriptContent));
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!shouldFail)
                            {
                                Assert.False(true, ex.Message);
                            }
                        }
                        finally
                        {
                            await DropDatabase(targetDatabase);
                        }
                    }
                }

                return response;
            }
        }

        private async Task DropDatabase(string databaseName)
        {
            string dropDatabaseQuery = string.Format(CultureInfo.InvariantCulture,
                       Scripts.DropDatabaseIfExist, databaseName);

            await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, "master", dropDatabaseQuery);
        }
    }
}
