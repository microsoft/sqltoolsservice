//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.DacFx
{
    public class DacFxServiceTests
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

        private LiveConnectionHelper.TestConnectionResult GetLiveAutoCompleteTestObjects()
        {
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            return result;
        }

        private async Task<Mock<RequestContext<DacFxResult>>> SendAndValidateExportRequest()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<DacFxResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<DacFxResult>())).Returns(Task.FromResult(new object()));

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

            return requestContext;
        }

        private async Task<Mock<RequestContext<DacFxResult>>> SendAndValidateImportRequest()
        {
            // first export a bacpac
            var result = GetLiveAutoCompleteTestObjects();
            var importRequestContext = new Mock<RequestContext<DacFxResult>>();
            importRequestContext.Setup(x => x.SendResult(It.IsAny<DacFxResult>())).Returns(Task.FromResult(new object()));

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
            return importRequestContext;
        }

        private async Task<Mock<RequestContext<DacFxResult>>> SendAndValidateExtractRequest()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<DacFxResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<DacFxResult>())).Returns(Task.FromResult(new object()));

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
            return requestContext;
        }

        private async Task<Mock<RequestContext<DacFxResult>>> SendAndValidateDeployRequest()
        {
            // first extract a db to have a dacpac to import later
            var result = GetLiveAutoCompleteTestObjects();
            var deployRequestContext = new Mock<RequestContext<DacFxResult>>();
            deployRequestContext.Setup(x => x.SendResult(It.IsAny<DacFxResult>())).Returns(Task.FromResult(new object()));

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
            return deployRequestContext;
        }

        private async Task<Mock<RequestContext<DacFxResult>>> ValidateExportCancellation()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<DacFxResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<DacFxResult>())).Returns(Task.FromResult(new object()));

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
            return requestContext;
        }



        private async Task<Mock<RequestContext<DacFxResult>>> SendAndValidateGenerateDeployScriptRequest()
        {
            // first extract a dacpac
            var result = GetLiveAutoCompleteTestObjects();
            var generateScriptRequestContext = new Mock<RequestContext<DacFxResult>>();
            generateScriptRequestContext.Setup(x => x.SendResult(It.IsAny<DacFxResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "DacFxGenerateScriptTest");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxGenerateScriptTest");

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

                // generate script
                var generateScriptParams = new GenerateDeployScriptParams
                {
                    PackageFilePath = extractParams.PackageFilePath,
                    DatabaseName = targetDb.DatabaseName
                };

                // Generate script for deploying source dacpac to target db
                GenerateDeployScriptOperation generateScriptOperation = new GenerateDeployScriptOperation(generateScriptParams, result.ConnectionInfo);
                service.PerformOperation(generateScriptOperation, TaskExecutionMode.Script);

                // Verify script was generated
                Assert.NotEmpty(generateScriptOperation.Result.DatabaseScript);
                Assert.Contains("CREATE TABLE", generateScriptOperation.Result.DatabaseScript);

                VerifyAndCleanup(extractParams.PackageFilePath);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
            return generateScriptRequestContext;
        }

        private async Task<Mock<RequestContext<DacFxResult>>> SendAndValidateGenerateDeployPlanRequest()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var generateDeployPlanRequestContext = new Mock<RequestContext<DacFxResult>>();
            generateDeployPlanRequestContext.Setup(x => x.SendResult(It.IsAny<DacFxResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "DacFxGenerateDeployPlanTest");
            SqlTestDb targetDb = null;
            DacFxService service = new DacFxService();
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

                ExtractOperation extractOperation = new ExtractOperation(extractParams, result.ConnectionInfo);
                service.PerformOperation(extractOperation, TaskExecutionMode.Execute);

                // generate deploy plan for deploying dacpac to targetDb
                targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "DacFxGenerateDeployPlanTestTarget");

                var generateDeployPlanParams = new GenerateDeployPlanParams
                {
                    PackageFilePath = extractParams.PackageFilePath,
                    DatabaseName = targetDb.DatabaseName,
                };

                GenerateDeployPlanOperation generateDeployPlanOperation = new GenerateDeployPlanOperation(generateDeployPlanParams, result.ConnectionInfo);
                service.PerformOperation(generateDeployPlanOperation, TaskExecutionMode.Execute);
                string report = generateDeployPlanOperation.DeployReport;
                Assert.NotNull(report);
                Assert.Contains("Create", report);
                Assert.Contains("Drop", report);
                Assert.Contains("Alter", report);

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
            return generateDeployPlanRequestContext;
        }

        /// <summary>
        /// Verify the export bacpac request
        /// </summary>
        [Fact]
        public async void ExportBacpac()
        {
            Assert.NotNull(await SendAndValidateExportRequest());
        }

        /// <summary>
        /// Verify the export request being cancelled
        /// </summary>
        [Fact]
        public async void ExportBacpacCancellationTest()
        {
            Assert.NotNull(await ValidateExportCancellation());
        }

        /// <summary>
        /// Verify the import bacpac request
        /// </summary>
        [Fact]
        public async void ImportBacpac()
        {
            Assert.NotNull(await SendAndValidateImportRequest());
        }

        /// <summary>
        /// Verify the extract dacpac request
        /// </summary>
        [Fact]
        public async void ExtractDacpac()
        {
            Assert.NotNull(await SendAndValidateExtractRequest());
        }

        /// <summary>
        /// Verify the deploy dacpac request
        /// </summary>
        [Fact]
        public async void DeployDacpac()
        {
            Assert.NotNull(await SendAndValidateDeployRequest());
        }

        /// <summary>
        /// Verify the generate deploy script request
        /// </summary>
        [Fact]
        public async void GenerateDeployScript()
        {
            Assert.NotNull(await SendAndValidateGenerateDeployScriptRequest());
        }

        /// <summary>
        /// Verify the generate deploy plan request
        /// </summary>
        [Fact]
        public async void GenerateDeployPlan()
        {
            Assert.NotNull(await SendAndValidateGenerateDeployPlanRequest());
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
