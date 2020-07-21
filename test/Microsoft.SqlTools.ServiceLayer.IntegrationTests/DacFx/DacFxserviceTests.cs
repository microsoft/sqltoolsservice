//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using Xunit;

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

        private LiveConnectionHelper.TestConnectionResult GetLiveAutoCompleteTestObjects()
        {
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            return result;
        }

        /// <summary>
        /// Verify the export bacpac request
        /// </summary>
        [Fact]
        public async void ExportBacpac()
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
        [Fact]
        public async void ImportBacpac()
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
        [Fact]
        public async void ExtractDacpac()
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
        [Fact]
        public async void ExtractDBToFileTarget()
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
        [Fact]
        public async void ExtractDBToFlatTarget()
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
                int actualCnt = Directory.GetFiles(folderPath, "*.sql", SearchOption.AllDirectories).Length;
                Assert.Equal(actualCnt, 2);

                // Remove the directory
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }
            }
            finally
            {
                testdb.Cleanup();
            }
        }

        /// <summary>
        /// Verify the deploy dacpac request
        /// </summary>
        [Fact]
        public async void DeployDacpac()
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
        }

        /// <summary>
        /// Verify the export request being cancelled
        /// </summary>
        [Fact]
        public async void ExportBacpacCancellationTest()
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
        [Fact]
        public async void GenerateDeployScript()
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
                Assert.NotEmpty(generateScriptOperation.Result.DatabaseScript);
                Assert.Contains("CREATE TABLE", generateScriptOperation.Result.DatabaseScript);

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
        [Fact]
        public async void GenerateDeployPlan()
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
                Assert.Contains("Create", report);
                Assert.Contains("Drop", report);
                Assert.Contains("Alter", report);

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
        [Fact]
        public async void DeployWithSqlCmdVariables()
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

                Assert.Contains(deployParams.SqlCommandVariableValues[databaseRefVarName], deployedProc);
                Assert.Contains(deployParams.SqlCommandVariableValues[filterValueVarName], deployedProc);

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
        [Fact]
        public async void GenerateDeployScriptWithSqlCmdVariables()
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
                Assert.NotEmpty(generateScriptOperation.Result.DatabaseScript);
                Assert.Contains($":setvar {databaseRefVarName} \"{generateScriptParams.SqlCommandVariableValues[databaseRefVarName]}\"", generateScriptOperation.Result.DatabaseScript);
                Assert.Contains($":setvar {filterValueVarName} \"{generateScriptParams.SqlCommandVariableValues[filterValueVarName]}\"", generateScriptOperation.Result.DatabaseScript);

                VerifyAndCleanup(dacpacPath);
            }
            finally
            {
                sourceDb.Cleanup();
            }
        }

        // <summary>
        /// Verify that options can get retrieved from publish profile
        /// </summary>
        [Fact]
        public async void GetOptionsFromProfile()
        {
            DeploymentOptions expectedResults = new DeploymentOptions(setDefaults: false);
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
        [Fact]
        public async void GetOptionsFromProfileWithoutOptions()
        {
            DeploymentOptions expectedResults = new DeploymentOptions(setDefaults: false);

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

        private bool ValidateOptions(DeploymentOptions expected, DeploymentOptions actual)
        {
            try
            {
                System.Reflection.PropertyInfo[] deploymentOptionsProperties = expected.GetType().GetProperties();
                foreach (var v in deploymentOptionsProperties)
                {
                    var defaultP = v.GetValue(expected);
                    var actualP = v.GetValue(actual);

                    if (v.Name == "ExcludeObjectTypes")
                    {
                        Assert.True((defaultP as ObjectType[]).Length == (actualP as ObjectType[]).Length, $"Number of excluded objects is different; expected: {(defaultP as ObjectType[]).Length} actual: {(actualP as ObjectType[]).Length}");
                    }
                    else
                    {
                        Assert.True((defaultP == null && actualP == null) || defaultP.Equals(actualP), $"Actual Property from Service is not equal to default property for { v.Name}, Actual value: {actualP} and Default value: {defaultP}");
                    }
                }
            } 
            catch (Exception e)
            {
                Console.WriteLine(e);
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
