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

            var scriptingParams = new DacFxExportParams
            {
                ConnectionString = testdb.ConnectionString,
                PackageFileName = Path.Combine(folderPath, string.Format("{0}.bacpac", testdb.DatabaseName))
            };

            DacFxService service = new DacFxService();
            await service.HandleExportRequest(scriptingParams, requestContext.Object);

            testdb.Cleanup();

            return requestContext;
        }

        private async Task<Mock<RequestContext<DacFxImportResult>>> SendAndValidateDacFxImportRequest()
        {
            // first export a db to have a bacpac to import later
            var exportRequestContext = new Mock<RequestContext<DacFxExportResult>>();
            exportRequestContext.Setup(x => x.SendResult(It.IsAny<DacFxExportResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "DacFxExportTest");
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

    }
}
