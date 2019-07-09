//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlServer.Dac;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SchemaCompare
{
    /// <summary>
    /// Group of tests to test non-default options and included items for schema compare
    /// Note - adding it to new class for easy findability
    /// </summary>
    public class SchemaCompareServiceOptionsTests
    {
        private const string Source1 = @"CREATE TABLE [dbo].[table1]
(
    [ID] INT NOT NULL PRIMARY KEY,
    [Date] DATE NOT NULL,
)";
        private const string Target1 = @"CREATE TABLE [dbo].[table1]
(
    [Date] DATE NOT NULL,
    [ID] INT NOT NULL PRIMARY KEY,
)";

        private const string Source2 = @"
CREATE FUNCTION [dbo].[Function1]
(
	@param1 int,
	@param2 char(5)
)
RETURNS @returntable TABLE
(
	c1 int,
	c2 char(5)
)
AS
BEGIN
	INSERT @returntable
	SELECT @param1, @param2
	RETURN
END"
;
        private const string Target2 = @"CREATE FUNCTION [dbo].[Function1]
(
	@param1 int,
	@param2 char(5)
)
RETURNS @returntable TABLE
(
	x1 int,
	x2 char(5)
)
AS
BEGIN
	INSERT @returntable
	SELECT @param1, @param2
	RETURN
END
";
        private DeploymentOptions GetIgnoreColumnOptions()
        {
            var options = new DeploymentOptions();
            options.IgnoreColumnOrder = true;
            return options;
        }

        private DeploymentOptions GetExcludeTableValuedFunctionOptions()
        {
            var options = new DeploymentOptions();
            options.ExcludeObjectTypes = new ObjectType[]{
                ObjectType.ServerTriggers,
                ObjectType.Routes,
                ObjectType.LinkedServerLogins,
                ObjectType.Endpoints,
                ObjectType.ErrorMessages,
                ObjectType.Filegroups,
                ObjectType.Logins,
                ObjectType.LinkedServers,
                ObjectType.Credentials,
                ObjectType.DatabaseScopedCredentials,
                ObjectType.DatabaseEncryptionKeys,
                ObjectType.MasterKeys,
                ObjectType.DatabaseAuditSpecifications,
                ObjectType.Audits,
                ObjectType.ServerAuditSpecifications,
                ObjectType.CryptographicProviders,
                ObjectType.ServerRoles,
                ObjectType.EventSessions,
                ObjectType.DatabaseOptions,
                ObjectType.EventNotifications,
                ObjectType.ServerRoleMembership,
                ObjectType.AssemblyFiles,
                ObjectType.TableValuedFunctions, //added Functions to excluded types
            };
            return options;
        }

        private async Task SendAndValidateSchemaCompareRequestDacpacToDacpacWithOptions(string sourceScript, string targetScript, DeploymentOptions nodiffOption, DeploymentOptions shouldDiffOption)
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();

            // create dacpacs from databases
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, sourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, targetScript, "SchemaCompareTarget");
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

                var schemaCompareParams1 = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo,
                    DeploymentOptions = nodiffOption
                };

                SchemaCompareOperation schemaCompareOperation1 = new SchemaCompareOperation(schemaCompareParams1, null, null);
                schemaCompareOperation1.Execute(TaskExecutionMode.Execute);
                Assert.True(schemaCompareOperation1.ComparisonResult.IsEqual);

                var schemaCompareParams2 = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo,
                    DeploymentOptions = shouldDiffOption,
                };

                SchemaCompareOperation schemaCompareOperation2 = new SchemaCompareOperation(schemaCompareParams2, null, null);
                schemaCompareOperation2.Execute(TaskExecutionMode.Execute);
                Assert.False(schemaCompareOperation2.ComparisonResult.IsEqual);
                Assert.NotNull(schemaCompareOperation2.ComparisonResult.Differences);

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

        private async Task SendAndValidateSchemaCompareRequestDatabaseToDatabaseWithOptions(string sourceScript, string targetScript, DeploymentOptions nodiffOption, DeploymentOptions shouldDiffOption)
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, sourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, targetScript, "SchemaCompareTarget");
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

                var schemaCompareParams1 = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo,
                    DeploymentOptions = nodiffOption
                };

                SchemaCompareOperation schemaCompareOperation1 = new SchemaCompareOperation(schemaCompareParams1, result.ConnectionInfo, result.ConnectionInfo);
                schemaCompareOperation1.Execute(TaskExecutionMode.Execute);

                Assert.True(schemaCompareOperation1.ComparisonResult.IsValid);
                Assert.True(schemaCompareOperation1.ComparisonResult.IsEqual);
                Assert.NotNull(schemaCompareOperation1.ComparisonResult.Differences);

                var schemaCompareParams2 = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo,
                    DeploymentOptions = shouldDiffOption,
                };

                SchemaCompareOperation schemaCompareOperation2 = new SchemaCompareOperation(schemaCompareParams2, result.ConnectionInfo, result.ConnectionInfo);
                schemaCompareOperation2.Execute(TaskExecutionMode.Execute);
                Assert.False(schemaCompareOperation2.ComparisonResult.IsEqual);
                Assert.NotNull(schemaCompareOperation2.ComparisonResult.Differences);
            }
            finally
            {
                // cleanup
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        private async Task SendAndValidateSchemaCompareGenerateScriptRequestDacpacToDatabaseWithOptions(string sourceScript, string targetScript, DeploymentOptions nodiffOption, DeploymentOptions shouldDiffOption)
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, sourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, targetScript, "SchemaCompareTarget");
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

                var schemaCompareParams1 = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo,
                    DeploymentOptions = nodiffOption,
                };

                SchemaCompareOperation schemaCompareOperation1 = new SchemaCompareOperation(schemaCompareParams1, result.ConnectionInfo, result.ConnectionInfo);
                schemaCompareOperation1.Execute(TaskExecutionMode.Execute);

                Assert.True(schemaCompareOperation1.ComparisonResult.IsValid);
                Assert.True(schemaCompareOperation1.ComparisonResult.IsEqual);
                Assert.NotNull(schemaCompareOperation1.ComparisonResult.Differences);

                // generate script
                var generateScriptParams1 = new SchemaCompareGenerateScriptParams
                {
                    TargetDatabaseName = targetDb.DatabaseName,
                    OperationId = schemaCompareOperation1.OperationId,
                };

                SchemaCompareGenerateScriptOperation generateScriptOperation1 = new SchemaCompareGenerateScriptOperation(generateScriptParams1, schemaCompareOperation1.ComparisonResult);

                try
                {
                    generateScriptOperation1.Execute(TaskExecutionMode.Script);
                    Assert.True(false); //fail if it reaches here
                }
                catch (Exception ex)
                {                    
                    // validate script generation failed because there were no differences
                    Assert.False(generateScriptOperation1.ScriptGenerationResult.Success);
                    Assert.Equal("Performing script generation is not possible for this comparison result.", generateScriptOperation1.ScriptGenerationResult.Message);
                    Assert.Equal("Performing script generation is not possible for this comparison result.", ex.Message);
                }

                var schemaCompareParams2 = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo,
                    DeploymentOptions = shouldDiffOption,
                };

                SchemaCompareOperation schemaCompareOperation2 = new SchemaCompareOperation(schemaCompareParams2, result.ConnectionInfo, result.ConnectionInfo);
                schemaCompareOperation2.Execute(TaskExecutionMode.Execute);

                Assert.True(schemaCompareOperation2.ComparisonResult.IsValid);
                Assert.False(schemaCompareOperation2.ComparisonResult.IsEqual);
                Assert.NotNull(schemaCompareOperation2.ComparisonResult.Differences);

                // generate script
                var generateScriptParams2 = new SchemaCompareGenerateScriptParams
                {
                    TargetDatabaseName = targetDb.DatabaseName,
                    OperationId = schemaCompareOperation1.OperationId,
                };

                SchemaCompareGenerateScriptOperation generateScriptOperation2 = new SchemaCompareGenerateScriptOperation(generateScriptParams2, schemaCompareOperation2.ComparisonResult);
                generateScriptOperation2.Execute(TaskExecutionMode.Script);

                // validate script generation succeeded
                Assert.True(generateScriptOperation2.ScriptGenerationResult.Success);
                Assert.True(!string.IsNullOrEmpty(generateScriptOperation2.ScriptGenerationResult.Script), "Should have differences");
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
        /// Verify the schema compare request comparing two dacpacs with and without ignore column option
        /// </summary>
        [Fact]
        public async void SchemaCompareDacpacToDacpacOptions()
        {
            await SendAndValidateSchemaCompareRequestDacpacToDacpacWithOptions(Source1, Target1, GetIgnoreColumnOptions(), new DeploymentOptions());
        }

        /// <summary>
        /// Verify the schema compare request comparing two dacpacs with and excluding table valued functions
        /// </summary>
        [Fact]
        public async void SchemaCompareDacpacToDacpacObjectTypes()
        {
            await SendAndValidateSchemaCompareRequestDacpacToDacpacWithOptions(Source2, Target2, GetExcludeTableValuedFunctionOptions(), new DeploymentOptions());
        }

        /// <summary>
        /// Verify the schema compare request comparing two databases with and without ignore column option
        /// </summary>
        [Fact]
        public async void SchemaCompareDatabaseToDatabaseOptions()
        {
            await SendAndValidateSchemaCompareRequestDatabaseToDatabaseWithOptions(Source1, Target1, GetIgnoreColumnOptions(), new DeploymentOptions());
        }

        /// <summary>
        /// Verify the schema compare request comparing two databases with and excluding table valued functions
        /// </summary>
        [Fact]
        public async void SchemaCompareDatabaseToDatabaseObjectTypes()
        {
            await SendAndValidateSchemaCompareRequestDatabaseToDatabaseWithOptions(Source2, Target2, GetExcludeTableValuedFunctionOptions(), new DeploymentOptions());
        }

        /// <summary>
        /// Verify the schema compare script generation comparing dacpac and db with and without ignore column option
        /// </summary>
        [Fact]
        public async void SchemaCompareGenerateScriptDacpacToDatabaseOptions()
        {
            await SendAndValidateSchemaCompareGenerateScriptRequestDacpacToDatabaseWithOptions(Source1, Target1, GetIgnoreColumnOptions(), new DeploymentOptions());
        }

        /// <summary>
        /// Verify the schema compare script generation comparing dacpac and db with and excluding table valued function
        /// </summary>
        [Fact]
        public async void SchemaCompareGenerateScriptDacpacToDatabaseObjectTypes()
        {
            await SendAndValidateSchemaCompareGenerateScriptRequestDacpacToDatabaseWithOptions(Source2, Target2, GetExcludeTableValuedFunctionOptions(), new DeploymentOptions());
        }

        /// <summary>
        /// Verify the schema compare default creation test
        /// </summary>
        [Fact]
        public void ValidateSchemaCompareOptionsDefaultAgainstDacFx()
        {
            DeploymentOptions deployOptions = new DeploymentOptions();
            DacDeployOptions dacOptions = new DacDeployOptions();

            // Changes to match new defaults
            dacOptions.AllowDropBlockingAssemblies = true;
            dacOptions.AllowIncompatiblePlatform = true;
            dacOptions.DropObjectsNotInSource = true;
            dacOptions.DropPermissionsNotInSource = true;
            dacOptions.DropRoleMembersNotInSource = true;
            dacOptions.IgnoreKeywordCasing = false;
            dacOptions.IgnoreSemicolonBetweenStatements = false;
            dacOptions.IgnoreWhitespace = false;

            SchemaCompareTestUtils.CompareOptions(deployOptions, dacOptions);
        }

        /// <summary>
        /// Verify the schema compare default creation test
        /// </summary>
        [Fact]
        public async void ValidateSchemaCompareGetDefaultOptionsCallFromService()
        {
            DeploymentOptions deployOptions = new DeploymentOptions();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareOptionsResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareOptionsResult>())).Returns(Task.FromResult(new object()));
            schemaCompareRequestContext.Setup((RequestContext<SchemaCompareOptionsResult> x) => x.SendResult(It.Is<SchemaCompareOptionsResult>((options) => SchemaCompareTestUtils.ValidateOptionsEqualsDefault(options) == true))).Returns(Task.FromResult(new object()));

            SchemaCompareGetOptionsParams p = new SchemaCompareGetOptionsParams();
            await SchemaCompareService.Instance.HandleSchemaCompareGetDefaultOptionsRequest(p, schemaCompareRequestContext.Object);
        }
    }
}
