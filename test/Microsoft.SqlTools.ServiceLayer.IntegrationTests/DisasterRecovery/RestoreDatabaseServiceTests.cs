//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests;
using Moq;
using Xunit;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.DisasterRecovery
{
    public class RestoreDatabaseServiceTests : ServiceTestBase
    {
        private ConnectionService _connectService = TestServiceProvider.Instance.ConnectionService;
        private Mock<IProtocolEndpoint> serviceHostMock;
        private DisasterRecoveryService service;
        private string fullBackUpDatabase;

        public RestoreDatabaseServiceTests()
        {
            serviceHostMock = new Mock<IProtocolEndpoint>();
            service = CreateService();
            service.InitializeService(serviceHostMock.Object);
        }

        private async Task VerifyBackupFileCreated()
        {
            if(fullBackUpDatabase == null)
            {
                fullBackUpDatabase = await CreateBackupFile();
            }
        }

        [Fact]
        public async void RestorePlanShouldCreatedSuccessfullyForFullBackup()
        {
            await VerifyBackupFileCreated();
            bool canRestore = true;
            await VerifyRestore(fullBackUpDatabase, canRestore);
        }

        [Fact]
        public async void RestorePlanShouldCreatedSuccessfullyOnExistingDatabaseGivenReplaceOption()
        {
            SqlTestDb testDb = null;
            try
            {
                testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "RestoreTest");
                //Create a backup from a test db but don't delete the database
                await VerifyBackupFileCreated();
                bool canRestore = true;
                Dictionary<string, object> options = new Dictionary<string, object>();
                options.Add(RestoreOptionsHelper.ReplaceDatabase, true);

                await VerifyRestore(new string[] { fullBackUpDatabase }, canRestore, true, testDb.DatabaseName, null, options);
            }
            finally
            {
                if (testDb != null)
                {
                    testDb.Cleanup();
                }
            }
        }

        [Fact]
        public async void RestorePlanShouldFailOnExistingDatabaseNotGivenReplaceOption()
        {
            SqlTestDb testDb = null;
            try
            {
                testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "RestoreTest");
                //Create a backup from a test db but don't delete the database
                await VerifyBackupFileCreated();
                bool canRestore = true;

                await VerifyRestore(new string[] { fullBackUpDatabase }, canRestore, false, testDb.DatabaseName, null, null);
            }
            finally
            {
                if (testDb != null)
                {
                    testDb.Cleanup();
                }
            }
        }

        [Fact]
        public async void RestoreShouldCreatedSuccessfullyGivenTwoBackupFiles()
        {

            string[] backupFileNames = new string[] { "FullBackup.bak", "DiffBackup.bak" };
            bool canRestore = true;
            var response = await VerifyRestore(backupFileNames, canRestore, false, "RestoredFromTwoBackupFile");
            Assert.True(response.BackupSetsToRestore.Count() == 2);
        }

        [Fact]
        public async void RestoreShouldFailGivenTwoBackupFilesButFilterFullBackup()
        {

            string[] backupFileNames = new string[] { "FullBackup.bak", "DiffBackup.bak" };
            bool canRestore = true;
            var response = await VerifyRestore(backupFileNames, canRestore, false, "RestoredFromTwoBackupFile");
            Assert.True(response.BackupSetsToRestore.Count() == 2);
            var fileInfo = response.BackupSetsToRestore.FirstOrDefault(x => x.GetPropertyValueAsString(BackupSetInfo.BackupTypePropertyName) != RestoreConstants.TypeFull);
            if(fileInfo != null)
            {
                var selectedBackupSets = new string[] { fileInfo.Id };
                await VerifyRestore(backupFileNames, false, false, "RestoredFromTwoBackupFile", selectedBackupSets);
            }
        }

        [Fact]
        public async void RestoreShouldCompletedSuccessfullyGivenTowBackupFilesButFilterDifferentialBackup()
        {

            string[] backupFileNames = new string[] { "FullBackup.bak", "DiffBackup.bak" };
            bool canRestore = true;
            var response = await VerifyRestore(backupFileNames, canRestore, false, "RestoredFromTwoBackupFile");
            Assert.True(response.BackupSetsToRestore.Count() == 2);
            var fileInfo = response.BackupSetsToRestore.FirstOrDefault(x => x.GetPropertyValueAsString(BackupSetInfo.BackupTypePropertyName) == RestoreConstants.TypeFull);
            if (fileInfo != null)
            {
                var selectedBackupSets = new string[] { fileInfo.Id };
                await VerifyRestore(backupFileNames, true, false, "RestoredFromTwoBackupFile2", selectedBackupSets);
            }
        }

        [Fact]
        public async void RestoreShouldExecuteSuccessfullyForFullBackup()
        {
            await VerifyBackupFileCreated();

            string backupFileName = fullBackUpDatabase;
            bool canRestore = true;
            var restorePlan = await VerifyRestore(backupFileName, canRestore, true);
            Assert.NotNull(restorePlan.BackupSetsToRestore);
        }

        [Fact]
        public async void RestoreToAnotherDatabaseShouldExecuteSuccessfullyForFullBackup()
        {
            await VerifyBackupFileCreated();

            string backupFileName = fullBackUpDatabase;
            bool canRestore = true;
            var restorePlan = await VerifyRestore(backupFileName, canRestore, true, "NewRestoredDatabase");
        }

        [Fact]
        public async void RestorePlanShouldFailForDiffBackup()
        {
            string backupFileName = "DiffBackup.bak";
            bool canRestore = false;
            await VerifyRestore(backupFileName, canRestore);
        }

        [Fact]
        public async void RestorePlanShouldFailForTransactionLogBackup()
        {
            string backupFileName = "TransactionLogBackup.bak";
            bool canRestore = false;
            await VerifyRestore(backupFileName, canRestore);
        }

        [Fact]
        public async Task RestorePlanRequestShouldReturnResponseWithDbFiles()
        {
            await VerifyBackupFileCreated();

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                string filePath = GetBackupFilePath(fullBackUpDatabase);

                RestoreParams restoreParams = new RestoreParams
                {
                    BackupFilePaths = filePath,
                    OwnerUri = queryTempFile.FilePath
                };

                await RunAndVerify<RestorePlanResponse>(
                    test: (requestContext) => service.HandleRestorePlanRequest(restoreParams, requestContext),
                    verify: ((result) =>
                    {
                        Assert.True(result.DbFiles.Any());
                    }));
            }
        }

        [Fact]
        public async Task RestoreDatabaseRequestShouldStartTheRestoreTask()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                string filePath = GetBackupFilePath("SomeFile.bak");

                RestoreParams restoreParams = new RestoreParams
                {
                    BackupFilePaths = filePath,
                    OwnerUri = queryTempFile.FilePath
                };

                await RunAndVerify<RestoreResponse>(
                    test: (requestContext) => service.HandleRestoreRequest(restoreParams, requestContext),
                    verify: ((result) =>
                    {
                        string taskId = result.TaskId;
                        var task = SqlTaskManager.Instance.Tasks.FirstOrDefault(x => x.TaskId.ToString() == taskId);
                        Assert.NotNull(task);

                    }));
            }
        }

        [Fact]
        public async Task RestorePlanRequestShouldReturnErrorMessageGivenInvalidFilePath()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                string filePath = GetBackupFilePath("InvalidFilePath");

                RestoreParams restoreParams = new RestoreParams
                {
                    BackupFilePaths = filePath,
                    OwnerUri = queryTempFile.FilePath
                };

                await RunAndVerify<RestorePlanResponse>(
                    test: (requestContext) => service.HandleRestorePlanRequest(restoreParams, requestContext),
                    verify: ((result) =>
                    {
                        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
                        Assert.False(result.CanRestore);
                    }));
            }
        }

        private async Task DropDatabase(string databaseName)
        {
            string dropDatabaseQuery = string.Format(CultureInfo.InvariantCulture,
                       Scripts.DropDatabaseIfExist, databaseName);

            await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, "master", dropDatabaseQuery);
        }

        private async Task<RestorePlanResponse> VerifyRestore(string backupFileName, bool canRestore, bool execute = false, string targetDatabase = null)
        {
            return await VerifyRestore(new string[] { backupFileName }, canRestore, execute, targetDatabase);
        }

        private async Task<RestorePlanResponse> VerifyRestore(
            string[] backupFileNames, 
            bool canRestore, 
            bool execute = false, 
            string targetDatabase = null, 
            string[] selectedBackupSets = null,
            Dictionary<string, object> options = null)
        {
            var filePaths = backupFileNames.Select(x => GetBackupFilePath(x));
            string backUpFilePath = filePaths.Aggregate((current, next) => current + " ," + next);


            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                RestoreDatabaseHelper service = new RestoreDatabaseHelper();
                var request = new RestoreParams
                {
                    BackupFilePaths = backUpFilePath,
                    TargetDatabaseName = targetDatabase,
                    OwnerUri = queryTempFile.FilePath,
                    SelectedBackupSets = selectedBackupSets
                };

                if(options != null)
                {
                    foreach (var item in options)
                    {
                        if (!request.Options.ContainsKey(item.Key))
                        {
                            request.Options.Add(item.Key, item.Value);
                        }
                    }
                }

                var restoreDataObject = service.CreateRestoreDatabaseTaskDataObject(request);
                var response = service.CreateRestorePlanResponse(restoreDataObject);

                Assert.NotNull(response);
                Assert.False(string.IsNullOrWhiteSpace(response.SessionId));
                Assert.Equal(response.CanRestore, canRestore);
                if (canRestore)
                {
                    Assert.True(response.DbFiles.Any());
                    if (string.IsNullOrEmpty(targetDatabase))
                    {
                        targetDatabase = response.DatabaseName;
                    }
                    Assert.Equal(response.DatabaseName, targetDatabase);
                   
                    if(execute)
                    {
                        request.SessionId = response.SessionId;
                        restoreDataObject = service.CreateRestoreDatabaseTaskDataObject(request);
                        Assert.Equal(response.SessionId, restoreDataObject.SessionId);
                        //await DropDatabase(targetDatabase);
                        //Thread.Sleep(2000);
                        request.RelocateDbFiles = !restoreDataObject.DbFilesLocationAreValid();
                        service.ExecuteRestore(restoreDataObject);
                        Assert.True(restoreDataObject.Server.Databases.Contains(targetDatabase));
                        if(selectedBackupSets != null)
                        {
                            Assert.Equal(selectedBackupSets.Count(), restoreDataObject.RestorePlan.RestoreOperations.Count());
                        }
                        await DropDatabase(targetDatabase);
                    }
                }

                return response;
            }
        }

        private static string TestLocationDirectory
        {
            get
            {
                return Path.Combine(RunEnvironmentInfo.GetTestDataLocation(), "DisasterRecovery");
            }
        }

        public DirectoryInfo BackupFileDirectory
        {
            get
            {
                string d = Path.Combine(TestLocationDirectory, "Backups");
                return new DirectoryInfo(d);
            }
        }

        public FileInfo GetBackupFile(string fileName)
        {
            return new FileInfo(Path.Combine(BackupFileDirectory.FullName, fileName));
        }

        private string GetBackupFilePath(string fileName)
        {
            if (!Path.IsPathRooted(fileName))
            {
                FileInfo inputFile = GetBackupFile(fileName);
                return inputFile.FullName;
            }
            else
            {
                return fileName;
            }
        }

        protected DisasterRecoveryService CreateService()
        {
            CreateServiceProviderWithMinServices();

            // Create the service using the service provider, which will initialize dependencies
            return ServiceProvider.GetService<DisasterRecoveryService>();
        }

        protected override RegisteredServiceProvider CreateServiceProviderWithMinServices()
        {
            return CreateProvider()
               .RegisterSingleService(new DisasterRecoveryService());
        }

        public async Task<string> CreateBackupFile()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                SqlTestDb testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "RestoreTest");
                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(testDb.DatabaseName, queryTempFile.FilePath);

                // Initialize backup service
                DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
                SqlConnection sqlConn = DisasterRecoveryService.GetSqlConnection(liveConnection.ConnectionInfo);

                // Get default backup path
                BackupConfigInfo backupConfigInfo = DisasterRecoveryService.Instance.GetBackupConfigInfo(helper.DataContainer, sqlConn, sqlConn.Database);
                string backupPath = Path.Combine(backupConfigInfo.DefaultBackupFolder, testDb.DatabaseName + ".bak");

                BackupInfo backupInfo = CreateBackupInfo(testDb.DatabaseName,
                    BackupType.Full,
                    new List<string>() { backupPath },
                    new Dictionary<string, int>() { { backupPath, (int)DeviceType.File } });

                var backupParams = new BackupParams
                {
                    OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                    BackupInfo = backupInfo
                };

                // Backup the database
                BackupOperation backupOperation = DisasterRecoveryService.Instance.SetBackupInput(helper.DataContainer, sqlConn, backupParams.BackupInfo);
                DisasterRecoveryService.Instance.PerformBackup(backupOperation);

                // Clean up the database
                testDb.Cleanup();
                return backupPath;
            }
        }

        private BackupInfo CreateBackupInfo(string databaseName, BackupType backupType, List<string> backupPathList, Dictionary<string, int> backupPathDevices)
        {
            BackupInfo backupInfo = new BackupInfo();
            backupInfo.BackupComponent = (int)BackupComponent.Database;
            backupInfo.BackupDeviceType = (int)BackupDeviceType.Disk;
            backupInfo.BackupPathDevices = backupPathDevices;
            backupInfo.BackupPathList = backupPathList;
            backupInfo.BackupsetName = "default_backup";
            backupInfo.BackupType = (int)backupType;
            backupInfo.DatabaseName = databaseName;
            backupInfo.SelectedFileGroup = null;
            backupInfo.SelectedFiles = "";
            return backupInfo;
        }
    }
}
