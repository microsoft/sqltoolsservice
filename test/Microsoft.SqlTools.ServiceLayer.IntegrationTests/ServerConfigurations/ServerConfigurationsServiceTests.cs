//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ServerConfigurations;
using Microsoft.SqlTools.ServiceLayer.ServerConfigurations.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.MachineLearningServices
{
    public class ServerConfigurationsServiceTests
    {
        [Test]
        public async Task VerifyListingConfigs()
        {
            List<ServerConfigProperty> configs = await GetAllConfigs();
            Assert.NotNull(configs);
            Assert.True(configs.Count > 0);
        }

        [Test]
        public async Task VerifyUpdatingConfigs()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                List<ServerConfigProperty> configs = await GetAllConfigs();
                Assert.NotNull(configs);
                Assert.True(configs.Count > 0);
                ServerConfigProperty sampleConfig = configs[0];

                ServerConfigViewResponseParams result = null;
                ServerConfigUpdateResponseParams updateResult = null;
                int newValue = sampleConfig.ConfigValue == sampleConfig.Minimum ? sampleConfig.Maximum : sampleConfig.Minimum;

                var requestContext = RequestContextMocks.Create<ServerConfigViewResponseParams>(r => result = r).AddErrorHandling(null);
                var updateRequestContext = RequestContextMocks.Create<ServerConfigUpdateResponseParams>(r => updateResult = r).AddErrorHandling(null);

                ServerConfigViewRequestParams requestParams = new ServerConfigViewRequestParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    ConfigNumber = sampleConfig.Number
                };
                ServerConfigUpdateRequestParams updateRequestParams = new ServerConfigUpdateRequestParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    ConfigNumber = sampleConfig.Number,
                    ConfigValue = newValue
                };

                await ServerConfigService.Instance.HandleServerConfigViewRequest(requestParams, requestContext.Object);
                Assert.NotNull(result);
                Assert.AreEqual(result.ConfigProperty.ConfigValue, sampleConfig.ConfigValue);
                await ServerConfigService.Instance.HandleServerConfigUpdateRequest(updateRequestParams, updateRequestContext.Object);
                Assert.NotNull(updateResult);
                Assert.AreEqual(updateResult.ConfigProperty.ConfigValue, newValue);
                updateRequestParams.ConfigValue = sampleConfig.ConfigValue;
                await ServerConfigService.Instance.HandleServerConfigUpdateRequest(updateRequestParams, updateRequestContext.Object);
                Assert.NotNull(updateResult);
                Assert.AreEqual(updateResult.ConfigProperty.ConfigValue, sampleConfig.ConfigValue);
                ServerConfigService.Instance.ConnectionServiceInstance.Disconnect(new DisconnectParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    Type = ServiceLayer.Connection.ConnectionType.Default
                });
            }
        }

        public async Task<List<ServerConfigProperty>> GetAllConfigs()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                ServerConfigListResponseParams result = null;

                var requestContext = RequestContextMocks.Create<ServerConfigListResponseParams>(r => result = r).AddErrorHandling(null);

                ServerConfigListRequestParams requestParams = new ServerConfigListRequestParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                };

                await ServerConfigService.Instance.HandleServerConfigListRequest(requestParams, requestContext.Object);
                Assert.NotNull(result);
                return result.ConfigProperties;
            }
        }

       
        [Test]
        public async Task VerifyConfigViewRequestSendErrorGivenInvalidConnection()
        {
            ServerConfigViewResponseParams result = null;
            var requestContext = RequestContextMocks.Create<ServerConfigViewResponseParams>(r => result = r).AddErrorHandling(null);
            requestContext.Setup(x => x.SendError(It.IsAny<Exception>())).Returns(System.Threading.Tasks.Task.FromResult(true));

            ServerConfigViewRequestParams requestParams = new ServerConfigViewRequestParams
            {
                OwnerUri = "invalid uri"
            };

            await ServerConfigService.Instance.HandleServerConfigViewRequest(requestParams, requestContext.Object);
            requestContext.Verify(x => x.SendError(It.IsAny<Exception>()));
        }

        [Test]
        public async Task VerifyConfigUpdateRequestSendErrorGivenInvalidConnection()
        {
            ServerConfigUpdateResponseParams result = null;
            var requestContext = RequestContextMocks.Create<ServerConfigUpdateResponseParams>(r => result = r).AddErrorHandling(null);
            requestContext.Setup(x => x.SendError(It.IsAny<Exception>())).Returns(System.Threading.Tasks.Task.FromResult(true));

            ServerConfigUpdateRequestParams requestParams = new ServerConfigUpdateRequestParams
            {
                OwnerUri = "invalid uri",
                ConfigValue = 1
            };

            await ServerConfigService.Instance.HandleServerConfigUpdateRequest(requestParams, requestContext.Object);
            requestContext.Verify(x => x.SendError(It.IsAny<Exception>()));
        }

        [Test]
        public async Task VerifyConfigListRequestSendErrorGivenInvalidConnection()
        {
            ServerConfigListResponseParams result = null;
            var requestContext = RequestContextMocks.Create<ServerConfigListResponseParams>(r => result = r).AddErrorHandling(null);
            requestContext.Setup(x => x.SendError(It.IsAny<Exception>())).Returns(System.Threading.Tasks.Task.FromResult(true));

            ServerConfigListRequestParams requestParams = new ServerConfigListRequestParams
            {
                OwnerUri = "invalid uri",
            };

            await ServerConfigService.Instance.HandleServerConfigListRequest(requestParams, requestContext.Object);
            requestContext.Verify(x => x.SendError(It.IsAny<Exception>()));
        }

    }
}
