//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ServerConfigurations;
using Microsoft.SqlTools.ServiceLayer.ServerConfigurations.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamJsonRpc;

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

                int newValue = sampleConfig.ConfigValue == sampleConfig.Minimum ? sampleConfig.Maximum : sampleConfig.Minimum;

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

                ServerConfigViewResponseParams result = await ServerConfigService.Instance.HandleServerConfigViewRequest(requestParams);
                Assert.NotNull(result);
                Assert.AreEqual(result.ConfigProperty.ConfigValue, sampleConfig.ConfigValue);
                ServerConfigUpdateResponseParams updateResult = await ServerConfigService.Instance.HandleServerConfigUpdateRequest(updateRequestParams);
                Assert.NotNull(updateResult);
                Assert.AreEqual(updateResult.ConfigProperty.ConfigValue, newValue);
                updateRequestParams.ConfigValue = sampleConfig.ConfigValue;
                updateResult = await ServerConfigService.Instance.HandleServerConfigUpdateRequest(updateRequestParams);
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

                ServerConfigListRequestParams requestParams = new ServerConfigListRequestParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                };

                ServerConfigListResponseParams result = await ServerConfigService.Instance.HandleServerConfigListRequest(requestParams);
                Assert.NotNull(result);
                return result.ConfigProperties;
            }
        }

       
        [Test]
        public void VerifyConfigViewRequestThrowsRpcErrorGivenInvalidConnection()
        {
            ServerConfigViewRequestParams requestParams = new ServerConfigViewRequestParams
            {
                OwnerUri = "invalid uri"
            };

            Assert.ThrowsAsync<LocalRpcException>(async () =>
                await ServerConfigService.Instance.HandleServerConfigViewRequest(requestParams));
        }

        [Test]
        public void VerifyConfigUpdateRequestThrowsRpcErrorGivenInvalidConnection()
        {
            ServerConfigUpdateRequestParams requestParams = new ServerConfigUpdateRequestParams
            {
                OwnerUri = "invalid uri",
                ConfigValue = 1
            };

            Assert.ThrowsAsync<LocalRpcException>(async () =>
                await ServerConfigService.Instance.HandleServerConfigUpdateRequest(requestParams));
        }

        [Test]
        public void VerifyConfigListRequestThrowsRpcErrorGivenInvalidConnection()
        {
            ServerConfigListRequestParams requestParams = new ServerConfigListRequestParams
            {
                OwnerUri = "invalid uri",
            };

            Assert.ThrowsAsync<LocalRpcException>(async () =>
                await ServerConfigService.Instance.HandleServerConfigListRequest(requestParams));
        }

    }
}
