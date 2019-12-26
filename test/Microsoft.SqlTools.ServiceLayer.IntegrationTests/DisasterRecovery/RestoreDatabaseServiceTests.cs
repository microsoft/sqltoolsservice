//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
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
        private string fullBackupFilePath;
        private string[] backupFilesToRecoverDatabase;
        private string databaseNameToRestoreFrom;

        //The table names used in the script to create backup files for a database
        //Each table is created after a backup script to verify recovering to different states
        private string[] tableNames = new string[] { "tb1", "tb2", "tb3", "tb4", "tb5" };

        public RestoreDatabaseServiceTests()
        {
            serviceHostMock = new Mock<IProtocolEndpoint>();
            service = CreateService();
            service.InitializeService(serviceHostMock.Object);
        }

        private async Task VerifyBackupFileCreated()
        {
            if(fullBackupFilePath == null)
            {
                fullBackupFilePath = await CreateBackupFile();
            }
        }

        private async Task<string[]> GetBackupFilesToRecoverDatabaseCreated()
        {
            if(backupFilesToRecoverDatabase == null)
            {
                backupFilesToRecoverDatabase = await CreateBackupSetsToRecoverDatabase();
            }
            return backupFilesToRecoverDatabase;
        }

        [Fact]
        public async void RestorePlanShouldCreatedSuccessfullyForFullBackup()
        {
            await VerifyBackupFileCreated();
            bool canRestore = true;
            await VerifyRestore(fullBackupFilePath, canRestore);
        }

        [Fact]
        public async void RestoreShouldNotRestoreAnyBackupSetsIfFullNotSelected()
        {
            var backupFiles = await GetBackupFilesToRecoverDatabaseCreated();
            //Remove the full backupset
            int indexToDelete = 0;
            //Verify that all backupsets are restored
            int[] expectedTable = new int[] { };

            await VerifyRestoreMultipleBackupSets(backupFiles, indexToDelete, expectedTable, TaskExecutionModeFlag.Execute);
        }

        [Fact]
        public async void RestoreShouldRestoreFromAnotherDatabase()
        {
            await GetBackupFilesToRecoverDatabaseCreated();

            var testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "RestoreTest");
            try
            {
                Dictionary<string, object> options = new Dictionary<string, object>();
                options.Add(RestoreOptionsHelper.ReplaceDatabase, true);
                await VerifyRestore(null, databaseNameToRestoreFrom, true, TaskExecutionModeFlag.ExecuteAndScript, testDb.DatabaseName, null, options, (database) =>
                {
                    return database.Tables.Contains("tb1", "test");
                });
            }
            finally
            {
                testDb.Cleanup();
            }
        }

        [Fact]
        public async void RestoreShouldFailIfThereAreOtherConnectionsToDatabase()
        {
            await GetBackupFilesToRecoverDatabaseCreated();

            var testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "RestoreTest");
            ConnectionService connectionService = LiveConnectionHelper.GetLiveTestConnectionService();
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                //Opening a connection to db to lock the db
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(testDb.DatabaseName, queryTempFile.FilePath, ConnectionType.Default);

                try
                {
                    bool restoreShouldFail = true;
                    Dictionary<string, object> options = new Dictionary<string, object>();
                    options.Add(RestoreOptionsHelper.ReplaceDatabase, true);
                    await VerifyRestore(null, databaseNameToRestoreFrom, true, TaskExecutionModeFlag.Execute, testDb.DatabaseName, null, options, null, restoreShouldFail);
                   
                }
                finally
                {
                    connectionService.Disconnect(new ServiceLayer.Connection.Contracts.DisconnectParams
                    {
                        OwnerUri = queryTempFile.FilePath,
                        Type = ConnectionType.Default
                    });
                    testDb.Cleanup();
                }
            }
        }

        [Fact]
        public async void RestoreShouldFailIfThereAreOtherConnectionsToDatabase2()
        {
            await GetBackupFilesToRecoverDatabaseCreated();

            var testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "RestoreTest");
            ConnectionService connectionService = LiveConnectionHelper.GetLiveTestConnectionService();
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {

                //OE connection will be closed after conneced
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(testDb.DatabaseName, queryTempFile.FilePath, ConnectionType.ObjectExplorer);
                //Opening a connection to db to lock the db
                ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo);

                try
                {
                    bool restoreShouldFail = true;
                    Dictionary<string, object> options = new Dictionary<string, object>();
                    options.Add(RestoreOptionsHelper.ReplaceDatabase, true);
                    await VerifyRestore(null, databaseNameToRestoreFrom, true, TaskExecutionModeFlag.Execute, testDb.DatabaseName, null, options, null, restoreShouldFail);
                    
                }
                finally
                {
                    connectionService.Disconnect(new ServiceLayer.Connection.Contracts.DisconnectParams
                    {
                        OwnerUri = queryTempFile.FilePath,
                        Type = ConnectionType.Default
                    });
                    testDb.Cleanup();
                }
            }
        }

        [Fact]
        public async void RestoreShouldCloseOtherConnectionsBeforeExecuting()
        {
            await GetBackupFilesToRecoverDatabaseCreated();

            var testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "RestoreTest");
            ConnectionService connectionService = LiveConnectionHelper.GetLiveTestConnectionService();
            TestConnectionResult connectionResult;
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {

                //OE connection will be closed after conneced
                connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(testDb.DatabaseName, queryTempFile.FilePath, ConnectionType.ObjectExplorer);
                //Opening a connection to db to lock the db

                connectionService.ConnectionQueue.AddConnectionContext(connectionResult.ConnectionInfo, "", true);

                try
                {
                    Dictionary<string, object> options = new Dictionary<string, object>();
                    options.Add(RestoreOptionsHelper.ReplaceDatabase, true);
                    await VerifyRestore(null, databaseNameToRestoreFrom, true, TaskExecutionModeFlag.Execute, testDb.DatabaseName, null, options
                        , (database) =>
                    {
                        return database.Tables.Contains("tb1", "test");
                    });
                }
                finally
                {
                    connectionService.Disconnect(new ServiceLayer.Connection.Contracts.DisconnectParams
                    {
                        OwnerUri = queryTempFile.FilePath,
                        Type = ConnectionType.Default
                    });
                    testDb.Cleanup();
                    
                }
            }
        }

        [Fact]
        public async void RestoreShouldRestoreTheBackupSetsThatAreSelected()
        {
            var backupFiles = await GetBackupFilesToRecoverDatabaseCreated();
            //Remove the last backupset
            int indexToDelete = 4;
            //Verify that backupset is not restored
            int[] expectedTable = new int[] { 0, 1, 2, 3 };

            await VerifyRestoreMultipleBackupSets(backupFiles, indexToDelete, expectedTable);
        }

        [Fact]
        public async void RestoreShouldNotRestoreTheLogBackupSetsIfOneNotSelected()
        {
            var backupFiles = await GetBackupFilesToRecoverDatabaseCreated();
            //Remove the one of the log backup sets
            int indexToDelete = 3;
            //Verify the logs backup set that's removed and all logs after that are not restored
            int[] expectedTable = new int[] { 0, 1, 2 };
            await VerifyRestoreMultipleBackupSets(backupFiles, indexToDelete, expectedTable);
        }

        private async Task VerifyRestoreMultipleBackupSets(string[] backupFiles, int backupSetIndexToDelete, int[] expectedSelectedIndexes, TaskExecutionModeFlag executionMode = TaskExecutionModeFlag.ExecuteAndScript)
        {
            var testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "RestoreTest");
            try
            {
                string targetDbName = testDb.DatabaseName;
                bool canRestore = true;
                var response = await VerifyRestore(backupFiles, null, canRestore, TaskExecutionModeFlag.None, targetDbName, null, null);
                Assert.True(response.BackupSetsToRestore.Count() >= 2);
                var allIds = response.BackupSetsToRestore.Select(x => x.Id).ToList();
                if (backupSetIndexToDelete >= 0)
                {
                    allIds.RemoveAt(backupSetIndexToDelete);
                }
                string[] selectedIds = allIds.ToArray();
                Dictionary<string, object> options = new Dictionary<string, object>();
                options.Add(RestoreOptionsHelper.ReplaceDatabase, true);
                response = await VerifyRestore(backupFiles, null, canRestore, executionMode, targetDbName, selectedIds, options, (database) =>
                {
                    if (executionMode.HasFlag(TaskExecutionModeFlag.Execute))
                    {
                        bool tablesFound = true;
                        for (int i = 0; i < tableNames.Length; i++)
                        {
                            string tableName = tableNames[i];
                            if (!database.Tables.Contains(tableName, "test") && expectedSelectedIndexes.Contains(i))
                            {
                                tablesFound = false;
                                break;
                            }
                        }
                        bool numberOfTableCreatedIsCorrect = database.Tables.Count == expectedSelectedIndexes.Length;
                        return numberOfTableCreatedIsCorrect && tablesFound;
                    }
                    return true;
                });

                for (int i = 0; i < response.BackupSetsToRestore.Count(); i++)
                {
                    DatabaseFileInfo databaseInfo = response.BackupSetsToRestore[i];
                    Assert.Equal(databaseInfo.IsSelected, expectedSelectedIndexes.Contains(i));
                }
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

                await VerifyRestore(new string[] { fullBackupFilePath }, null, canRestore, TaskExecutionModeFlag.ExecuteAndScript, testDb.DatabaseName, null, options);
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

                await VerifyRestore(new string[] { fullBackupFilePath }, null, canRestore, TaskExecutionModeFlag.None, testDb.DatabaseName, null, null);
            }
            finally
            {
                if (testDb != null)
                {
                    testDb.Cleanup();
                }
            }
        }

        //[Fact]
        public async void RestoreShouldCreatedSuccessfullyGivenTwoBackupFiles()
        {

            string[] backupFileNames = new string[] { "FullBackup.bak", "DiffBackup.bak" };
            bool canRestore = true;
            var response = await VerifyRestore(backupFileNames, null, canRestore, TaskExecutionModeFlag.None, "RestoredFromTwoBackupFile");
            Assert.True(response.BackupSetsToRestore.Count() == 2);
        }

        //[Fact]
        public async void RestoreShouldFailGivenTwoBackupFilesButFilterFullBackup()
        {

            string[] backupFileNames = new string[] { "FullBackup.bak", "DiffBackup.bak" };
            bool canRestore = true;
            var response = await VerifyRestore(backupFileNames, null, canRestore, TaskExecutionModeFlag.None, "RestoredFromTwoBackupFile");
            Assert.True(response.BackupSetsToRestore.Count() == 2);
            var fileInfo = response.BackupSetsToRestore.FirstOrDefault(x => x.GetPropertyValueAsString(BackupSetInfo.BackupTypePropertyName) != RestoreConstants.TypeFull);
            if(fileInfo != null)
            {
                var selectedBackupSets = new string[] { fileInfo.Id };
                await VerifyRestore(backupFileNames, null, true, TaskExecutionModeFlag.None, "RestoredFromTwoBackupFile", selectedBackupSets);
            }
        }

        //[Fact]
        public async void RestoreShouldCompletedSuccessfullyGivenTwoBackupFilesButFilterDifferentialBackup()
        {

            string[] backupFileNames = new string[] { "FullBackup.bak", "DiffBackup.bak" };
            bool canRestore = true;
            var response = await VerifyRestore(backupFileNames, null, canRestore, TaskExecutionModeFlag.None, "RestoredFromTwoBackupFile");
            Assert.True(response.BackupSetsToRestore.Count() == 2);
            var fileInfo = response.BackupSetsToRestore.FirstOrDefault(x => x.GetPropertyValueAsString(BackupSetInfo.BackupTypePropertyName) == RestoreConstants.TypeFull);
            if (fileInfo != null)
            {
                var selectedBackupSets = new string[] { fileInfo.Id };
                await VerifyRestore(backupFileNames, null, true, TaskExecutionModeFlag.None, "RestoredFromTwoBackupFile2", selectedBackupSets);
            }
        }

        [Fact]
        public async void RestoreShouldExecuteSuccessfullyForFullBackup()
        {
            await VerifyBackupFileCreated();

            string backupFileName = fullBackupFilePath;
            bool canRestore = true;
            var restorePlan = await VerifyRestore(backupFileName, canRestore, TaskExecutionModeFlag.Execute);
            Assert.NotNull(restorePlan.BackupSetsToRestore);
        }

        [Fact]
        public async void RestoreToAnotherDatabaseShouldExecuteSuccessfullyForFullBackup()
        {
            await VerifyBackupFileCreated();

            string backupFileName = fullBackupFilePath;
            bool canRestore = true;
            var restorePlan = await VerifyRestore(backupFileName, canRestore, TaskExecutionModeFlag.ExecuteAndScript, "NewRestoredDatabase");
        }

        //[Fact]
        public async void RestorePlanShouldCreatedSuccessfullyForDiffBackup()
        {
            string backupFileName = "DiffBackup.bak";
            bool canRestore = true;
            await VerifyRestore(backupFileName, canRestore);
        }

        //[Fact]
        public async void RestorePlanShouldCreatedSuccessfullyForTransactionLogBackup()
        {
            string backupFileName = "TransactionLogBackup.bak";
            bool canRestore = true;
            await VerifyRestore(backupFileName, canRestore);
        }

        [Fact]
        public async Task RestorePlanRequestShouldReturnResponseWithDbFiles()
        {
            await VerifyBackupFileCreated();

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                string filePath = GetBackupFilePath(fullBackupFilePath);

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
        public async Task CancelRestorePlanRequestShouldCancelSuccessfully()
        {
            await VerifyBackupFileCreated();

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                string filePath = GetBackupFilePath(fullBackupFilePath);

                RestoreParams restoreParams = new RestoreParams
                {
                    BackupFilePaths = filePath,
                    OwnerUri = queryTempFile.FilePath
                };

                await RunAndVerify<RestorePlanResponse>(
                    test: (requestContext) => service.HandleRestorePlanRequest(restoreParams, requestContext),
                    verify: ((result) =>
                    {
                        restoreParams.SessionId = result.SessionId;
                        Assert.True(result.DbFiles.Any());
                    }));

                await RunAndVerify<bool>(
                   test: (requestContext) => service.HandleCancelRestorePlanRequest(restoreParams, requestContext),
                   verify: ((result) =>
                   {
                       Assert.True(result);
                   }));
            }
        }

        [Fact]
        public async Task RestoreConfigInfoRequestShouldReturnResponse()
        {
            await VerifyBackupFileCreated();

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                string filePath = GetBackupFilePath(fullBackupFilePath);

                RestoreConfigInfoRequestParams restoreParams = new RestoreConfigInfoRequestParams
                {
                    OwnerUri = queryTempFile.FilePath
                };

                await RunAndVerify<RestoreConfigInfoResponse>(
                    test: (requestContext) => service.HandleRestoreConfigInfoRequest(restoreParams, requestContext),
                    verify: ((result) =>
                    {
                        Assert.True(result.ConfigInfo.Any());
                        Assert.True(result.ConfigInfo.ContainsKey(RestoreOptionsHelper.SourceDatabaseNamesWithBackupSets));
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

        private async Task<RestorePlanResponse> VerifyRestore(string backupFileName, bool canRestore, TaskExecutionModeFlag executionMode = TaskExecutionModeFlag.None, string targetDatabase = null)
        {
            return await VerifyRestore(new string[] { backupFileName }, null, canRestore, executionMode, targetDatabase);
        }

        private async Task<RestorePlanResponse> VerifyRestore(
            string[] backupFileNames = null, 
            string sourceDbName = null,
            bool canRestore = true,
            TaskExecutionModeFlag executionMode = TaskExecutionModeFlag.None, 
            string targetDatabase = null, 
            string[] selectedBackupSets = null,
            Dictionary<string, object> options = null,
            Func<Database, bool> verifyDatabase = null,
            bool shouldFail = false)
        {
            string backUpFilePath = string.Empty;
            if (backupFileNames != null)
            {
                var filePaths = backupFileNames.Select(x => GetBackupFilePath(x));
                backUpFilePath = filePaths.Aggregate((current, next) => current + " ," + next);
            }

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                RestoreDatabaseHelper service = new RestoreDatabaseHelper();

                // If source database is sepecified verfiy it's part of source db list
                if(!string.IsNullOrEmpty(sourceDbName))
                {
                    RestoreConfigInfoResponse configInfoResponse = service.CreateConfigInfoResponse(new RestoreConfigInfoRequestParams
                    {
                        OwnerUri = queryTempFile.FilePath
                    });
                    IEnumerable<string> dbNames = configInfoResponse.ConfigInfo[RestoreOptionsHelper.SourceDatabaseNamesWithBackupSets] as IEnumerable<string>;
                    Assert.True(dbNames.Any(x => x == sourceDbName));
                }
                var request = new RestoreParams
                {
                    BackupFilePaths = backUpFilePath,
                    TargetDatabaseName = targetDatabase,
                    OwnerUri = queryTempFile.FilePath,
                    SelectedBackupSets = selectedBackupSets,
                    SourceDatabaseName = sourceDbName
                };
                request.Options[RestoreOptionsHelper.ReadHeaderFromMedia] = backupFileNames != null;

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

                var restoreDataObject = service.CreateRestoreDatabaseTaskDataObject(request);
                restoreDataObject.ConnectionInfo = connectionResult.ConnectionInfo;
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
                    Assert.NotNull(response.PlanDetails);
                    Assert.True(response.PlanDetails.Any());
                    Assert.NotNull(response.PlanDetails[RestoreOptionsHelper.BackupTailLog]);
                    Assert.NotNull(response.PlanDetails[RestoreOptionsHelper.TailLogBackupFile]);
                    Assert.NotNull(response.PlanDetails[RestoreOptionsHelper.DataFileFolder]);
                    Assert.NotNull(response.PlanDetails[RestoreOptionsHelper.LogFileFolder]);
                    Assert.NotNull(response.PlanDetails[RestoreOptionsHelper.StandbyFile]);
                    Assert.NotNull(response.PlanDetails[RestoreOptionsHelper.StandbyFile]);
                   
                    if(executionMode != TaskExecutionModeFlag.None)
                    {
                        try
                        {
                            request.SessionId = response.SessionId;
                            restoreDataObject = service.CreateRestoreDatabaseTaskDataObject(request);
                            Assert.Equal(response.SessionId, restoreDataObject.SessionId);
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
                                    Assert.Equal(selectedBackupSets.Count(), restoreDataObject.RestorePlanToExecute.RestoreOperations.Count());
                                }
                            }
                            if(executionMode.HasFlag(TaskExecutionModeFlag.Script))
                            {
                                Assert.False(string.IsNullOrEmpty(restoreDataObject.ScriptContent));
                            }
                        }
                        catch(Exception ex)
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

        public async Task<string[]> CreateBackupSetsToRecoverDatabase()
        {
            List<string> backupFiles = new List<string>();
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                string query = $"CREATE SCHEMA [test]";
                SqlTestDb testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, query, "RestoreTest");
                string databaseName = testDb.DatabaseName;
                // Initialize backup service
                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName, queryTempFile.FilePath);
                DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
                {
                    BackupConfigInfo backupConfigInfo = DisasterRecoveryService.Instance.GetBackupConfigInfo(helper.DataContainer, sqlConn, sqlConn.Database);

                    query = $"create table [test].[{tableNames[0]}] (c1 int)";
                    await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, databaseName, query);
                    string backupPath = Path.Combine(backupConfigInfo.DefaultBackupFolder, databaseName + "_full.bak");
                    query = $"BACKUP DATABASE [{databaseName}] TO  DISK = N'{backupPath}' WITH NOFORMAT, NOINIT, NAME = N'{databaseName}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD,  STATS = 10";
                    await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, "master", query);
                    backupFiles.Add(backupPath);

                    query = $"create table [test].[{tableNames[1]}] (c1 int)";
                    await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, databaseName, query);
                    backupPath = Path.Combine(backupConfigInfo.DefaultBackupFolder, databaseName + "_diff.bak");
                    query = $"BACKUP DATABASE [{databaseName}] TO  DISK = N'{backupPath}' WITH DIFFERENTIAL, NOFORMAT, NOINIT, NAME = N'{databaseName}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD,  STATS = 10";
                    await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, "master", query);
                    backupFiles.Add(backupPath);

                    query = $"create table [test].[{tableNames[2]}] (c1 int)";
                    await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, databaseName, query);
                    backupPath = Path.Combine(backupConfigInfo.DefaultBackupFolder, databaseName + "_log1.bak");
                    query = $"BACKUP Log [{databaseName}] TO  DISK = N'{backupPath}' WITH NOFORMAT, NOINIT, NAME = N'{databaseName}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD,  STATS = 10";
                    await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, "master", query);
                    backupFiles.Add(backupPath);

                    query = $"create table [test].[{tableNames[3]}] (c1 int)";
                    await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, databaseName, query);
                    backupPath = Path.Combine(backupConfigInfo.DefaultBackupFolder, databaseName + "_log2.bak");
                    query = $"BACKUP Log [{databaseName}] TO  DISK = N'{backupPath}' WITH NOFORMAT, NOINIT, NAME = N'{databaseName}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD,  STATS = 10";
                    await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, "master", query);
                    backupFiles.Add(backupPath);

                    query = $"create table [test].[{tableNames[4]}] (c1 int)";
                    await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, databaseName, query);
                    backupPath = Path.Combine(backupConfigInfo.DefaultBackupFolder, databaseName + "_log3.bak");
                    query = $"BACKUP Log [{databaseName}] TO  DISK = N'{backupPath}' WITH NOFORMAT, NOINIT, NAME = N'{databaseName}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD,  STATS = 10";
                    await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, "master", query);
                    backupFiles.Add(backupPath);

                    databaseNameToRestoreFrom = testDb.DatabaseName;
                    // Clean up the database
                    testDb.Cleanup();
                }
            }
            return backupFiles.ToArray();

        }

        public async Task<string> CreateBackupFile()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                SqlTestDb testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "RestoreTest");
                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(testDb.DatabaseName, queryTempFile.FilePath);

                // Initialize backup service
                DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
                SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo);

                // Get default backup pathS
                BackupConfigInfo backupConfigInfo = DisasterRecoveryService.Instance.GetBackupConfigInfo(helper.DataContainer, sqlConn, sqlConn.Database);
                string backupPath = Path.Combine(backupConfigInfo.DefaultBackupFolder, testDb.DatabaseName + ".bak");

                BackupInfo backupInfo = CreateBackupInfo(testDb.DatabaseName,
                    BackupType.Full,
                    new List<string>() { backupPath },
                    new Dictionary<string, int>() { { backupPath, (int)DeviceType.File } });

                var backupParams = new BackupParams
                {
                    OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                    BackupInfo = backupInfo,
                    TaskExecutionMode = TaskExecutionMode.Execute
                };

                // Backup the database
                BackupOperation backupOperation = DisasterRecoveryService.Instance.CreateBackupOperation(helper.DataContainer, sqlConn, backupParams.BackupInfo);
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
