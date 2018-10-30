using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
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
                ConnectionString = testdb.ConnectionString,
                PackageFileName = Path.Combine(folderPath, string.Format("{0}.bacpac", testdb.DatabaseName))
            };

            DacFxService service = new DacFxService();
            await service.HandleExportRequest(exportParams, requestContext.Object);

            testdb.Cleanup();

            return requestContext;
        }

        private async Task<Mock<RequestContext<DacFxImportResult>>> SendAndValidateDacFxImportRequest()
        {
            // first export a db to have a bacpac to import later
            var exportRequestContext = new Mock<RequestContext<DacFxExportResult>>();
            exportRequestContext.Setup(x => x.SendResult(It.IsAny<DacFxExportResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxImportTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            var exportParams = new DacFxExportParams
            {
                ConnectionString = sourceDb.ConnectionString,
                PackageFileName = Path.Combine(folderPath, string.Format("{0}.bacpac", sourceDb.DatabaseName))
            };

            DacFxService service = new DacFxService();
            await service.HandleExportRequest(exportParams, exportRequestContext.Object);

            var result = GetLiveAutoCompleteTestObjects();
            var importRequestContext = new Mock<RequestContext<DacFxImportResult>>();
            importRequestContext.Setup(x => x.SendResult(It.IsAny<DacFxImportResult>())).Returns(Task.FromResult(new object()));


            var importParams = new DacFxImportParams
            {
                ConnectionString = ConnectionService.BuildConnectionString(result.ConnectionInfo.ConnectionDetails),
                PackageFilePath = exportParams.PackageFileName,
                TargetDatabaseName = string.Concat(sourceDb.DatabaseName, "-import")
            };

            await service.HandleImportRequest(importParams, importRequestContext.Object);
            SqlTestDb targetDb = SqlTestDb.CreateFromExisting(importParams.TargetDatabaseName);

            // cleanup both created dbs
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
                ConnectionString = testdb.ConnectionString,
                PackageFileName = Path.Combine(folderPath, string.Format("{0}.dacpac", testdb.DatabaseName)),
                ApplicationName = "test",
                ApplicationVersion = new Version(1, 0)
            };

            DacFxService service = new DacFxService();
            await service.HandleExtractRequest(extractParams, requestContext.Object);

            testdb.Cleanup();

            return requestContext;
        }

        private async Task<Mock<RequestContext<DacFxDeployResult>>> SendAndValidateDacFxDeployRequest()
        {
            // first extract a db to have a dacpac to import later
            var extractRequestContext = new Mock<RequestContext<DacFxExtractResult>>();
            extractRequestContext.Setup(x => x.SendResult(It.IsAny<DacFxExtractResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxDeployTest");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            var extractParams = new DacFxExtractParams
            {
                ConnectionString = sourceDb.ConnectionString,
                PackageFileName = Path.Combine(folderPath, string.Format("{0}.dacpac", sourceDb.DatabaseName)),
                ApplicationName = "test",
                ApplicationVersion = new Version(1, 0)
            };

            DacFxService service = new DacFxService();
            await service.HandleExtractRequest(extractParams, extractRequestContext.Object);

            var result = GetLiveAutoCompleteTestObjects();
            var deployRequestContext = new Mock<RequestContext<DacFxDeployResult>>();
            deployRequestContext.Setup(x => x.SendResult(It.IsAny<DacFxDeployResult>())).Returns(Task.FromResult(new object()));


            var deployParams = new DacFxDeployParams
            {
                ConnectionString = ConnectionService.BuildConnectionString(result.ConnectionInfo.ConnectionDetails),
                PackageFilePath = extractParams.PackageFileName,
                TargetDatabaseName = string.Concat(sourceDb.DatabaseName, "-deploy")
            };

            await service.HandleDeployRequest(deployParams, deployRequestContext.Object);
            SqlTestDb targetDb = SqlTestDb.CreateFromExisting(deployParams.TargetDatabaseName);

            // cleanup both created dbs
            sourceDb.Cleanup();
            targetDb.Cleanup();

            return deployRequestContext;
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

    }
}
