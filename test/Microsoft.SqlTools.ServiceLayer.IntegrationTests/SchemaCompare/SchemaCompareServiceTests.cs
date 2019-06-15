//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Moq;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SchemaCompare
{
    public class SchemaCompareServiceTests
    {
        private const string SourceScript = @"CREATE TABLE [dbo].[table1]
(
    [ID] INT NOT NULL PRIMARY KEY,
    [Date] DATE NOT NULL
)
CREATE TABLE [dbo].[table2]
(
    [ID] INT NOT NULL PRIMARY KEY,
    [col1] NCHAR(10) NULL
)";

        private const string TargetScript = @"CREATE TABLE [dbo].[table2]
(
    [ID] INT NOT NULL PRIMARY KEY,
    [col1] NCHAR(10) NULL,
    [col2] NCHAR(10) NULL
)
CREATE TABLE [dbo].[table3]
(
    [ID] INT NOT NULL PRIMARY KEY,
    [col1] INT NULL,
)";

        /// <summary>
        /// Verify the schema compare request comparing two dacpacs
        /// </summary>
        [Fact]
        public async void SchemaCompareDacpacToDacpac()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();

            // create dacpacs from databases
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");
            try
            {
                string sourceDacpacFilePath = SchemaCompareTestUtils.CreateDacpac(sourceDb);
                string targetDacpacFilePath = SchemaCompareTestUtils.CreateDacpac(targetDb);

                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                sourceInfo.PackageFilePath = sourceDacpacFilePath;
                targetInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                targetInfo.PackageFilePath = targetDacpacFilePath;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, null, null);
                ValidateSchemaCompareWithExcludeIncludeResults(schemaCompareOperation);

                // cleanup
                SchemaCompareTestUtils.VerifyAndCleanup(sourceDacpacFilePath);
                SchemaCompareTestUtils.VerifyAndCleanup(targetDacpacFilePath);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the schema compare request comparing a two databases
        /// </summary>
        [Fact]
        public async void SchemaCompareDatabaseToDatabase()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SchemaCompareTest");
            Directory.CreateDirectory(folderPath);

            try
            {
                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Database;
                sourceInfo.DatabaseName = sourceDb.DatabaseName;
                targetInfo.EndpointType = SchemaCompareEndpointType.Database;
                targetInfo.DatabaseName = targetDb.DatabaseName;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, result.ConnectionInfo);
                ValidateSchemaCompareWithExcludeIncludeResults(schemaCompareOperation);
            }
            finally
            {
                // cleanup
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the schema compare request comparing a database to a dacpac
        /// </summary>
        [Fact]
        public async void SchemaCompareDatabaseToDacpac()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");

            try
            {
                string targetDacpacFilePath = SchemaCompareTestUtils.CreateDacpac(targetDb);

                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Database;
                sourceInfo.DatabaseName = sourceDb.DatabaseName;
                targetInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                targetInfo.PackageFilePath = targetDacpacFilePath;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, null);
                ValidateSchemaCompareWithExcludeIncludeResults(schemaCompareOperation);

                // cleanup
                SchemaCompareTestUtils.VerifyAndCleanup(targetDacpacFilePath);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the schema compare generate script request comparing a database to a database
        /// </summary>
        [Fact]
        public async void SchemaCompareGenerateScriptDatabaseToDatabase()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");

            try
            {
                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Database;
                sourceInfo.DatabaseName = sourceDb.DatabaseName;
                targetInfo.EndpointType = SchemaCompareEndpointType.Database;
                targetInfo.DatabaseName = targetDb.DatabaseName;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, result.ConnectionInfo);

                // generate script params
                var generateScriptParams = new SchemaCompareGenerateScriptParams
                {
                    TargetDatabaseName = targetDb.DatabaseName,
                    OperationId = schemaCompareOperation.OperationId,
                };

                ValidateSchemaCompareScriptGenerationWithExcludeIncludeResults(schemaCompareOperation, generateScriptParams);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the schema compare generate script request comparing a dacpac to a database
        /// </summary>
        [Fact]
        public async void SchemaCompareGenerateScriptDacpacToDatabase()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SchemaCompareTest");
            Directory.CreateDirectory(folderPath);

            try
            {
                string sourceDacpacFilePath = SchemaCompareTestUtils.CreateDacpac(sourceDb);

                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                sourceInfo.PackageFilePath = sourceDacpacFilePath;
                targetInfo.EndpointType = SchemaCompareEndpointType.Database;
                targetInfo.DatabaseName = targetDb.DatabaseName;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, result.ConnectionInfo);

                // generate script
                var generateScriptParams = new SchemaCompareGenerateScriptParams
                {
                    TargetDatabaseName = targetDb.DatabaseName,
                    OperationId = schemaCompareOperation.OperationId,
                };

                ValidateSchemaCompareScriptGenerationWithExcludeIncludeResults(schemaCompareOperation, generateScriptParams);

                // cleanup
                SchemaCompareTestUtils.VerifyAndCleanup(sourceDacpacFilePath);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the schema compare publish changes request comparing a dacpac to a database
        /// </summary>
        [Fact]
        public async void SchemaComparePublishChangesDacpacToDatabase()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "SchemaCompareTarget");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SchemaCompareTest");
            Directory.CreateDirectory(folderPath);

            try
            {
                string sourceDacpacFilePath = SchemaCompareTestUtils.CreateDacpac(sourceDb);

                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                sourceInfo.PackageFilePath = sourceDacpacFilePath;
                targetInfo.EndpointType = SchemaCompareEndpointType.Database;
                targetInfo.DatabaseName = targetDb.DatabaseName;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, result.ConnectionInfo);
                schemaCompareOperation.Execute(TaskExecutionMode.Execute);

                Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
                Assert.False(schemaCompareOperation.ComparisonResult.IsEqual);
                Assert.NotNull(schemaCompareOperation.ComparisonResult.Differences);
                var enumerator = schemaCompareOperation.ComparisonResult.Differences.GetEnumerator();
                enumerator.MoveNext();
                Assert.True(enumerator.Current.SourceObject.Name.ToString().Equals("[dbo].[table1]"));
                enumerator.MoveNext();
                Assert.True(enumerator.Current.SourceObject.Name.ToString().Equals("[dbo].[table2]"));

                // update target
                var publishChangesParams = new SchemaComparePublishChangesParams
                {
                    TargetDatabaseName = targetDb.DatabaseName,
                    OperationId = schemaCompareOperation.OperationId,
                };

                SchemaComparePublishChangesOperation publishChangesOperation = new SchemaComparePublishChangesOperation(publishChangesParams, schemaCompareOperation.ComparisonResult);
                publishChangesOperation.Execute(TaskExecutionMode.Execute);
                Assert.True(publishChangesOperation.PublishResult.Success);
                Assert.Empty(publishChangesOperation.PublishResult.Errors);

                // Verify that there are no differences after the publish by running the comparison again
                schemaCompareOperation.Execute(TaskExecutionMode.Execute);

                Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
                Assert.True(schemaCompareOperation.ComparisonResult.IsEqual);
                Assert.Empty(schemaCompareOperation.ComparisonResult.Differences);

                // cleanup
                SchemaCompareTestUtils.VerifyAndCleanup(sourceDacpacFilePath);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the schema compare publish changes request comparing a database to a database
        /// </summary>
        [Fact]
        public async void SchemaComparePublishChangesDatabaseToDatabase()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "SchemaCompareTarget");

            try
            {
                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Database;
                sourceInfo.DatabaseName = sourceDb.DatabaseName;
                targetInfo.EndpointType = SchemaCompareEndpointType.Database;
                targetInfo.DatabaseName = targetDb.DatabaseName;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, result.ConnectionInfo);
                schemaCompareOperation.Execute(TaskExecutionMode.Execute);

                Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
                Assert.False(schemaCompareOperation.ComparisonResult.IsEqual);
                Assert.NotNull(schemaCompareOperation.ComparisonResult.Differences);
                var enumerator = schemaCompareOperation.ComparisonResult.Differences.GetEnumerator();
                enumerator.MoveNext();
                Assert.True(enumerator.Current.SourceObject.Name.ToString().Equals("[dbo].[table1]"));
                enumerator.MoveNext();
                Assert.True(enumerator.Current.SourceObject.Name.ToString().Equals("[dbo].[table2]"));

                // update target
                var publishChangesParams = new SchemaComparePublishChangesParams
                {
                    TargetDatabaseName = targetDb.DatabaseName,
                    OperationId = schemaCompareOperation.OperationId,
                };

                SchemaComparePublishChangesOperation publishChangesOperation = new SchemaComparePublishChangesOperation(publishChangesParams, schemaCompareOperation.ComparisonResult);
                publishChangesOperation.Execute(TaskExecutionMode.Execute);
                Assert.True(publishChangesOperation.PublishResult.Success);
                Assert.Empty(publishChangesOperation.PublishResult.Errors);

                // Verify that there are no differences after the publish by running the comparison again
                schemaCompareOperation.Execute(TaskExecutionMode.Execute);

                Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
                Assert.True(schemaCompareOperation.ComparisonResult.IsEqual);
                Assert.Empty(schemaCompareOperation.ComparisonResult.Differences);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the schema compare Scmp File Save for database endpoints
        /// </summary>
        [Fact]
        public async void SchemaCompareSaveScmpFileForDatabases()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");

            try
            {
                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Database;
                sourceInfo.DatabaseName = sourceDb.DatabaseName;
                targetInfo.EndpointType = SchemaCompareEndpointType.Database;
                targetInfo.DatabaseName = targetDb.DatabaseName;

                CreateAndValidateScmpFile(sourceInfo, targetInfo, true, true);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the schema compare Scmp File Save for dacpac endpoints
        /// </summary>
        [Fact]
        public async void SchemaCompareSaveScmpFileForDacpacs()
        {
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");

            try
            {
                string sourceDacpac = SchemaCompareTestUtils.CreateDacpac(sourceDb);
                string targetDacpac = SchemaCompareTestUtils.CreateDacpac(targetDb);
                string filePath = SchemaCompareTestUtils.CreateScmpPath();

                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                sourceInfo.PackageFilePath = sourceDacpac;
                targetInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                targetInfo.PackageFilePath = targetDacpac;

                CreateAndValidateScmpFile(sourceInfo, targetInfo, false, false);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the schema compare Scmp File Save for dacpac and db endpoints combination
        /// </summary>
        [Fact]
        public async void SchemaCompareSaveScmpFileForDacpacToDB()
        {
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");

            try
            {
                string sourceDacpac = SchemaCompareTestUtils.CreateDacpac(sourceDb);
                string filePath = SchemaCompareTestUtils.CreateScmpPath();

                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                sourceInfo.PackageFilePath = sourceDacpac;
                targetInfo.EndpointType = SchemaCompareEndpointType.Database;
                targetInfo.DatabaseName = targetDb.DatabaseName;

                CreateAndValidateScmpFile(sourceInfo, targetInfo, false, true);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        /// <summary>
        /// Verify opening an scmp comparing two databases
        /// </summary>
        [Fact]
        public async void SchemaCompareOpenScmpDatabaseToDatabaseRequest()
        {
            await CreateAndOpenScmp(SchemaCompareEndpointType.Database, SchemaCompareEndpointType.Database);
        }

        /// <summary>
        /// Verify opening an scmp comparing a dacpac and database
        /// </summary>
        [Fact]
        public async void SchemaCompareOpenScmpDacpacToDatabaseRequest()
        {
            await CreateAndOpenScmp(SchemaCompareEndpointType.Dacpac, SchemaCompareEndpointType.Database);
        }

        /// <summary>
        /// Verify opening an scmp comparing two dacpacs
        /// </summary>
        [Fact]
        public async void SchemaCompareOpenScmpDacpacToDacpacRequest()
        {
            await CreateAndOpenScmp(SchemaCompareEndpointType.Dacpac, SchemaCompareEndpointType.Dacpac);
        }

        /// <summary>
        /// Verify the schema compare Service Calls ends to end
        /// </summary>
        [Fact]
        public async Task VerifySchemaCompareServiceCalls()
        {
            string operationId = null;
            DiffEntry diffEntry = null;
            var connectionObject = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");

            try
            {
                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Database;
                sourceInfo.DatabaseName = sourceDb.DatabaseName;
                sourceInfo.OwnerUri = connectionObject.ConnectionInfo.OwnerUri;
                targetInfo.EndpointType = SchemaCompareEndpointType.Database;
                targetInfo.DatabaseName = targetDb.DatabaseName;
                targetInfo.OwnerUri = connectionObject.ConnectionInfo.OwnerUri;

                // Schema compare service call
                var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
                schemaCompareRequestContext.Setup((RequestContext<SchemaCompareResult> x) => x.SendResult(It.Is<SchemaCompareResult>((diffResult) =>
                ValidateScResult(diffResult, ref diffEntry, ref operationId)))).Returns(Task.FromResult(new object()));

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo,
                    DeploymentOptions = new DeploymentOptions()
                };

                await SchemaCompareService.Instance.HandleSchemaCompareRequest(schemaCompareParams, schemaCompareRequestContext.Object);
                await SchemaCompareService.Instance.CurrentSchemaCompareTask;

                // Generate script Service call
                var generateScriptRequestContext = new Mock<RequestContext<ResultStatus>>();
                generateScriptRequestContext.Setup((RequestContext<ResultStatus> x) => x.SendResult(It.Is<ResultStatus>((result) => result.Success == true))).Returns(Task.FromResult(new object()));

                var generateScriptParams = new SchemaCompareGenerateScriptParams
                {
                    OperationId = operationId,
                    TargetDatabaseName = targetDb.DatabaseName,
                    TargetServerName = "My server"
                };

                await SchemaCompareService.Instance.HandleSchemaCompareGenerateScriptRequest(generateScriptParams, generateScriptRequestContext.Object);

                // Publish service call
                var publishRequestContext = new Mock<RequestContext<ResultStatus>>();
                publishRequestContext.Setup((RequestContext<ResultStatus> x) => x.SendResult(It.Is<ResultStatus>((result) => result.Success == true))).Returns(Task.FromResult(new object()));


                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(targetDb.ConnectionString);
                var publishParams = new SchemaCompareGenerateScriptParams
                {
                    OperationId = operationId,
                    TargetDatabaseName = targetDb.DatabaseName,
                    TargetServerName = builder.DataSource,
                };

                await SchemaCompareService.Instance.HandleSchemaCompareGenerateScriptRequest(publishParams, publishRequestContext.Object);

                // Include/Exclude service call
                var excludeRequestContext = new Mock<RequestContext<ResultStatus>>();
                excludeRequestContext.Setup((RequestContext<ResultStatus> x) => x.SendResult(It.Is<ResultStatus>((result) => result.Success == true))).Returns(Task.FromResult(new object()));

                var excludeParams = new SchemaCompareNodeParams
                {
                    OperationId = operationId,
                    DiffEntry = diffEntry
                };

                await SchemaCompareService.Instance.HandleSchemaCompareIncludeExcludeNodeRequest(excludeParams, publishRequestContext.Object);

                // Save Scmp service call
                var saveScmpRequestContext = new Mock<RequestContext<ResultStatus>>();
                saveScmpRequestContext.Setup((RequestContext<ResultStatus> x) => x.SendResult(It.Is<ResultStatus>((result) => result.Success == true))).Returns(Task.FromResult(new object()));
                var scmpFilePath = SchemaCompareTestUtils.CreateScmpPath();

                var saveScmpParams = new SchemaCompareSaveScmpParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo,
                    DeploymentOptions = new DeploymentOptions(),
                    ScmpFilePath = scmpFilePath
                };

                await SchemaCompareService.Instance.HandleSchemaCompareSaveScmpRequest(saveScmpParams, publishRequestContext.Object);
                await SchemaCompareService.Instance.CurrentSchemaCompareTask;
                SchemaCompareTestUtils.VerifyAndCleanup(scmpFilePath);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the schema compare cancel 
        /// </summary>
        [Fact]
        public async void SchemaCompareCancelCompareOperation()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");

            try
            {
                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Database;
                sourceInfo.DatabaseName = sourceDb.DatabaseName;
                targetInfo.EndpointType = SchemaCompareEndpointType.Database;
                targetInfo.DatabaseName = targetDb.DatabaseName;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, result.ConnectionInfo);
                schemaCompareOperation.schemaCompareStarted += (sender, e) => { schemaCompareOperation.Cancel(); };

                try
                {
                    Task cTask = Task.Factory.StartNew(() => schemaCompareOperation.Execute(TaskExecutionMode.Execute));
                    cTask.Wait();
                    Assert.False(cTask.IsCompletedSuccessfully, "schema compare task should not complete after cancel");
                }
                catch (Exception ex)
                {
                    Assert.NotNull(ex.InnerException);
                    Assert.True(ex.InnerException is OperationCanceledException, $"Exception is expected to be Operation cancelled but actually is {ex.InnerException}");
                }

                Assert.Null(schemaCompareOperation.ComparisonResult.Differences);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        private void ValidateSchemaCompareWithExcludeIncludeResults(SchemaCompareOperation schemaCompareOperation)
        {
            schemaCompareOperation.Execute(TaskExecutionMode.Execute);

            Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
            Assert.False(schemaCompareOperation.ComparisonResult.IsEqual);
            Assert.NotNull(schemaCompareOperation.ComparisonResult.Differences);

            // create Diff Entry from Difference
            DiffEntry diff = SchemaCompareUtils.CreateDiffEntry(schemaCompareOperation.ComparisonResult.Differences.First(), null);

            int initial = schemaCompareOperation.ComparisonResult.Differences.Count();
            SchemaCompareNodeParams schemaCompareExcludeNodeParams = new SchemaCompareNodeParams()
            {
                OperationId = schemaCompareOperation.OperationId,
                DiffEntry = diff,
                IncludeRequest = false,
                TaskExecutionMode = TaskExecutionMode.Execute
            };
            SchemaCompareIncludeExcludeNodeOperation nodeExcludeOperation = new SchemaCompareIncludeExcludeNodeOperation(schemaCompareExcludeNodeParams, schemaCompareOperation.ComparisonResult);
            nodeExcludeOperation.Execute(TaskExecutionMode.Execute);

            int afterExclude = schemaCompareOperation.ComparisonResult.Differences.Count();

            Assert.True(initial == afterExclude, $"Changes should be same again after excluding/including, before {initial}, now {afterExclude}");

            SchemaCompareNodeParams schemaCompareincludeNodeParams = new SchemaCompareNodeParams()
            {
                OperationId = schemaCompareOperation.OperationId,
                DiffEntry = diff,
                IncludeRequest = true,
                TaskExecutionMode = TaskExecutionMode.Execute
            };

            SchemaCompareIncludeExcludeNodeOperation nodeIncludeOperation = new SchemaCompareIncludeExcludeNodeOperation(schemaCompareincludeNodeParams, schemaCompareOperation.ComparisonResult);
            nodeIncludeOperation.Execute(TaskExecutionMode.Execute);
            int afterInclude = schemaCompareOperation.ComparisonResult.Differences.Count();


            Assert.True(initial == afterInclude, $"Changes should be same again after excluding/including, before:{initial}, now {afterInclude}");
        }

        private void ValidateSchemaCompareScriptGenerationWithExcludeIncludeResults(SchemaCompareOperation schemaCompareOperation, SchemaCompareGenerateScriptParams generateScriptParams)
        {
            schemaCompareOperation.Execute(TaskExecutionMode.Execute);

            Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
            Assert.False(schemaCompareOperation.ComparisonResult.IsEqual);
            Assert.NotNull(schemaCompareOperation.ComparisonResult.Differences);

            SchemaCompareGenerateScriptOperation generateScriptOperation = new SchemaCompareGenerateScriptOperation(generateScriptParams, schemaCompareOperation.ComparisonResult);
            generateScriptOperation.Execute(TaskExecutionMode.Script);

            Assert.True(generateScriptOperation.ScriptGenerationResult.Success);
            string initialScript = generateScriptOperation.ScriptGenerationResult.Script;

            // create Diff Entry from on Difference
            DiffEntry diff = SchemaCompareUtils.CreateDiffEntry(schemaCompareOperation.ComparisonResult.Differences.First(), null);

            //Validate Diff Entry creation for object type
            ValidateDiffEntryCreation(diff, schemaCompareOperation.ComparisonResult.Differences.First());

            int initial = schemaCompareOperation.ComparisonResult.Differences.Count();
            SchemaCompareNodeParams schemaCompareExcludeNodeParams = new SchemaCompareNodeParams()
            {
                OperationId = schemaCompareOperation.OperationId,
                DiffEntry = diff,
                IncludeRequest = false,
                TaskExecutionMode = TaskExecutionMode.Execute
            };
            SchemaCompareIncludeExcludeNodeOperation nodeExcludeOperation = new SchemaCompareIncludeExcludeNodeOperation(schemaCompareExcludeNodeParams, schemaCompareOperation.ComparisonResult);
            nodeExcludeOperation.Execute(TaskExecutionMode.Execute);

            int afterExclude = schemaCompareOperation.ComparisonResult.Differences.Count();

            Assert.True(initial == afterExclude, $"Changes should be same again after excluding/including, before {initial}, now {afterExclude}");

            generateScriptOperation = new SchemaCompareGenerateScriptOperation(generateScriptParams, schemaCompareOperation.ComparisonResult);
            generateScriptOperation.Execute(TaskExecutionMode.Script);

            Assert.True(generateScriptOperation.ScriptGenerationResult.Success);
            string afterExcludeScript = generateScriptOperation.ScriptGenerationResult.Script;
            Assert.True(initialScript.Length > afterExcludeScript.Length, $"Script should be affected (less statements) exclude operation, before {initialScript}, now {afterExcludeScript}");

            SchemaCompareNodeParams schemaCompareincludeNodeParams = new SchemaCompareNodeParams()
            {
                OperationId = schemaCompareOperation.OperationId,
                DiffEntry = diff,
                IncludeRequest = true,
                TaskExecutionMode = TaskExecutionMode.Execute
            };

            SchemaCompareIncludeExcludeNodeOperation nodeIncludeOperation = new SchemaCompareIncludeExcludeNodeOperation(schemaCompareincludeNodeParams, schemaCompareOperation.ComparisonResult);
            nodeIncludeOperation.Execute(TaskExecutionMode.Execute);
            int afterInclude = schemaCompareOperation.ComparisonResult.Differences.Count();

            Assert.True(initial == afterInclude, $"Changes should be same again after excluding/including:{initial}, now {afterInclude}");

            generateScriptOperation = new SchemaCompareGenerateScriptOperation(generateScriptParams, schemaCompareOperation.ComparisonResult);
            generateScriptOperation.Execute(TaskExecutionMode.Script);

            Assert.True(generateScriptOperation.ScriptGenerationResult.Success);
            string afterIncludeScript = generateScriptOperation.ScriptGenerationResult.Script;
            Assert.True(initialScript.Length == afterIncludeScript.Length, $"Changes should be same as inital since we included what we excluded, before {initialScript}, now {afterIncludeScript}");
        }

        private async Task CreateAndOpenScmp(SchemaCompareEndpointType sourceEndpointType, SchemaCompareEndpointType targetEndpointType)
        {
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareOpenScmpSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareOpenScmpTarget");

            try
            {
                SchemaCompareEndpoint sourceEndpoint = CreateSchemaCompareEndpoint(sourceDb, sourceEndpointType);
                SchemaCompareEndpoint targetEndpoint = CreateSchemaCompareEndpoint(targetDb, targetEndpointType);

                // create a comparison and exclude the first difference
                SchemaComparison compare = new SchemaComparison(sourceEndpoint, targetEndpoint);
                SchemaComparisonResult result = compare.Compare();
                Assert.NotEmpty(result.Differences);
                SchemaDifference difference = result.Differences.First();
                if (difference.SourceObject != null)
                {
                    compare.ExcludedSourceObjects.Add(new SchemaComparisonExcludedObjectId(difference.SourceObject.ObjectType, difference.SourceObject.Name));
                }
                else
                {
                    compare.ExcludedSourceObjects.Add(new SchemaComparisonExcludedObjectId(difference.TargetObject.ObjectType, difference.TargetObject.Name));
                }

                // save to scmp
                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SchemaCompareTest");
                Directory.CreateDirectory(folderPath);
                string filePath = Path.Combine(folderPath, string.Format("SchemaCompareOpenScmpTest{0}.scmp", DateTime.Now.ToFileTime()));
                compare.SaveToFile(filePath);
                Assert.True(File.Exists(filePath));

                var schemaCompareOpenScmpParams = new SchemaCompareOpenScmpParams
                {
                    filePath = filePath
                };

                SchemaCompareOpenScmpOperation schemaCompareOpenScmpOperation = new SchemaCompareOpenScmpOperation(schemaCompareOpenScmpParams);
                schemaCompareOpenScmpOperation.Execute(TaskExecutionMode.Execute);

                Assert.NotNull(schemaCompareOpenScmpOperation.Result);
                Assert.True(schemaCompareOpenScmpOperation.Result.Success);
                Assert.NotEmpty(schemaCompareOpenScmpOperation.Result.ExcludedSourceElements);
                Assert.Equal(1, schemaCompareOpenScmpOperation.Result.ExcludedSourceElements.Count());
                Assert.Empty(schemaCompareOpenScmpOperation.Result.ExcludedTargetElements);
                Assert.Equal(targetDb.DatabaseName, schemaCompareOpenScmpOperation.Result.OriginalTargetName);
                ValidateResultEndpointInfo(sourceEndpoint, schemaCompareOpenScmpOperation.Result.SourceEndpointInfo, sourceDb.ConnectionString);
                ValidateResultEndpointInfo(targetEndpoint, schemaCompareOpenScmpOperation.Result.TargetEndpointInfo, targetDb.ConnectionString);

                SchemaCompareTestUtils.VerifyAndCleanup(filePath);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        private SchemaCompareEndpoint CreateSchemaCompareEndpoint(SqlTestDb db, SchemaCompareEndpointType endpointType)
        {
            if (endpointType == SchemaCompareEndpointType.Dacpac)
            {
                string dacpacFilePath = SchemaCompareTestUtils.CreateDacpac(db);
                return new SchemaCompareDacpacEndpoint(dacpacFilePath);
            }
            else
            {
                return new SchemaCompareDatabaseEndpoint(db.ConnectionString);
            }
        }

        private void ValidateResultEndpointInfo(SchemaCompareEndpoint originalEndpoint, SchemaCompareEndpointInfo resultEndpoint, string connectionString)
        {
            if (resultEndpoint.EndpointType == SchemaCompareEndpointType.Dacpac)
            {
                SchemaCompareDacpacEndpoint dacpacEndpoint = originalEndpoint as SchemaCompareDacpacEndpoint;
                Assert.Equal(dacpacEndpoint.FilePath, resultEndpoint.PackageFilePath);
            }
            else
            {
                SchemaCompareDatabaseEndpoint databaseEndpoint = originalEndpoint as SchemaCompareDatabaseEndpoint;
                Assert.Equal(databaseEndpoint.DatabaseName, resultEndpoint.DatabaseName);
                Assert.Contains(resultEndpoint.ConnectionDetails.ConnectionString, connectionString); // connectionString has password but resultEndpoint doesn't
            }
        }

        private void ValidateDiffEntryCreation(DiffEntry diff, SchemaDifference schemaDifference)
        {
            if (schemaDifference.SourceObject != null)
            {
                ValidateDiffEntryObjects(diff.SourceValue, diff.SourceObjectType, schemaDifference.SourceObject);
            }
            if (schemaDifference.TargetObject != null)
            {
                ValidateDiffEntryObjects(diff.TargetValue, diff.TargetObjectType, schemaDifference.TargetObject);
            }
        }

        private void ValidateDiffEntryObjects(string[] diffObjectName, string diffObjectTypeType, TSqlObject dacfxObject)
        {
            Assert.Equal(dacfxObject.Name.Parts.Count, diffObjectName.Length);
            for (int i = 0; i < diffObjectName.Length; i++)
            {
                Assert.Equal(dacfxObject.Name.Parts[i], diffObjectName[i]);
            }

            var dacFxExcludedObject = new SchemaComparisonExcludedObjectId(dacfxObject.ObjectType, dacfxObject.Name);
            var excludedObject = new SchemaComparisonExcludedObjectId(diffObjectTypeType, new ObjectIdentifier(diffObjectName));

            Assert.Equal(dacFxExcludedObject.Identifier.ToString(), excludedObject.Identifier.ToString());
            Assert.Equal(dacFxExcludedObject.TypeName, excludedObject.TypeName);

            string dacFxType = dacFxExcludedObject.TypeName;
            Assert.Equal(dacFxType, diffObjectTypeType);
        }

        private void CreateAndValidateScmpFile(SchemaCompareEndpointInfo sourceInfo, SchemaCompareEndpointInfo targetInfo, bool isSourceDb, bool isTargetDb)
        {
            string filePath = SchemaCompareTestUtils.CreateScmpPath();
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();

            SchemaCompareObjectId[] schemaCompareObjectIds = new SchemaCompareObjectId[]{
                new SchemaCompareObjectId()
                {
                    NameParts = new string[] {"dbo", "Table1" },
                    SqlObjectType = "Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlTable",
                }
            };

            var schemaCompareParams = new SchemaCompareSaveScmpParams
            {
                SourceEndpointInfo = sourceInfo,
                TargetEndpointInfo = targetInfo,
                DeploymentOptions = new DeploymentOptions()
                {
                    // change some random ones explicitly
                    AllowDropBlockingAssemblies = true,
                    DropConstraintsNotInSource = true,
                    IgnoreAnsiNulls = true,
                    NoAlterStatementsToChangeClrTypes = false,
                    PopulateFilesOnFileGroups = false,
                    VerifyDeployment = false,
                },
                ScmpFilePath = filePath,
                ExcludedSourceObjects = schemaCompareObjectIds,
                ExcludedTargetObjects = null,
            };

            SchemaCompareSaveScmpOperation schemaCompareOperation = new SchemaCompareSaveScmpOperation(schemaCompareParams, result.ConnectionInfo, result.ConnectionInfo);
            schemaCompareOperation.Execute(TaskExecutionMode.Execute);

            Assert.True(File.Exists(filePath), "SCMP file should be present");

            string text = File.ReadAllText(filePath);
            Assert.True(!string.IsNullOrEmpty(text), "SCMP File should not be empty");

            // Validate with DacFx SchemaComparison object
            SchemaComparison sc = new SchemaComparison(filePath);

            if (isSourceDb)
            {
                Assert.True(sc.Source is SchemaCompareDatabaseEndpoint, "Source should be SchemaCompareDatabaseEndpoint");
                Assert.True((sc.Source as SchemaCompareDatabaseEndpoint).DatabaseName == sourceInfo.DatabaseName, $"Source Database {(sc.Source as SchemaCompareDatabaseEndpoint).DatabaseName} name does not match the params passed {sourceInfo.DatabaseName}");
            }
            else
            {
                Assert.True(sc.Source is SchemaCompareDacpacEndpoint, "Source should be SchemaCompareDacpacEndpoint");
                Assert.True((sc.Source as SchemaCompareDacpacEndpoint).FilePath == sourceInfo.PackageFilePath, $"Source dacpac {(sc.Source as SchemaCompareDacpacEndpoint).FilePath} name does not match the params passed {sourceInfo.PackageFilePath}");
                SchemaCompareTestUtils.VerifyAndCleanup(sourceInfo.PackageFilePath);
            }

            if (isTargetDb)
            {
                Assert.True(sc.Target is SchemaCompareDatabaseEndpoint, "Source should be SchemaCompareDatabaseEndpoint");
                Assert.True((sc.Target as SchemaCompareDatabaseEndpoint).DatabaseName == targetInfo.DatabaseName, $"Source Database {(sc.Target as SchemaCompareDatabaseEndpoint).DatabaseName} name does not match the params passed {targetInfo.DatabaseName}");
            }
            else
            {
                Assert.True(sc.Target is SchemaCompareDacpacEndpoint, "Source should be SchemaCompareDacpacEndpoint");
                Assert.True((sc.Target as SchemaCompareDacpacEndpoint).FilePath == targetInfo.PackageFilePath, $"Source dacpac {(sc.Target as SchemaCompareDacpacEndpoint).FilePath} name does not match the params passed {targetInfo.PackageFilePath}");
                SchemaCompareTestUtils.VerifyAndCleanup(targetInfo.PackageFilePath);
            }

            Assert.True(!sc.ExcludedTargetObjects.Any(), "Target Excluded Objects are expected to be Empty");
            Assert.True(sc.ExcludedSourceObjects.Count == 1, $"Exactly {1} Source Excluded Object Should be present but {sc.ExcludedSourceObjects.Count} found");
            SchemaCompareTestUtils.CompareOptions(schemaCompareParams.DeploymentOptions, sc.Options);
            SchemaCompareTestUtils.VerifyAndCleanup(filePath);
        }

        private bool ValidateScResult(SchemaCompareResult diffResult, ref DiffEntry diffEntry, ref string operationId)
        {
            try
            {
                operationId = diffResult.OperationId;
                diffEntry = diffResult.Differences.ElementAt(0);
                return (diffResult.Success == true && diffResult.Differences != null && diffResult.Differences.Count > 0);
            }
            catch
            {
                return false;
            }
        }
    }
}
