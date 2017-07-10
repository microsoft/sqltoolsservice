//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting.Protocol;
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

        public RestoreDatabaseServiceTests()
        {
            serviceHostMock = new Mock<IProtocolEndpoint>();
            service = CreateService();
            service.InitializeService(serviceHostMock.Object);
        }

        [Fact]
        public async void RestorePlanShouldCreatedSuccessfullyForFullBackup()
        {
            string backupFileName = "FullBackup.bak";
            bool canRestore = true;
            await VerifyRestore(backupFileName, canRestore);
        }

        [Fact]
        public async void RestoreShouldExecuteSuccessfullyForFullBackup()
        {
            string backupFileName = "FullBackup.bak";
            bool canRestore = true;
            var restorePlan = await VerifyRestore(backupFileName, canRestore, true);
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
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                string filePath = GetBackupFilePath("FullBackup.bak");

                RestoreParams restoreParams = new RestoreParams
                {
                    BackupFilePath = filePath,
                    OwnerUri = queryTempFile.FilePath
                };

                await RunAndVerify<RestorePlanResponse>(
                    test: (requestContext) => service.HandleRestorePlanRequest(restoreParams, requestContext),
                    verify: ((result) =>
                    {
                        Assert.True(result.DbFiles.Any());
                        Assert.Equal(result.DatabaseName, "BackupTestDb");
                    }));
            }
        }

        [Fact]
        public async Task RestoreDatabaseRequestShouldStartTheRestoreTask()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                string filePath = GetBackupFilePath("FullBackup.bak");

                RestoreParams restoreParams = new RestoreParams
                {
                    BackupFilePath = filePath,
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

        private async Task DropDatabase(string databaseName)
        {
            string dropDatabaseQuery = string.Format(CultureInfo.InvariantCulture,
                       Scripts.DropDatabaseIfExist, databaseName);

            await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, "master", dropDatabaseQuery);
        }

        private async Task<RestorePlanResponse> VerifyRestore(string backupFileName, bool canRestore, bool execute = false)
        {
            string filePath = GetBackupFilePath(backupFileName);
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                RestoreDatabaseService service = new RestoreDatabaseService();
                var request = new RestoreParams
                {
                    BackupFilePath = filePath,
                    DatabaseName = string.Empty,
                    OwnerUri = queryTempFile.FilePath
                };

                var restoreDataObject = service.CreateRestoreDatabaseTaskDataObject(request);
                var response = service.CreateRestorePlanResponse(restoreDataObject);

                Assert.NotNull(response);
                Assert.Equal(response.CanRestore, canRestore);
                if (canRestore)
                {
                    Assert.True(response.DbFiles.Any());
                    Assert.Equal(response.DatabaseName, "BackupTestDb");
                    if(execute)
                    {
                        await DropDatabase(response.DatabaseName);
                        Thread.Sleep(2000);
                        request.RelocateDbFiles = response.RelocateFilesNeeded;
                        service.ExecuteRestore(restoreDataObject);
                        Assert.True(restoreDataObject.Server.Databases.Contains(response.DatabaseName));
                        await DropDatabase(response.DatabaseName);
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
            FileInfo inputFile = GetBackupFile(fileName);
            return inputFile.FullName;
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
    }
}
