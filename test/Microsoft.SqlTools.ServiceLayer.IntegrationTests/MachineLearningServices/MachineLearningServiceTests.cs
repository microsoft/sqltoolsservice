using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.MachineLearningServices;
using Microsoft.SqlTools.ServiceLayer.MachineLearningServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Moq;
using System;
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
                    Status = true
                };

                await MachineLearningService.Instance.HandleExternalScriptConfigUpdateRequest(updateRequestParams, updateRequestContext.Object);
                Assert.NotNull(updateResult);
                await MachineLearningService.Instance.HandleExternalScriptConfigStatusRequest(requestParams, requestContext.Object);
                Assert.NotNull(result);
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
        public async void VerifyExternalLanguageStatusRequestSendErrorGivenInvalidConnection()
        {
            ExternalLanguageStatusResponseParams result = null;
            var requestContext = RequestContextMocks.Create<ExternalLanguageStatusResponseParams>(r => result = r).AddErrorHandling(null);
            requestContext.Setup(x => x.SendError(It.IsAny<Exception>())).Returns(System.Threading.Tasks.Task.FromResult(true));

            ExternalLanguageStatusRequestParams requestParams = new ExternalLanguageStatusRequestParams
            {
                OwnerUri = "invalid uri",
                LanguageName = "Python"
            };

            await MachineLearningService.Instance.HandleExternalLanguageStatusRequest(requestParams, requestContext.Object);
            requestContext.Verify(x => x.SendError(It.IsAny<Exception>()));
        }

        [Fact]
        public async void VerifyExternalScriptConfigStatusRequestSendErrorGivenInvalidConnection()
        {
            ExternalScriptConfigStatusResponseParams result = null;
            var requestContext = RequestContextMocks.Create<ExternalScriptConfigStatusResponseParams>(r => result = r).AddErrorHandling(null);
            requestContext.Setup(x => x.SendError(It.IsAny<Exception>())).Returns(System.Threading.Tasks.Task.FromResult(true));

            ExternalScriptConfigStatusRequestParams requestParams = new ExternalScriptConfigStatusRequestParams
            {
                OwnerUri = "invalid uri"
            };

            await MachineLearningService.Instance.HandleExternalScriptConfigStatusRequest(requestParams, requestContext.Object);
            requestContext.Verify(x => x.SendError(It.IsAny<Exception>()));
        }

        [Fact]
        public async void VerifyExternalScriptConfigUpdateRequestSendErrorGivenInvalidConnection()
        {
            ExternalScriptConfigUpdateResponseParams result = null;
            var requestContext = RequestContextMocks.Create<ExternalScriptConfigUpdateResponseParams>(r => result = r).AddErrorHandling(null);
            requestContext.Setup(x => x.SendError(It.IsAny<Exception>())).Returns(System.Threading.Tasks.Task.FromResult(true));

            ExternalScriptConfigUpdateRequestParams requestParams = new ExternalScriptConfigUpdateRequestParams
            {
                OwnerUri = "invalid uri",
                Status = true
            };

            await MachineLearningService.Instance.HandleExternalScriptConfigUpdateRequest(requestParams, requestContext.Object);
            requestContext.Verify(x => x.SendError(It.IsAny<Exception>()));
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
