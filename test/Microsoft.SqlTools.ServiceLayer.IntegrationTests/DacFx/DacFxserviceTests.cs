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

        private async Task<Mock<RequestContext<DacFxExportResult>>> SendAndValidateDacFxExportRequest()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<DacFxExportResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<DacFxExportResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb testdb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxExportTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            var exportParams = new DacFxExportParams
            {
                DatabaseName = testdb.DatabaseName,
                PackageFilePath = Path.Combine(folderPath, string.Format("{0}.bacpac", testdb.DatabaseName))
            };

            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(result.ConnectionInfo, "Export");
            DacFxService service = new DacFxService();
            DacFxExportOperation operation = new DacFxExportOperation(exportParams, sqlConn);
            service.PerformOperation(operation);

            // cleanup
            VerifyAndCleanup(exportParams.PackageFilePath);
            testdb.Cleanup();

            return requestContext;
        }

        private async Task<Mock<RequestContext<DacFxImportResult>>> SendAndValidateDacFxImportRequest()
        {
            // first export a bacpac
            var result = GetLiveAutoCompleteTestObjects();
            var exportRequestContext = new Mock<RequestContext<DacFxExportResult>>();
            exportRequestContext.Setup(x => x.SendResult(It.IsAny<DacFxExportResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxImportTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            var exportParams = new DacFxExportParams
            {
                DatabaseName = sourceDb.DatabaseName,
                PackageFilePath = Path.Combine(folderPath, string.Format("{0}.bacpac", sourceDb.DatabaseName))
            };

            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(result.ConnectionInfo, "Import");
            DacFxService service = new DacFxService();
            DacFxExportOperation exportOperation = new DacFxExportOperation(exportParams, sqlConn);
            service.PerformOperation(exportOperation);

            // import the created bacpac
            var importRequestContext = new Mock<RequestContext<DacFxImportResult>>();
            importRequestContext.Setup(x => x.SendResult(It.IsAny<DacFxImportResult>())).Returns(Task.FromResult(new object()));

            var importParams = new DacFxImportParams
            {
                PackageFilePath = exportParams.PackageFilePath,
                TargetDatabaseName = string.Concat(sourceDb.DatabaseName, "-imported")
            };

            DacFxImportOperation importOperation = new DacFxImportOperation(importParams, sqlConn);
            service.PerformOperation(importOperation);
            SqlTestDb targetDb = SqlTestDb.CreateFromExisting(importParams.TargetDatabaseName);

            // cleanup
            VerifyAndCleanup(exportParams.PackageFilePath);
            sourceDb.Cleanup();
            targetDb.Cleanup();

            return importRequestContext;
        }

        private async Task<Mock<RequestContext<DacFxExtractResult>>> SendAndValidateDacFxExtractRequest()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<DacFxExtractResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<DacFxExtractResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb testdb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxExtractTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            var extractParams = new DacFxExtractParams
            {
                DatabaseName = testdb.DatabaseName,
                PackageFilePath = Path.Combine(folderPath, string.Format("{0}.dacpac", testdb.DatabaseName)),
                ApplicationName = "test",
                ApplicationVersion = new Version(1, 0)
            };

            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(result.ConnectionInfo, "Extract");
            DacFxService service = new DacFxService();
            DacFxExtractOperation operation = new DacFxExtractOperation(extractParams, sqlConn);
            service.PerformOperation(operation);

            // cleanup
            VerifyAndCleanup(extractParams.PackageFilePath);
            testdb.Cleanup();

            return requestContext;
        }

        private async Task<Mock<RequestContext<DacFxDeployResult>>> SendAndValidateDacFxDeployRequest()
        {
            // first extract a db to have a dacpac to import later
            var result = GetLiveAutoCompleteTestObjects();
            var extractRequestContext = new Mock<RequestContext<DacFxExtractResult>>();
            extractRequestContext.Setup(x => x.SendResult(It.IsAny<DacFxExtractResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxDeployTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            var extractParams = new DacFxExtractParams
            {
                DatabaseName = sourceDb.DatabaseName,
                PackageFilePath = Path.Combine(folderPath, string.Format("{0}.dacpac", sourceDb.DatabaseName)),
                ApplicationName = "test",
                ApplicationVersion = new Version(1, 0)
            };

            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(result.ConnectionInfo, "Deploy");
            DacFxService service = new DacFxService();
            DacFxExtractOperation extractOperation = new DacFxExtractOperation(extractParams, sqlConn);
            service.PerformOperation(extractOperation);

            // deploy the created dacpac
            var deployRequestContext = new Mock<RequestContext<DacFxDeployResult>>();
            deployRequestContext.Setup(x => x.SendResult(It.IsAny<DacFxDeployResult>())).Returns(Task.FromResult(new object()));


            var deployParams = new DacFxDeployParams
            {
                PackageFilePath = extractParams.PackageFilePath,
                TargetDatabaseName = string.Concat(sourceDb.DatabaseName, "-deployed")
            };

            DacFxDeployOperation deployOperation = new DacFxDeployOperation(deployParams, sqlConn);
            service.PerformOperation(deployOperation); SqlTestDb targetDb = SqlTestDb.CreateFromExisting(deployParams.TargetDatabaseName);

            // cleanup
            VerifyAndCleanup(extractParams.PackageFilePath);
            sourceDb.Cleanup();
            targetDb.Cleanup();

            return deployRequestContext;
        }

        private async Task<Mock<RequestContext<DacFxExportResult>>> ValidateDacFxExportCancellation()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<DacFxExportResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<DacFxExportResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb testdb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxExportTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            var exportParams = new DacFxExportParams
            {
                DatabaseName = testdb.DatabaseName,
                PackageFilePath = Path.Combine(folderPath, string.Format("{0}.bacpac", testdb.DatabaseName))
            };

            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(result.ConnectionInfo, "Export");
            DacFxService service = new DacFxService();
            DacFxExportOperation operation = new DacFxExportOperation(exportParams, sqlConn);

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
        /// Verify the DacFx export request
        /// </summary>
        [Fact]
        public async void DacFxExport()
        {
            Assert.NotNull(await SendAndValidateDacFxExportRequest());
        }

        /// <summary>
        /// Verify the DacFx export request being cancelled
        /// </summary>
        [Fact]
        public async void DacFxExportCancellationTest()
        {
            Assert.NotNull(await ValidateDacFxExportCancellation());
        }

        /// <summary>
        /// Verify the DacFx import request
        /// </summary>
        [Fact]
        public async void DacFxImport()
        {
            Assert.NotNull(await SendAndValidateDacFxImportRequest());
        }

        /// <summary>
        /// Verify the DacFx extract request
        /// </summary>
        [Fact]
        public async void DacFxExtract()
        {
            Assert.NotNull(await SendAndValidateDacFxExtractRequest());
        }

        /// <summary>
        /// Verify the DacFx deploy request
        /// </summary>
        [Fact]
        public async void DacFxDeploy()
        {
            Assert.NotNull(await SendAndValidateDacFxDeployRequest());
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
