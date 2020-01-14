//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.LanguageExtensions;
using Microsoft.SqlTools.ServiceLayer.LanguageExtensions.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Moq;
using System;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.LanguageExtensions
{
    public class LanguageExtensionsTests
    {
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

                await LanguageExtensionsService.Instance.HandleExternalLanguageStatusRequest(requestParams, requestContext.Object);
                Assert.NotNull(result);

                LanguageExtensionsService.Instance.ConnectionServiceInstance.Disconnect(new DisconnectParams
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

            await LanguageExtensionsService.Instance.HandleExternalLanguageStatusRequest(requestParams, requestContext.Object);
            requestContext.Verify(x => x.SendError(It.IsAny<Exception>()));
        }

    }
}
