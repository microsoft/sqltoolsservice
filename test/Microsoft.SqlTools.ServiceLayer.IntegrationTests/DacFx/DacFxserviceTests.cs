using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Moq;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.DacFx
{
    public class DacFxServiceTests
    {
        private async Task<Mock<RequestContext<DacFxExportResult>>> SendAndValidateDacFxExportRequest()
        {
            var requestContext = new Mock<RequestContext<DacFxExportResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<DacFxExportResult>())).Returns(Task.FromResult(new object()));
            var builder = new SqlConnectionStringBuilder();
            builder.DataSource = "KISANTIA-HQ";
            builder.InitialCatalog = "customerrepro";
            builder.UserID = "test";
            builder.Password = "Yukon901";

            var scriptingParams = new DacFxExportParams
            {
                ConnectionString = builder.ConnectionString
            };

            DacFxService service = new DacFxService();
            await service.HandleExportRequest(scriptingParams, requestContext.Object);

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
