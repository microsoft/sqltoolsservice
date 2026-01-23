//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlServer.Dac;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

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
            options.BooleanOptionsDictionary[nameof(DacDeployOptions.IgnoreColumnOrder)].Value = true;
            return options;
        }

        private DeploymentOptions GetExcludeTableValuedFunctionOptions()
        {
            var options = new DeploymentOptions();
            options.ExcludeObjectTypes = new DeploymentOptionProperty<string[]>
                (
                    new string[]{
                        Enum.GetName(ObjectType.ServerTriggers),
                        Enum.GetName(ObjectType.Routes),
                        Enum.GetName(ObjectType.LinkedServerLogins),
                        Enum.GetName(ObjectType.Endpoints),
                        Enum.GetName(ObjectType.ErrorMessages),
                        Enum.GetName(ObjectType.Filegroups),
                        Enum.GetName(ObjectType.Files),
                        Enum.GetName(ObjectType.Logins),
                        Enum.GetName(ObjectType.LinkedServers),
                        Enum.GetName(ObjectType.Credentials),
                        Enum.GetName(ObjectType.DatabaseScopedCredentials),
                        Enum.GetName(ObjectType.DatabaseEncryptionKeys),
                        Enum.GetName(ObjectType.MasterKeys),
                        Enum.GetName(ObjectType.DatabaseAuditSpecifications),
                        Enum.GetName(ObjectType.Audits),
                        Enum.GetName(ObjectType.ServerAuditSpecifications),
                        Enum.GetName(ObjectType.CryptographicProviders),
                        Enum.GetName(ObjectType.ServerRoles),
                        Enum.GetName(ObjectType.EventSessions),
                        Enum.GetName(ObjectType.DatabaseOptions),
                        Enum.GetName(ObjectType.EventNotifications),
                        Enum.GetName(ObjectType.ServerRoleMembership),
                        Enum.GetName(ObjectType.AssemblyFiles),
                        Enum.GetName(ObjectType.TableValuedFunctions), //added Functions to excluded types
                    }
                );
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
                Assert.IsNull(schemaCompareOperation1.ErrorMessage);

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
                Assert.IsNull(schemaCompareOperation2.ErrorMessage);

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
                Assert.IsNull(schemaCompareOperation1.ErrorMessage);

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
                Assert.IsNull(schemaCompareOperation2.ErrorMessage);
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
                Assert.IsNull(schemaCompareOperation1.ErrorMessage);

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
                    Assert.AreEqual("Performing script generation is not possible for this comparison result.", generateScriptOperation1.ScriptGenerationResult.Message);
                    Assert.AreEqual("Performing script generation is not possible for this comparison result.", ex.Message);
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
                Assert.IsNull(schemaCompareOperation2.ErrorMessage);

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
        [Test]
        public async Task SchemaCompareDacpacToDacpacOptions()
        {
            await SendAndValidateSchemaCompareRequestDacpacToDacpacWithOptions(Source1, Target1, GetIgnoreColumnOptions(), new DeploymentOptions());
        }

        /// <summary>
        /// Verify the schema compare request comparing two dacpacs with and excluding table valued functions
        /// </summary>
        [Test]
        public async Task SchemaCompareDacpacToDacpacObjectTypes()
        {
            await SendAndValidateSchemaCompareRequestDacpacToDacpacWithOptions(Source2, Target2, GetExcludeTableValuedFunctionOptions(), new DeploymentOptions());
        }

        /// <summary>
        /// Verify the schema compare request comparing two databases with and without ignore column option
        /// </summary>
        [Test]
        public async Task SchemaCompareDatabaseToDatabaseOptions()
        {
            await SendAndValidateSchemaCompareRequestDatabaseToDatabaseWithOptions(Source1, Target1, GetIgnoreColumnOptions(), new DeploymentOptions());
        }

        /// <summary>
        /// Verify the schema compare request comparing two databases with and excluding table valued functions
        /// </summary>
        [Test]
        public async Task SchemaCompareDatabaseToDatabaseObjectTypes()
        {
            await SendAndValidateSchemaCompareRequestDatabaseToDatabaseWithOptions(Source2, Target2, GetExcludeTableValuedFunctionOptions(), new DeploymentOptions());
        }

        /// <summary>
        /// Verify the schema compare script generation comparing dacpac and db with and without ignore column option
        /// </summary>
        [Test]
        public async Task SchemaCompareGenerateScriptDacpacToDatabaseOptions()
        {
            await SendAndValidateSchemaCompareGenerateScriptRequestDacpacToDatabaseWithOptions(Source1, Target1, GetIgnoreColumnOptions(), new DeploymentOptions());
        }

        /// <summary>
        /// Verify the schema compare script generation comparing dacpac and db with and excluding table valued function
        /// </summary>
        [Test]
        public async Task SchemaCompareGenerateScriptDacpacToDatabaseObjectTypes()
        {
            await SendAndValidateSchemaCompareGenerateScriptRequestDacpacToDatabaseWithOptions(Source2, Target2, GetExcludeTableValuedFunctionOptions(), new DeploymentOptions());
        }

        /// <summary>
        /// Verify the schema compare default creation test
        /// </summary>
        [Test]
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

            SchemaCompareTestUtils.CompareOptions(deployOptions, dacOptions);
        }

        /// <summary>
        /// Verify the schema compare default creation test
        /// </summary>
        [Test]
        public async Task ValidateSchemaCompareGetDefaultOptionsCallFromService()
        {
            DeploymentOptions deployOptions = new DeploymentOptions();
            var schemaCompareRequestContext = new Mock<RequestContext<GetDeploymentOptionsResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<GetDeploymentOptionsResult>())).Returns(Task.FromResult(new object()));
            schemaCompareRequestContext.Setup((RequestContext<GetDeploymentOptionsResult> x) => x.SendResult(It.Is<GetDeploymentOptionsResult>((options) => SchemaCompareTestUtils.ValidateOptionsEqualsDefault(options) == true))).Returns(Task.FromResult(new object()));

            GetDeploymentOptionsParams p = new GetDeploymentOptionsParams();
            DacFxService service = new DacFxService();
            await service.HandleGetDeploymentOptionsRequest(p, schemaCompareRequestContext.Object);
        }

        /// <summary>
        /// Verify that Scenario parameter controls whether Schema Compare or Deployment defaults are used
        /// </summary>
        [Test]
        public async Task ValidateSchemaCompareGetOptionsWithScenarioParameter()
        {
            var requestContext = new Mock<RequestContext<GetDeploymentOptionsResult>>();
            GetDeploymentOptionsResult schemaCompareResult = null;
            GetDeploymentOptionsResult publishResult = null;

            requestContext.Setup(x => x.SendResult(It.IsAny<GetDeploymentOptionsResult>()))
                .Returns(Task.FromResult(new object()));

            // Test Schema Compare scenario - should have SSMS-matching modified defaults
            GetDeploymentOptionsParams schemaCompareParams = new GetDeploymentOptionsParams { Scenario = DeploymentScenario.SchemaCompare };
            requestContext.Setup(x => x.SendResult(It.IsAny<GetDeploymentOptionsResult>()))
                .Callback<GetDeploymentOptionsResult>(r => schemaCompareResult = r)
                .Returns(Task.FromResult(new object()));
            
            DacFxService service = new DacFxService();
            await service.HandleGetDeploymentOptionsRequest(schemaCompareParams, requestContext.Object);
            
            Assert.That(schemaCompareResult, Is.Not.Null, "Schema Compare result should not be null");
            Assert.That(schemaCompareResult.Success, Is.True, "Schema Compare request should succeed");
            
            // Verify SSMS-matching modified defaults are applied
            Assert.That(schemaCompareResult.DefaultDeploymentOptions.BooleanOptionsDictionary[nameof(DacDeployOptions.AllowDropBlockingAssemblies)].Value, 
                Is.True, "AllowDropBlockingAssemblies should be true for Schema Compare (SSMS-matching default)");
            Assert.That(schemaCompareResult.DefaultDeploymentOptions.BooleanOptionsDictionary[nameof(DacDeployOptions.DropObjectsNotInSource)].Value, 
                Is.True, "DropObjectsNotInSource should be true for Schema Compare (SSMS-matching default)");
            Assert.That(schemaCompareResult.DefaultDeploymentOptions.BooleanOptionsDictionary[nameof(DacDeployOptions.IgnoreKeywordCasing)].Value, 
                Is.False, "IgnoreKeywordCasing should be false for Schema Compare (SSMS-matching default)");

            // Test Deployment/Publish scenario - should have DacFx native defaults
            GetDeploymentOptionsParams publishParams = new GetDeploymentOptionsParams { Scenario = DeploymentScenario.Deployment };
            requestContext.Setup(x => x.SendResult(It.IsAny<GetDeploymentOptionsResult>()))
                .Callback<GetDeploymentOptionsResult>(r => publishResult = r)
                .Returns(Task.FromResult(new object()));
            
            await service.HandleGetDeploymentOptionsRequest(publishParams, requestContext.Object);
            
            Assert.That(publishResult, Is.Not.Null, "Deployment result should not be null");
            Assert.That(publishResult.Success, Is.True, "Deployment request should succeed");
            
            // Verify DacFx native defaults are used
            Assert.That(publishResult.DefaultDeploymentOptions.BooleanOptionsDictionary[nameof(DacDeployOptions.AllowDropBlockingAssemblies)].Value, 
                Is.False, "AllowDropBlockingAssemblies should be false for Deployment (DacFx native default)");
            Assert.That(publishResult.DefaultDeploymentOptions.BooleanOptionsDictionary[nameof(DacDeployOptions.DropObjectsNotInSource)].Value, 
                Is.False, "DropObjectsNotInSource should be false for Deployment (DacFx native default)");
            Assert.That(publishResult.DefaultDeploymentOptions.BooleanOptionsDictionary[nameof(DacDeployOptions.IgnoreKeywordCasing)].Value, 
                Is.True, "IgnoreKeywordCasing should be true for Deployment (DacFx native default)");
        }
    }
}
