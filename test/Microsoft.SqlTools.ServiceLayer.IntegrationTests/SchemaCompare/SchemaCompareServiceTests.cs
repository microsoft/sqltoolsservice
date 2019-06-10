//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.SchemaCopmare;
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

        private async Task SendAndValidateSchemaCompareRequestDacpacToDacpac()
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

        private async Task SendAndValidateSchemaCompareRequestDatabaseToDatabase()
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

        private async Task SendAndValidateSchemaCompareRequestDatabaseToDacpac()
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

        private async Task SendAndValidateSchemaCompareGenerateScriptRequestDatabaseToDatabase()
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

        private async Task SendAndValidateSchemaCompareGenerateScriptRequestDacpacToDatabase()
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

        private async Task SendAndValidateSchemaComparePublishChangesRequestDacpacToDatabase()
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

        private async Task SendAndValidateSchemaComparePublishChangesRequestDatabaseToDatabase()
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
            generateScriptOperation.Execute();

            Assert.True(generateScriptOperation.ScriptGenerationResult.Success);
            string initialScript = generateScriptOperation.ScriptGenerationResult.Script;

            // create Diff Entry from on Difference
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

            generateScriptOperation = new SchemaCompareGenerateScriptOperation(generateScriptParams, schemaCompareOperation.ComparisonResult);
            generateScriptOperation.Execute();

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
            generateScriptOperation.Execute();

            Assert.True(generateScriptOperation.ScriptGenerationResult.Success);
            string afterIncludeScript = generateScriptOperation.ScriptGenerationResult.Script;
            Assert.True(initialScript.Length == afterIncludeScript.Length, $"Changes should be same as inital since we included what we excluded, before {initialScript}, now {afterIncludeScript}");
        }


        private async Task SaveAndValidateSchemaCompareScmpFileForDatabases()
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

                CreateAndValidateScmpFile(sourceInfo, targetInfo, true);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        private async Task SaveAndValidateSchemaCompareScmpFileForDacpacs()
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

                CreateAndValidateScmpFile(sourceInfo, targetInfo, false);

            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        private void CreateAndValidateScmpFile(SchemaCompareEndpointInfo sourceInfo, SchemaCompareEndpointInfo targetInfo, bool isEnpointTypeDatabase)
        {
            string filePath = SchemaCompareTestUtils.CreateScmpPath();
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();

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
                scmpFilePath = filePath
            };

            SchemaCompareSaveScmpOperation schemaCompareOperation = new SchemaCompareSaveScmpOperation(schemaCompareParams, result.ConnectionInfo, result.ConnectionInfo);
            schemaCompareOperation.Execute(TaskExecutionMode.Execute);

            Assert.True(File.Exists(filePath), "SCMP file should be present");

            string text = File.ReadAllText(filePath);
            Assert.True(!string.IsNullOrEmpty(text), "SCMP File should not be empty");

            // Validate with DacFx SchemaComparison object
            SchemaComparison sc = new SchemaComparison(filePath);

            if (isEnpointTypeDatabase)
            {
                Assert.True(sc.Source is SchemaCompareDatabaseEndpoint, "Source should be SchemaCompareDatabaseEndpoint");
                Assert.True((sc.Source as SchemaCompareDatabaseEndpoint).DatabaseName == sourceInfo.DatabaseName, $"Source Database {(sc.Source as SchemaCompareDatabaseEndpoint).DatabaseName} name does not match the params passed {sourceInfo.DatabaseName}");
                Assert.True(sc.Target is SchemaCompareDatabaseEndpoint, "Source should be SchemaCompareDatabaseEndpoint");
                Assert.True((sc.Target as SchemaCompareDatabaseEndpoint).DatabaseName == targetInfo.DatabaseName, $"Source Database {(sc.Target as SchemaCompareDatabaseEndpoint).DatabaseName} name does not match the params passed {targetInfo.DatabaseName}");
            }
            else
            {
                Assert.True(sc.Source is SchemaCompareDacpacEndpoint, "Source should be SchemaCompareDacpacEndpoint");
                Assert.True((sc.Source as SchemaCompareDacpacEndpoint).FilePath == sourceInfo.PackageFilePath, $"Source dacpac {(sc.Source as SchemaCompareDacpacEndpoint).FilePath} name does not match the params passed {sourceInfo.PackageFilePath}");
                Assert.True(sc.Target is SchemaCompareDacpacEndpoint, "Source should be SchemaCompareDacpacEndpoint");
                Assert.True((sc.Target as SchemaCompareDacpacEndpoint).FilePath == targetInfo.PackageFilePath, $"Source dacpac {(sc.Target as SchemaCompareDacpacEndpoint).FilePath} name does not match the params passed {targetInfo.PackageFilePath}");

                SchemaCompareTestUtils.VerifyAndCleanup(sourceInfo.PackageFilePath);
                SchemaCompareTestUtils.VerifyAndCleanup(targetInfo.PackageFilePath);
            }

            SchemaCompareTestUtils.CompareOptions(schemaCompareParams.DeploymentOptions, sc.Options);
            SchemaCompareTestUtils.VerifyAndCleanup(filePath);
        }

        private async Task VerifySchemaCompareServiceCalls()
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
                    scmpFilePath = scmpFilePath
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

        /// <summary>
        /// Verify the schema compare request comparing two dacpacs
        /// </summary>
        [Fact]
        public async void SchemaCompareDacpacToDacpac()
        {
            await SendAndValidateSchemaCompareRequestDacpacToDacpac();
        }

        /// <summary>
        /// Verify the schema compare request comparing a two databases
        /// </summary>
        [Fact]
        public async void SchemaCompareDatabaseToDatabase()
        {
            await SendAndValidateSchemaCompareRequestDatabaseToDatabase();
        }

        /// <summary>
        /// Verify the schema compare request comparing a database to a dacpac
        /// </summary>
        [Fact]
        public async void SchemaCompareDatabaseToDacpac()
        {
            await SendAndValidateSchemaCompareRequestDatabaseToDacpac();
        }

        /// <summary>
        /// Verify the schema compare generate script request comparing a database to a database
        /// </summary>
        [Fact]
        public async void SchemaCompareGenerateScriptDatabaseToDatabase()
        {
            await SendAndValidateSchemaCompareGenerateScriptRequestDatabaseToDatabase();
        }

        /// <summary>
        /// Verify the schema compare generate script request comparing a dacpac to a database
        /// </summary>
        [Fact]
        public async void SchemaCompareGenerateScriptDacpacToDatabase()
        {
            await SendAndValidateSchemaCompareGenerateScriptRequestDacpacToDatabase();
        }

        /// <summary>
        /// Verify the schema compare publish changes request comparing a dacpac to a database
        /// </summary>
        [Fact]
        public async void SchemaComparePublishChangesDacpacToDatabase()
        {
            await SendAndValidateSchemaComparePublishChangesRequestDacpacToDatabase();
        }

        /// <summary>
        /// Verify the schema compare publish changes request comparing a database to a database
        /// </summary>
        [Fact]
        public async void SchemaComparePublishChangesDatabaseToDatabase()
        {
            await SendAndValidateSchemaComparePublishChangesRequestDatabaseToDatabase();
        }

        /// <summary>
        /// Verify saving of scmp files for database enpoints
        /// </summary>
        [Fact]
        public async void SchemaCompareSaveScmpFilesForDatabaseEndpoints()
        {
            await SaveAndValidateSchemaCompareScmpFileForDatabases();
        }

        /// <summary>
        /// Verify saving of scmp files for dacpac enpoints
        /// </summary>
        [Fact]
        public async void SchemaCompareSaveScmpFilesForDacPacEndpoints()
        {
            await SaveAndValidateSchemaCompareScmpFileForDacpacs();
        }

        /// <summary>
        /// Verify E2E schema compare service calls
        /// </summary>
        [Fact]
        public async void ValidateE2ESchemaCompareServiceCalls()
        {
            await VerifySchemaCompareServiceCalls();
        }
    }
}
