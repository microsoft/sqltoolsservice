//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.DacFx
{
    public class DacFxServiceTests
    {

        private LiveConnectionHelper.TestConnectionResult GetLiveAutoCompleteTestObjects()
        {
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            return result;
        }

        private async Task<Mock<RequestContext<ExportResult>>> SendAndValidateExportRequest()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<ExportResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<ExportResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb testdb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxExportTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            var exportParams = new ExportParams
            {
                SourceDatabaseName = testdb.DatabaseName,
                PackageFilePath = Path.Combine(folderPath, string.Format("{0}.bacpac", testdb.DatabaseName))
            };

            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(result.ConnectionInfo, "Export");
            DacFxService service = new DacFxService();
            ExportOperation operation = new ExportOperation(exportParams, sqlConn);
            service.PerformOperation(operation);

            // cleanup
            VerifyAndCleanup(exportParams.PackageFilePath);
            testdb.Cleanup();

            return requestContext;
        }

        private async Task<Mock<RequestContext<ImportResult>>> SendAndValidateImportRequest()
        {
            // first export a bacpac
            var result = GetLiveAutoCompleteTestObjects();
            var exportRequestContext = new Mock<RequestContext<ExportResult>>();
            exportRequestContext.Setup(x => x.SendResult(It.IsAny<ExportResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxImportTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            var exportParams = new ExportParams
            {
                SourceDatabaseName = sourceDb.DatabaseName,
                PackageFilePath = Path.Combine(folderPath, string.Format("{0}.bacpac", sourceDb.DatabaseName))
            };

            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(result.ConnectionInfo, "Import");
            DacFxService service = new DacFxService();
            ExportOperation exportOperation = new ExportOperation(exportParams, sqlConn);
            service.PerformOperation(exportOperation);

            // import the created bacpac
            var importRequestContext = new Mock<RequestContext<ImportResult>>();
            importRequestContext.Setup(x => x.SendResult(It.IsAny<ImportResult>())).Returns(Task.FromResult(new object()));

            var importParams = new ImportParams
            {
                PackageFilePath = exportParams.PackageFilePath,
                TargetDatabaseName = string.Concat(sourceDb.DatabaseName, "-imported")
            };

            ImportOperation importOperation = new ImportOperation(importParams, sqlConn);
            service.PerformOperation(importOperation);
            SqlTestDb targetDb = SqlTestDb.CreateFromExisting(importParams.TargetDatabaseName);

            // cleanup
            VerifyAndCleanup(exportParams.PackageFilePath);
            sourceDb.Cleanup();
            targetDb.Cleanup();

            return importRequestContext;
        }

        private async Task<Mock<RequestContext<ExtractResult>>> SendAndValidateExtractRequest()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<ExtractResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<ExtractResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb testdb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxExtractTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            var extractParams = new ExtractParams
            {
                SourceDatabaseName = testdb.DatabaseName,
                PackageFilePath = Path.Combine(folderPath, string.Format("{0}.dacpac", testdb.DatabaseName)),
                ApplicationName = "test",
                ApplicationVersion = new Version(1, 0)
            };

            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(result.ConnectionInfo, "Extract");
            DacFxService service = new DacFxService();
            ExtractOperation operation = new ExtractOperation(extractParams, sqlConn);
            service.PerformOperation(operation);

            // cleanup
            VerifyAndCleanup(extractParams.PackageFilePath);
            testdb.Cleanup();

            return requestContext;
        }

        private async Task<Mock<RequestContext<DeployResult>>> SendAndValidateDeployRequest()
        {
            // first extract a db to have a dacpac to import later
            var result = GetLiveAutoCompleteTestObjects();
            var extractRequestContext = new Mock<RequestContext<ExtractResult>>();
            extractRequestContext.Setup(x => x.SendResult(It.IsAny<ExtractResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxDeployTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            var extractParams = new ExtractParams
            {
                SourceDatabaseName = sourceDb.DatabaseName,
                PackageFilePath = Path.Combine(folderPath, string.Format("{0}.dacpac", sourceDb.DatabaseName)),
                ApplicationName = "test",
                ApplicationVersion = new Version(1, 0)
            };

            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(result.ConnectionInfo, "Deploy");
            DacFxService service = new DacFxService();
            ExtractOperation extractOperation = new ExtractOperation(extractParams, sqlConn);
            service.PerformOperation(extractOperation);

            // deploy the created dacpac
            var deployRequestContext = new Mock<RequestContext<DeployResult>>();
            deployRequestContext.Setup(x => x.SendResult(It.IsAny<DeployResult>())).Returns(Task.FromResult(new object()));


            var deployParams = new DeployParams
            {
                PackageFilePath = extractParams.PackageFilePath,
                TargetDatabaseName = string.Concat(sourceDb.DatabaseName, "-deployed"),
                UpgradeExisting = false
            };

            DeployOperation deployOperation = new DeployOperation(deployParams, sqlConn);
            service.PerformOperation(deployOperation); SqlTestDb targetDb = SqlTestDb.CreateFromExisting(deployParams.TargetDatabaseName);

            // cleanup
            VerifyAndCleanup(extractParams.PackageFilePath);
            sourceDb.Cleanup();
            targetDb.Cleanup();

            return deployRequestContext;
        }

        private async Task<Mock<RequestContext<ExportResult>>> ValidateExportCancellation()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<ExportResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<ExportResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb testdb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxExportTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            var exportParams = new ExportParams
            {
                SourceDatabaseName = testdb.DatabaseName,
                PackageFilePath = Path.Combine(folderPath, string.Format("{0}.bacpac", testdb.DatabaseName))
            };

            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(result.ConnectionInfo, "Export");
            DacFxService service = new DacFxService();
            ExportOperation operation = new ExportOperation(exportParams, sqlConn);

            // set cancellation token to cancel
            operation.Cancel();
            OperationCanceledException expectedException = null;

            try
            {
                service.PerformOperation(operation);
            }
            catch(OperationCanceledException canceledException)
            {
                expectedException = canceledException;
            }

            Assert.NotNull(expectedException);

            // cleanup
            testdb.Cleanup();

            return requestContext;
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
