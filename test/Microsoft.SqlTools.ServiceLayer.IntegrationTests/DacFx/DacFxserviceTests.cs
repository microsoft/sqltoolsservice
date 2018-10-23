using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
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
            var scriptingParams = new DacFxExportParams
            {
                ConnectionString = testdb.ConnectionString
            };

            DacFxService service = new DacFxService();
            await service.HandleExportRequest(scriptingParams, requestContext.Object);

            testdb.Cleanup();

            return requestContext;
        }

        /// <summary>
        /// Verify the DacFx object request
        /// </summary>
        [Fact]
        public async void DacFxExport()
        {
            Assert.NotNull(await SendAndValidateDacFxExportRequest());
        }
    }
}
