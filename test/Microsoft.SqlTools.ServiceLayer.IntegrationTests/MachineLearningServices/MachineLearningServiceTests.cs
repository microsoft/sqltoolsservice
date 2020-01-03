using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.MachineLearningServices;
using Microsoft.SqlTools.ServiceLayer.MachineLearningServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.MachineLearningServices
{
    public class MachineLearningServiceTests
    {
        [Fact]
        public async void VerifyEnablingExternalScript()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                ExternalScriptConfigStatusResponseParams result = null;
                ExternalScriptConfigUpdateResponseParams updateResult = null;
               
                var requestContext = RequestContextMocks.Create<ExternalScriptConfigStatusResponseParams>(r => result = r).AddErrorHandling(null);
                var updateRequestContext = RequestContextMocks.Create<ExternalScriptConfigUpdateResponseParams>(r => updateResult = r).AddErrorHandling(null);
                
                ExternalScriptConfigStatusRequestParams requestParams = new ExternalScriptConfigStatusRequestParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                };
                ExternalScriptConfigUpdateRequestParams updateRequestParams = new ExternalScriptConfigUpdateRequestParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Status = false
                };

                await MachineLearningService.Instance.HandleExternalScriptConfigUpdateRequest(updateRequestParams, updateRequestContext.Object);
                await MachineLearningService.Instance.HandleExternalScriptConfigStatusRequest(requestParams, requestContext.Object);
                Assert.False(result.Status);

                updateRequestParams.Status = true;
                await MachineLearningService.Instance.HandleExternalScriptConfigUpdateRequest(updateRequestParams, updateRequestContext.Object);
                await MachineLearningService.Instance.HandleExternalScriptConfigStatusRequest(requestParams, requestContext.Object);
                Assert.True(result.Status);
                MachineLearningService.Instance.ConnectionServiceInstance.Disconnect(new DisconnectParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    Type = ServiceLayer.Connection.ConnectionType.Default
                });
            }
        }

        [Fact]
        public async void VerifyExternalLanguageStatusRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                ExternalLanguageStatusResponseParams result = null;
                var requestContext = RequestContextMocks.Create<ExternalLanguageStatusResponseParams>(r => result = r).AddErrorHandling(null);

                ExternalLanguageStatusRequestParams requestParams = new ExternalLanguageStatusRequestParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    LanguageName = "Python"
                };
             
                await MachineLearningService.Instance.HandleExternalLanguageStatusRequest(requestParams, requestContext.Object);
                Assert.NotNull(result);

                MachineLearningService.Instance.ConnectionServiceInstance.Disconnect(new DisconnectParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    Type = ServiceLayer.Connection.ConnectionType.Default
                });
            }
        }

        [Fact]
        public async void VerifyEnablingExternalScriptNotAvaialbleInAzure()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath, ServiceLayer.Connection.ConnectionType.Default, TestServerType.Azure);

                ExternalScriptConfigStatusResponseParams result = null;
                ExternalScriptConfigUpdateResponseParams updateResult = null;

                var requestContext = RequestContextMocks.Create<ExternalScriptConfigStatusResponseParams>(r => result = r).AddErrorHandling(null);
                var updateRequestContext = RequestContextMocks.Create<ExternalScriptConfigUpdateResponseParams>(r => updateResult = r).AddErrorHandling(null);

                ExternalScriptConfigStatusRequestParams requestParams = new ExternalScriptConfigStatusRequestParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                };
                ExternalScriptConfigUpdateRequestParams updateRequestParams = new ExternalScriptConfigUpdateRequestParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Status = true
                };

                await MachineLearningService.Instance.HandleExternalScriptConfigUpdateRequest(updateRequestParams, updateRequestContext.Object);
                Assert.False(updateResult.Result);
                MachineLearningService.Instance.ConnectionServiceInstance.Disconnect(new DisconnectParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    Type = ServiceLayer.Connection.ConnectionType.Default
                });
            }
        }
    }
}
