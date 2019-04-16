//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
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
    /// Group of tests to test non-default options and included items for schema comapre
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
        private async Task<Mock<RequestContext<SchemaCompareResult>>> SendAndValidateSchemaCompareRequestDacpacToDacpacWithOptions(string sourceScript, string targetScript, SchemaCompareOptions nodiffOption, SchemaCompareOptions shouldDiffOption)
        {

            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareResult>())).Returns(Task.FromResult(new object()));

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
                    SchemaCompareOptions = nodiffOption
                };

                SchemaCompareOperation schemaCompareOperation1 = new SchemaCompareOperation(schemaCompareParams1, null, null);
                schemaCompareOperation1.Execute(TaskExecutionMode.Execute);
                Assert.True(schemaCompareOperation1.ComparisonResult.IsEqual);
                
                var schemaCompareParams2 = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo,
                    SchemaCompareOptions = shouldDiffOption,
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

            return schemaCompareRequestContext;
        }

        private async Task<Mock<RequestContext<SchemaCompareResult>>> SendAndValidateSchemaCompareRequestDatabaseToDatabaseWithOptions(string sourceScript, string targetScript, SchemaCompareOptions nodiffOption, SchemaCompareOptions shouldDiffOption)
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareResult>())).Returns(Task.FromResult(new object()));

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
                    SchemaCompareOptions = nodiffOption
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
                    SchemaCompareOptions = shouldDiffOption,
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
            return schemaCompareRequestContext;
        }

        private async Task<Mock<RequestContext<SchemaCompareResult>>> SendAndValidateSchemaCompareGenerateScriptRequestDacpacToDatabaseWithOptions(string sourceScript, string targetScript, SchemaCompareOptions nodiffOption, SchemaCompareOptions shouldDiffOption)
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareResult>())).Returns(Task.FromResult(new object()));

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
                    SchemaCompareOptions = nodiffOption,
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
                    ScriptFilePath = Path.Combine(folderPath, string.Concat(sourceDb.DatabaseName, "_", "Update.publish1.sql"))
                };

                SchemaCompareGenerateScriptOperation generateScriptOperation1 = new SchemaCompareGenerateScriptOperation(generateScriptParams1, schemaCompareOperation1.ComparisonResult);
                generateScriptOperation1.Execute(TaskExecutionMode.Execute);

                // validate script
                var filePath1 = Path.Combine(folderPath, string.Concat(sourceDb.DatabaseName, "_", "Update.publish1.sql"));
                Assert.True(File.Exists(filePath1) && string.IsNullOrEmpty(File.ReadAllText(filePath1)), "Should not be any differences");

                var schemaCompareParams2 = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo,
                    SchemaCompareOptions = shouldDiffOption,
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
                    ScriptFilePath = Path.Combine(folderPath, string.Concat(sourceDb.DatabaseName, "_", "Update.publish2.sql"))
                };

                SchemaCompareGenerateScriptOperation generateScriptOperation2 = new SchemaCompareGenerateScriptOperation(generateScriptParams2, schemaCompareOperation2.ComparisonResult);
                generateScriptOperation2.Execute(TaskExecutionMode.Execute);

                // validate script
                var filePath2 = Path.Combine(folderPath, string.Concat(sourceDb.DatabaseName, "_", "Update.publish2.sql"));
                Assert.True(File.Exists(filePath2) && !string.IsNullOrEmpty(File.ReadAllText(filePath2)), "Should have differences differences");

                // cleanup
                SchemaCompareTestUtils.VerifyAndCleanup(generateScriptParams1.ScriptFilePath);
                SchemaCompareTestUtils.VerifyAndCleanup(generateScriptParams2.ScriptFilePath);
                SchemaCompareTestUtils.VerifyAndCleanup(sourceDacpacFilePath);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
            return schemaCompareRequestContext;
        }

        [Fact]
        public async void SchemaCompareDacpacToDacpacOptions()
        {
            var options = new SchemaCompareOptions();
            options.IgnoreColumnOrder = true;
            Assert.NotNull(await SendAndValidateSchemaCompareRequestDacpacToDacpacWithOptions(Source1, Target1, options, new SchemaCompareOptions()));
        }

        [Fact]
        public async void SchemaCompareDacpacToDacpacObjectTypes()
        {
            var options = new SchemaCompareOptions();
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
            Assert.NotNull(await SendAndValidateSchemaCompareRequestDacpacToDacpacWithOptions(Source2, Target2, options, new SchemaCompareOptions()));
        }

        [Fact]
        public async void SchemaCompareDatabaseToDatabaseOptions()
        {
            var options = new SchemaCompareOptions();
            options.IgnoreColumnOrder = true;
            Assert.NotNull(await SendAndValidateSchemaCompareRequestDatabaseToDatabaseWithOptions(Source1, Target1, options, new SchemaCompareOptions()));
        }

        [Fact]
        public async void SchemaCompareDatabaseToDatabaseObjectTypes()
        {
            var options = new SchemaCompareOptions();
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
            Assert.NotNull(await SendAndValidateSchemaCompareRequestDatabaseToDatabaseWithOptions(Source2, Target2, options, new SchemaCompareOptions()));
        }

        [Fact]
        public async void SchemaCompareGenerateScriptDacpacToDatabaseOptions()
        {
            var options = new SchemaCompareOptions();
            options.IgnoreColumnOrder = true;
            Assert.NotNull(await SendAndValidateSchemaCompareGenerateScriptRequestDacpacToDatabaseWithOptions(Source1, Target1, options, new SchemaCompareOptions()));
        }

        [Fact]
        public async void SchemaCompareGenerateScriptDacpacToDatabaseObjectTypes()
        {
            var options = new SchemaCompareOptions();
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
            Assert.NotNull(await SendAndValidateSchemaCompareGenerateScriptRequestDacpacToDatabaseWithOptions(Source2, Target2, options, new SchemaCompareOptions()));
        }

    }
}
