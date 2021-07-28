﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;
using Moq;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.DacFx
{
    public class DacFxServiceTests
    {
        private string publishProfileFolder = Path.Combine("..", "..", "..", "DacFx", "PublishProfiles");
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

        private const string SourceViewScript = @"CREATE VIEW [dbo].[view1] AS SELECT dbo.table1.* FROM dbo.table1";

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

        private const string databaseRefVarName = "DatabaseRef";
        private const string filterValueVarName = "FilterValue";

        private string storedProcScript = $@"
CREATE PROCEDURE [dbo].[Procedure1]
	@param1 int = 0,
	@param2 int
AS
	SELECT * FROM [$({databaseRefVarName})].[dbo].[Table1] WHERE Type = '$({filterValueVarName})'
RETURN 0
";

        private string dacpacsFolder = Path.Combine("..", "..", "..", "DacFx", "Dacpacs");

        private string goodCreateStreamingJob = @"EXEC sys.sp_create_streaming_job @NAME = 'myJob', @STATEMENT = 'INSERT INTO SqlOutputStream SELECT
    timeCreated, 
    machine.temperature as machine_temperature, 
    machine.pressure as machine_pressure, 
    ambient.temperature as ambient_temperature, 
    ambient.humidity as ambient_humidity
FROM EdgeHubInputStream'";

        private string missingCreateBothStreamingJob = @$"EXEC sys.sp_create_streaming_job @NAME = 'myJob', @STATEMENT = 'INSERT INTO MissingSqlOutputStream SELECT
    timeCreated, 
    machine.temperature as machine_temperature, 
    machine.pressure as machine_pressure, 
    ambient.temperature as ambient_temperature, 
    ambient.humidity as ambient_humidity
FROM MissingEdgeHubInputStream'";

        private LiveConnectionHelper.TestConnectionResult GetLiveAutoCompleteTestObjects()
        {
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            return result;
        }

        /// <summary>
        /// Verify the export bacpac request
        /// </summary>
        [Test]
        public async Task ExportBacpac()
        {
            var result = GetLiveAutoCompleteTestObjects();
            SqlTestDb testdb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxExportTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            try
            {
                var exportParams = new ExportParams
                {
                    DatabaseName = testdb.DatabaseName,
                    PackageFilePath = Path.Combine(folderPath, string.Format("{0}.bacpac", testdb.DatabaseName))
                };

                DacFxService service = new DacFxService();
                ExportOperation operation = new ExportOperation(exportParams, result.ConnectionInfo);
                service.PerformOperation(operation, TaskExecutionMode.Execute);

                VerifyAndCleanup(exportParams.PackageFilePath);
            }
            finally
            {
                testdb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the import bacpac request
        /// </summary>
        [Test]
        public async Task ImportBacpac()
        {
            // first export a bacpac
            var result = GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxImportTest");
            SqlTestDb targetDb = null;
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            try
            {
                var exportParams = new ExportParams
                {
                    DatabaseName = sourceDb.DatabaseName,
                    PackageFilePath = Path.Combine(folderPath, string.Format("{0}.bacpac", sourceDb.DatabaseName))
                };

                DacFxService service = new DacFxService();
                ExportOperation exportOperation = new ExportOperation(exportParams, result.ConnectionInfo);
                service.PerformOperation(exportOperation, TaskExecutionMode.Execute);

                // import the created bacpac
                var importParams = new ImportParams
                {
                    PackageFilePath = exportParams.PackageFilePath,
                    DatabaseName = string.Concat(sourceDb.DatabaseName, "-imported")
                };

                ImportOperation importOperation = new ImportOperation(importParams, result.ConnectionInfo);
                service.PerformOperation(importOperation, TaskExecutionMode.Execute);
                targetDb = SqlTestDb.CreateFromExisting(importParams.DatabaseName);

                VerifyAndCleanup(exportParams.PackageFilePath);
            }
            finally
            {
                sourceDb.Cleanup();
                if (targetDb != null)
                {
                    targetDb.Cleanup();
                }
            }
        }

        /// <summary>
        /// Verify the extract dacpac request
        /// </summary>
        [Test]
        public async Task ExtractDacpac()
        {
            var result = GetLiveAutoCompleteTestObjects();
            SqlTestDb testdb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxExtractTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            try
            {
                var extractParams = new ExtractParams
                {
                    DatabaseName = testdb.DatabaseName,
                    PackageFilePath = Path.Combine(folderPath, string.Format("{0}.dacpac", testdb.DatabaseName)),
                    ApplicationName = "test",
                    ApplicationVersion = "1.0.0.0"
                };

                DacFxService service = new DacFxService();
                ExtractOperation operation = new ExtractOperation(extractParams, result.ConnectionInfo);
                service.PerformOperation(operation, TaskExecutionMode.Execute);

                VerifyAndCleanup(extractParams.PackageFilePath);
            }
            finally
            {
                testdb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the extract request to create Sql file
        /// </summary>
        [Test]
        public async Task ExtractDBToFileTarget()
        {
            var result = GetLiveAutoCompleteTestObjects();
            SqlTestDb testdb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, doNotCleanupDb: false, databaseName: null, query: SourceScript, dbNamePrefix: "DacFxExtractDBToFileTarget");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            try
            {
                var extractParams = new ExtractParams
                {
                    DatabaseName = testdb.DatabaseName,
                    PackageFilePath = Path.Combine(folderPath, string.Format("{0}.sql", testdb.DatabaseName)),
                    ApplicationName = "test",
                    ApplicationVersion = "1.0.0.0",
                    ExtractTarget = DacExtractTarget.File
                };

                DacFxService service = new DacFxService();
                ExtractOperation operation = new ExtractOperation(extractParams, result.ConnectionInfo);
                service.PerformOperation(operation, TaskExecutionMode.Execute);

                VerifyAndCleanup(extractParams.PackageFilePath);
            }
            finally
            {
                testdb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the extract request to create a Flat file structure
        /// </summary>
        [Test]
        public async Task ExtractDBToFlatTarget()
        {
            var result = GetLiveAutoCompleteTestObjects();
            SqlTestDb testdb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, doNotCleanupDb: false, databaseName: null, query: SourceScript, dbNamePrefix: "DacFxExtractDBToFlatTarget");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest", "FlatExtract");
            Directory.CreateDirectory(folderPath);

            try
            {
                var extractParams = new ExtractParams
                {
                    DatabaseName = testdb.DatabaseName,
                    PackageFilePath = folderPath,
                    ApplicationName = "test",
                    ApplicationVersion = "1.0.0.0",
                    ExtractTarget = DacExtractTarget.Flat
                };

                DacFxService service = new DacFxService();
                ExtractOperation operation = new ExtractOperation(extractParams, result.ConnectionInfo);
                service.PerformOperation(operation, TaskExecutionMode.Execute);

                // Verify two sql files are generated in the target folder path 
                // for dev-servers where there are more users/permissions present on server - the extract might have more files than just 2 expected tables, so check only for tables
                int actualCnt = Directory.GetFiles(folderPath, "table*.sql", SearchOption.AllDirectories).Length;
                Assert.AreEqual(2, actualCnt);
            }
            finally
            {
                // Remove the directory
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }
                testdb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the deploy dacpac request
        /// </summary>
        [Test]
        public async Task DeployDacpac()
        {
            // first extract a db to have a dacpac to import later
            var result = GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxDeployTest");
            SqlTestDb targetDb = null;
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            try
            {
                var extractParams = new ExtractParams
                {
                    DatabaseName = sourceDb.DatabaseName,
                    PackageFilePath = Path.Combine(folderPath, string.Format("{0}.dacpac", sourceDb.DatabaseName)),
                    ApplicationName = "test",
                    ApplicationVersion = "1.0.0.0"
                };

                DacFxService service = new DacFxService();
                ExtractOperation extractOperation = new ExtractOperation(extractParams, result.ConnectionInfo);
                service.PerformOperation(extractOperation, TaskExecutionMode.Execute);

                // deploy the created dacpac
                var deployParams = new DeployParams
                {
                    PackageFilePath = extractParams.PackageFilePath,
                    DatabaseName = string.Concat(sourceDb.DatabaseName, "-deployed"),
                    UpgradeExisting = false
                };

                DeployOperation deployOperation = new DeployOperation(deployParams, result.ConnectionInfo);
                service.PerformOperation(deployOperation, TaskExecutionMode.Execute);
                targetDb = SqlTestDb.CreateFromExisting(deployParams.DatabaseName);

                VerifyAndCleanup(extractParams.PackageFilePath);
            }
            finally
            {
                sourceDb.Cleanup();
                if (targetDb != null)
                {
                    targetDb.Cleanup();
                }
            }
            return;
        }

        /// <summary>
        /// Verify the export request being cancelled
        /// </summary>
        [Test]
        public async Task ExportBacpacCancellationTest()
        {
            var result = GetLiveAutoCompleteTestObjects();
            SqlTestDb testdb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxExportTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            try
            {
                var exportParams = new ExportParams
                {
                    DatabaseName = testdb.DatabaseName,
                    PackageFilePath = Path.Combine(folderPath, string.Format("{0}.bacpac", testdb.DatabaseName))
                };

                DacFxService service = new DacFxService();
                ExportOperation operation = new ExportOperation(exportParams, result.ConnectionInfo);

                // set cancellation token to cancel
                operation.Cancel();
                OperationCanceledException expectedException = null;

                try
                {
                    service.PerformOperation(operation, TaskExecutionMode.Execute);
                }
                catch (OperationCanceledException canceledException)
                {
                    expectedException = canceledException;
                }

                Assert.NotNull(expectedException);
            }
            finally
            {
                testdb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the generate deploy script request
        /// </summary>
        [Test]
        public async Task GenerateDeployScript()
        {
            // first extract a dacpac
            var result = GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "DacFxGenerateScriptTest");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxGenerateScriptTest");

            try
            {
                DacFxService service = new DacFxService();
                string dacpacPath = InitialExtract(service, sourceDb, result);

                // generate script
                var generateScriptParams = new GenerateDeployScriptParams
                {
                    PackageFilePath = dacpacPath,
                    DatabaseName = targetDb.DatabaseName
                };

                // Generate script for deploying source dacpac to target db
                GenerateDeployScriptOperation generateScriptOperation = new GenerateDeployScriptOperation(generateScriptParams, result.ConnectionInfo);
                service.PerformOperation(generateScriptOperation, TaskExecutionMode.Script);

                // Verify script was generated
                Assert.That(generateScriptOperation.Result.DatabaseScript, Is.Not.Empty);
                Assert.That(generateScriptOperation.Result.DatabaseScript, Does.Contain("CREATE TABLE"));

                VerifyAndCleanup(dacpacPath);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the generate deploy plan request
        /// </summary>
        [Test]
        public async Task GenerateDeployPlan()
        {
            var result = GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "DacFxGenerateDeployPlanTest");
            SqlTestDb targetDb = null;

            try
            {
                DacFxService service = new DacFxService();
                string dacpacPath = InitialExtract(service, sourceDb, result);

                // generate deploy plan for deploying dacpac to targetDb
                targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "DacFxGenerateDeployPlanTestTarget");

                var generateDeployPlanParams = new GenerateDeployPlanParams
                {
                    PackageFilePath = dacpacPath,
                    DatabaseName = targetDb.DatabaseName,
                };

                GenerateDeployPlanOperation generateDeployPlanOperation = new GenerateDeployPlanOperation(generateDeployPlanParams, result.ConnectionInfo);
                service.PerformOperation(generateDeployPlanOperation, TaskExecutionMode.Execute);
                string report = generateDeployPlanOperation.DeployReport;
                Assert.NotNull(report);
                Assert.Multiple(() =>
                {
                    Assert.That(report, Does.Contain("Create"));
                    Assert.That(report, Does.Contain("Drop"));
                    Assert.That(report, Does.Contain("Alter"));
                });

                VerifyAndCleanup(dacpacPath);
            }
            finally
            {
                sourceDb.Cleanup();
                if (targetDb != null)
                {
                    targetDb.Cleanup();
                }
            }
        }

        // <summary>
        /// Verify that SqlCmdVars are set correctly for a deploy request
        /// </summary>
        [Test]
        public async Task DeployWithSqlCmdVariables()
        {
            var result = GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, query: storedProcScript, dbNamePrefix: "DacFxDeploySqlCmdVarsTest");
            SqlTestDb targetDb = null;

            try
            {
                DacFxService service = new DacFxService();
                // First extract a db to have a dacpac to deploy later
                string dacpacPath = InitialExtract(service, sourceDb, result);

                // Deploy the created dacpac with SqlCmdVars
                var deployParams = new DeployParams
                {
                    PackageFilePath = dacpacPath,
                    DatabaseName = string.Concat(sourceDb.DatabaseName, "-deployed"),
                    UpgradeExisting = false,
                    SqlCommandVariableValues = new Dictionary<string, string>()
                    {
                        { databaseRefVarName, "OtherDatabase" },
                        { filterValueVarName, "Employee" }
                    }
                };

                DeployOperation deployOperation = new DeployOperation(deployParams, result.ConnectionInfo);
                service.PerformOperation(deployOperation, TaskExecutionMode.Execute);
                targetDb = SqlTestDb.CreateFromExisting(deployParams.DatabaseName);

                string deployedProc;

                using (SqlConnection conn = new SqlConnection(targetDb.ConnectionString))
                {
                    try
                    {
                        await conn.OpenAsync();
                        deployedProc = (string)ReliableConnectionHelper.ExecuteScalar(conn, "SELECT OBJECT_DEFINITION (OBJECT_ID(N'Procedure1'));");
                    }
                    finally
                    {
                        conn.Close();
                    }
                }

                Assert.That(deployedProc, Does.Contain(deployParams.SqlCommandVariableValues[databaseRefVarName]));
                Assert.That(deployedProc, Does.Contain(deployParams.SqlCommandVariableValues[filterValueVarName]));

                VerifyAndCleanup(dacpacPath);
            }
            finally
            {
                sourceDb.Cleanup();
                if (targetDb != null)
                {
                    targetDb.Cleanup();
                }
            }
        }

        // <summary>
        /// Verify that SqlCmdVars are set correctly for a generate script request
        /// </summary>
        [Test]
        public async Task GenerateDeployScriptWithSqlCmdVariables()
        {
            var result = GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, query: storedProcScript, dbNamePrefix: "DacFxGenerateScriptSqlCmdVarsTest");

            try
            {
                DacFxService service = new DacFxService();
                // First extract a db to have a dacpac to generate the script for later
                string dacpacPath = InitialExtract(service, sourceDb, result);

                // Generate script for deploying source dacpac to target db with SqlCmdVars
                var generateScriptParams = new GenerateDeployScriptParams
                {
                    PackageFilePath = dacpacPath,
                    DatabaseName = string.Concat(sourceDb.DatabaseName, "-generated"),
                    SqlCommandVariableValues = new Dictionary<string, string>()
                    {
                        { databaseRefVarName, "OtherDatabase" },
                        { filterValueVarName, "Employee" }
                    }
                };

                GenerateDeployScriptOperation generateScriptOperation = new GenerateDeployScriptOperation(generateScriptParams, result.ConnectionInfo);
                service.PerformOperation(generateScriptOperation, TaskExecutionMode.Script);

                // Verify the SqlCmdVars were set correctly in the script
                Assert.That(generateScriptOperation.Result.DatabaseScript, Is.Not.Empty);
                Assert.That(generateScriptOperation.Result.DatabaseScript, Does.Contain($":setvar {databaseRefVarName} \"{generateScriptParams.SqlCommandVariableValues[databaseRefVarName]}\""));
                Assert.That(generateScriptOperation.Result.DatabaseScript, Does.Contain($":setvar {filterValueVarName} \"{generateScriptParams.SqlCommandVariableValues[filterValueVarName]}\""));

                VerifyAndCleanup(dacpacPath);
            }
            finally
            {
                sourceDb.Cleanup();
            }
        }

        ///
        /// Verify that options are set correctly for a deploy request
        /// </summary>
        [Test]
        public async Task DeployWithOptions()
        {
            var result = GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, query: SourceScript, dbNamePrefix: "DacFxDeployOptionsTestSource");
            sourceDb.RunQuery(SourceViewScript);
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, query: TargetScript, dbNamePrefix: "DacFxDeployOptionsTestTarget");

            try
            {
                DacFxService service = new DacFxService();
                // First extract a db to have a dacpac to deploy later
                string dacpacPath = InitialExtract(service, sourceDb, result);

                // Deploy the created dacpac with options
                var deployParams = new DeployParams
                {
                    PackageFilePath = dacpacPath,
                    DatabaseName = targetDb.DatabaseName,
                    UpgradeExisting = true,
                    DeploymentOptions = new DeploymentOptions()
                    {
                        DropObjectsNotInSource = false,
                        ExcludeObjectTypes = new[] { ObjectType.Views }
                    }
                };

                // expect table3 to not have been dropped and view1 to not have been created
                await VerifyDeployWithOptions(deployParams, targetDb, service, result.ConnectionInfo, expectedTableResult: "table3", expectedViewResult: null);

                // Deploy the created dacpac with options
                var deployNoOptionsParams = new DeployParams
                {
                    PackageFilePath = dacpacPath,
                    DatabaseName = targetDb.DatabaseName,
                    UpgradeExisting = true
                };

                // expect table3 to be dropped and view1 created
                await VerifyDeployWithOptions(deployNoOptionsParams, targetDb, service, result.ConnectionInfo, expectedTableResult: null, expectedViewResult: "view1");

                VerifyAndCleanup(dacpacPath);
            }
            finally
            {
                sourceDb.Cleanup();
                if (targetDb != null)
                {
                    targetDb.Cleanup();
                }
            }
        }

        private async Task VerifyDeployWithOptions(DeployParams deployParams, SqlTestDb targetDb, DacFxService service, ConnectionInfo connInfo, string expectedTableResult, string expectedViewResult)
        {
            var deployOperation = new DeployOperation(deployParams, connInfo);
            service.PerformOperation(deployOperation, TaskExecutionMode.Execute);

            using (SqlConnection conn = new SqlConnection(targetDb.ConnectionString))
            {
                try
                {
                    await conn.OpenAsync();
                    var deployedResult = (string)ReliableConnectionHelper.ExecuteScalar(conn, $"SELECT TABLE_NAME FROM {targetDb.DatabaseName}.INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'table3'; ");
                    Assert.AreEqual(expectedTableResult, deployedResult);

                    deployedResult = (string)ReliableConnectionHelper.ExecuteScalar(conn, $"SELECT TABLE_NAME FROM {targetDb.DatabaseName}.INFORMATION_SCHEMA.VIEWS WHERE TABLE_NAME = 'view1'; ");
                    Assert.AreEqual(expectedViewResult, deployedResult);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        // <summary>
        /// Verify that options are set correctly for a generate script request
        /// </summary>
        [Test]
        public async Task GenerateDeployScriptWithOptions()
        {
            var result = GetLiveAutoCompleteTestObjects();
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, query: SourceScript, dbNamePrefix: "DacFxDeployOptionsTestSource");
            sourceDb.RunQuery(SourceViewScript);
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, query: TargetScript, dbNamePrefix: "DacFxDeployOptionsTestTarget");

            try
            {
                DacFxService service = new DacFxService();
                // First extract a db to have a dacpac to deploy later
                string dacpacPath = InitialExtract(service, sourceDb, result);

                // generate script to deploy the created dacpac with options
                var generateScriptFalseOptionParams = new GenerateDeployScriptParams
                {
                    PackageFilePath = dacpacPath,
                    DatabaseName = targetDb.DatabaseName,
                    DeploymentOptions = new DeploymentOptions()
                    {
                        DropObjectsNotInSource = false,
                        ExcludeObjectTypes = new[] { ObjectType.Views }
                    }
                };

                var generateScriptFalseOptionOperation = new GenerateDeployScriptOperation(generateScriptFalseOptionParams, result.ConnectionInfo);
                service.PerformOperation(generateScriptFalseOptionOperation, TaskExecutionMode.Execute);

                Assert.That(generateScriptFalseOptionOperation.Result.DatabaseScript, Does.Not.Contain("table3"));
                Assert.That(generateScriptFalseOptionOperation.Result.DatabaseScript, Does.Not.Contain("CREATE VIEW"));

                // try to deploy with the option set to true to make sure it works
                var generateScriptTrueOptionParams = new GenerateDeployScriptParams
                {
                    PackageFilePath = dacpacPath,
                    DatabaseName = targetDb.DatabaseName,
                    DeploymentOptions = new DeploymentOptions()
                    {
                        DropObjectsNotInSource = true,
                        ExcludeObjectTypes = new[] { ObjectType.Views }
                    }
                };

                var generateScriptTrueOptionOperation = new GenerateDeployScriptOperation(generateScriptTrueOptionParams, result.ConnectionInfo);
                service.PerformOperation(generateScriptTrueOptionOperation, TaskExecutionMode.Execute);

                Assert.That(generateScriptTrueOptionOperation.Result.DatabaseScript, Does.Contain("DROP TABLE [dbo].[table3]"));
                Assert.That(generateScriptTrueOptionOperation.Result.DatabaseScript, Does.Not.Contain("CREATE VIEW"));

                // now generate script without options
                var generateScriptNoOptionsParams = new GenerateDeployScriptParams
                {
                    PackageFilePath = dacpacPath,
                    DatabaseName = targetDb.DatabaseName,
                };

                var generateScriptNoOptionsOperation = new GenerateDeployScriptOperation(generateScriptNoOptionsParams, result.ConnectionInfo);
                service.PerformOperation(generateScriptNoOptionsOperation, TaskExecutionMode.Execute);

                Assert.That(generateScriptNoOptionsOperation.Result.DatabaseScript, Does.Contain("table3"));
                Assert.That(generateScriptNoOptionsOperation.Result.DatabaseScript, Does.Contain("CREATE VIEW"));

                VerifyAndCleanup(dacpacPath);
            }
            finally
            {
                sourceDb.Cleanup();
                if (targetDb != null)
                {
                    targetDb.Cleanup();
                }
            }
        }

        // <summary>
        /// Verify that options can get retrieved from publish profile
        /// </summary>
        [Test]
        public async Task GetOptionsFromProfile()
        {
            DeploymentOptions expectedResults = DeploymentOptions.GetDefaultPublishOptions();

            expectedResults.ExcludeObjectTypes = null;
            expectedResults.IncludeCompositeObjects = true;
            expectedResults.BlockOnPossibleDataLoss = true;
            expectedResults.AllowIncompatiblePlatform = true;

            var dacfxRequestContext = new Mock<RequestContext<DacFxOptionsResult>>();
            dacfxRequestContext.Setup((RequestContext<DacFxOptionsResult> x) => x.SendResult(It.Is<DacFxOptionsResult>((result) => ValidateOptions(expectedResults, result.DeploymentOptions) == true))).Returns(Task.FromResult(new object()));

            DacFxService service = new DacFxService();
            string file = Path.Combine(publishProfileFolder, "profileWithOptions.publish.xml");

            var getOptionsFromProfileParams = new GetOptionsFromProfileParams
            {
                ProfilePath = file
            };

            await service.HandleGetOptionsFromProfileRequest(getOptionsFromProfileParams, dacfxRequestContext.Object);
            dacfxRequestContext.VerifyAll();
        }

        // <summary>
        /// Verify that default options are returned if a profile doesn't specify any options
        /// </summary>
        [Test]
        public async Task GetOptionsFromProfileWithoutOptions()
        {
            DeploymentOptions expectedResults = DeploymentOptions.GetDefaultPublishOptions();
            expectedResults.ExcludeObjectTypes = null;

            var dacfxRequestContext = new Mock<RequestContext<DacFxOptionsResult>>();
            dacfxRequestContext.Setup((RequestContext<DacFxOptionsResult> x) => x.SendResult(It.Is<DacFxOptionsResult>((result) => ValidateOptions(expectedResults, result.DeploymentOptions) == true))).Returns(Task.FromResult(new object()));

            DacFxService service = new DacFxService();
            string file = Path.Combine(publishProfileFolder, "profileNoOptions.publish.xml");

            var getOptionsFromProfileParams = new GetOptionsFromProfileParams
            {
                ProfilePath = file
            };

            await service.HandleGetOptionsFromProfileRequest(getOptionsFromProfileParams, dacfxRequestContext.Object);
            dacfxRequestContext.VerifyAll();
        }

        /// <summary>
        /// Verify the default dacFx options for publishing
        /// </summary>
        [Test]
        public async Task ValidateGetDefaultPublishOptionsCallFromService()
        {
            DeploymentOptions expectedResults = DeploymentOptions.GetDefaultPublishOptions();

            var dacfxRequestContext = new Mock<RequestContext<DacFxOptionsResult>>();
            dacfxRequestContext.Setup((RequestContext<DacFxOptionsResult> x) => x.SendResult(It.Is<DacFxOptionsResult>((result) => ValidateOptions(expectedResults, result.DeploymentOptions) == true))).Returns(Task.FromResult(new object()));

            GetDefaultPublishOptionsParams p = new GetDefaultPublishOptionsParams();

            DacFxService service = new DacFxService();
            await service.HandleGetDefaultPublishOptionsRequest(p, dacfxRequestContext.Object);
        }

        /// <summary>
        /// Verify that streaming job
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task ValidateStreamingJob()
        {
            var dacfxRequestContext = new Mock<RequestContext<ValidateStreamingJobResult>>();
            DacFxService service = new DacFxService();

            ValidateStreamingJobResult expectedResult;

            // Positive case: both input and output are present

            expectedResult = new ValidateStreamingJobResult() { Success = true };
            dacfxRequestContext.Setup((RequestContext<ValidateStreamingJobResult> x) => x.SendResult(It.Is<ValidateStreamingJobResult>((result) => ValidateStreamingJobErrors(expectedResult, result) == true))).Returns(Task.FromResult(new object()));

            ValidateStreamingJobParams parameters = new ValidateStreamingJobParams()
            {
                PackageFilePath = Path.Combine(dacpacsFolder, "StreamingJobTestDb.dacpac"),
                CreateStreamingJobTsql = goodCreateStreamingJob
            };

            await service.HandleValidateStreamingJobRequest(parameters, dacfxRequestContext.Object);
            dacfxRequestContext.VerifyAll();

            // Negative case: input and output streams are both missing from model

            const string errorMessage = @"Validation for external streaming job 'myJob' failed:
Streaming query statement contains a reference to missing input stream 'MissingEdgeHubInputStream'.  You must add it to the database model.
Streaming query statement contains a reference to missing output stream 'MissingSqlOutputStream'.  You must add it to the database model.";
            expectedResult = new ValidateStreamingJobResult() { Success = false, ErrorMessage = errorMessage };
            dacfxRequestContext.Setup((RequestContext<ValidateStreamingJobResult> x) => x.SendResult(It.Is<ValidateStreamingJobResult>((result) => ValidateStreamingJobErrors(expectedResult, result)))).Returns(Task.FromResult(new object()));

            parameters = new ValidateStreamingJobParams()
            {
                PackageFilePath = Path.Combine(dacpacsFolder, "StreamingJobTestDb.dacpac"),
                CreateStreamingJobTsql = missingCreateBothStreamingJob
            };

            await service.HandleValidateStreamingJobRequest(parameters, dacfxRequestContext.Object);
            dacfxRequestContext.VerifyAll();
        }

        private bool ValidateStreamingJobErrors(ValidateStreamingJobResult expected, ValidateStreamingJobResult actual)
        {
            return expected.Success == actual.Success
                && expected.ErrorMessage == actual.ErrorMessage;
        }

        private bool ValidateOptions(DeploymentOptions expected, DeploymentOptions actual)
        {
            System.Reflection.PropertyInfo[] deploymentOptionsProperties = expected.GetType().GetProperties();
            foreach (var v in deploymentOptionsProperties)
            {
                var defaultP = v.GetValue(expected);
                var actualP = v.GetValue(actual);

                if (v.Name == "ExcludeObjectTypes")
                {
                    Assert.True((defaultP as ObjectType[])?.Length == (actualP as ObjectType[])?.Length, "Number of excluded objects is different not equal");
                }
                else
                {
                    Assert.True((defaultP == null && actualP == null) || (defaultP == null && (actualP as string) == string.Empty) || defaultP.Equals(actualP), $"Actual Property from Service is not equal to default property for {v.Name}, Actual value: {actualP} and Default value: {defaultP}");
                }
            }

            return true;
        }

        private string InitialExtract(DacFxService service, SqlTestDb sourceDb, LiveConnectionHelper.TestConnectionResult result)
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            var extractParams = new ExtractParams
            {
                DatabaseName = sourceDb.DatabaseName,
                PackageFilePath = Path.Combine(folderPath, string.Format("{0}.dacpac", sourceDb.DatabaseName)),
                ApplicationName = "test",
                ApplicationVersion = "1.0.0.0"
            };

            ExtractOperation extractOperation = new ExtractOperation(extractParams, result.ConnectionInfo);
            service.PerformOperation(extractOperation, TaskExecutionMode.Execute);

            return extractParams.PackageFilePath;
        }

        private void VerifyAndCleanup(string filePath)
        {
            // Verify it was created
            Assert.True(File.Exists(filePath));

            // Remove the file
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
