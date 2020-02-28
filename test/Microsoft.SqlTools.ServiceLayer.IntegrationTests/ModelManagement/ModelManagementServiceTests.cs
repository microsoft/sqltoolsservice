//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ModelManagement;
using Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ModelManagement
{
    public class ModelManagementServiceTests : ServiceTestBase
    {
        private const string databaseName = "ModelsDatabase";
        private const string tableName = "ModelsTable";

        [Fact]
        public async void VerifyAddRequest()
        {
            RegisteredModel model = new RegisteredModel
            {
                Name = Guid.NewGuid().ToString(),
                Description = "description",
                Version = "1.1.1"
            };
            await AddOrUpdateModel(model);
            await VerifyModelList(actual =>
            {
                if (model != null)
                {
                    Assert.True(actual.Models.Any(x => x.Name == model.Name));
                }
            });
            await DeleteModel(model);
        }

        [Fact]
        public async void VerifyListRequest()
        {
            await VerifyModelList(actual =>
            {
            });
        }

        [Fact]
        public async void VerifyDeleteRequest()
        {
            RegisteredModel model = new RegisteredModel
            {
                Name = Guid.NewGuid().ToString(),
                Description = "description",
                Version = "1.1.1"
            };
            await AddOrUpdateModel(model);
            await VerifyModelList(actual =>
            {
                if (model != null)
                {
                    Assert.True(actual.Models.Any(x => x.Name == model.Name));
                }
            });
            await DeleteModel(model);
            await VerifyModelList(actual =>
            {
                if (model != null)
                {
                    Assert.True(actual.Models.Any(x => x.Name != model.Name));
                }
            });
        }

        [Fact]
        public async void VerifyUpdateRequest()
        {
            RegisteredModel model = new RegisteredModel
            {
                Name = Guid.NewGuid().ToString(),
                Description = "description",
                Version = "1.1.1"
            };
            await AddOrUpdateModel(model);
            await VerifyModelList(actual =>
            {
                if (model != null)
                {
                    Assert.True(actual.Models.Any(x => x.Name == model.Name));
                }
            });
            model.Version = "1.1.2";
            await AddOrUpdateModel(model);
            await VerifyModelList(actual =>
            {
                if (model != null)
                {
                    var updatedModel = actual.Models.FirstOrDefault(x => x.Name == model.Name);
                    Assert.True(updatedModel.Version == model.Version);
                }
            });
            await DeleteModel(model);
        }


        private async Task AddOrUpdateModel(RegisteredModel model)
        {
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            {
                ModelManagementService service = new ModelManagementService();
                model.FilePath = tempFile.FilePath;

                await VerifyRequst<ModelUpdateResponseParams>(
                   test: async (requestContext, connectionUrl) =>
                   {
                       ModelUpdateRequestParams requestParams = new ModelUpdateRequestParams
                       {
                           OwnerUri = connectionUrl,
                           DatabaseName = databaseName,
                           TableName = tableName,
                           Model = model
                       };
                       await service.HandleModelUpdateRequest(requestParams, requestContext);
                       return null;
                   },
                   verify: (actual =>
                   {
                       Assert.NotNull(actual);
                   }));
            }
        }

        private async Task DeleteModel(RegisteredModel model)
        {
            ModelManagementService service = new ModelManagementService();

            await VerifyRequst<ModelDeleteResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   ModelDeleteRequestParams requestParams = new ModelDeleteRequestParams
                   {
                       OwnerUri = connectionUrl,
                       DatabaseName = databaseName,
                       TableName = tableName,
                       Name = model.Name
                   };
                   await service.HandleModelDeleteRequest(requestParams, requestContext);
                   return null;
               },
               verify: (actual =>
               {
                   Assert.NotNull(actual);
               }));
        }

        private async Task VerifyModelList(Action<ModelListResponseParams> verify)
        {
            ModelManagementService service = new ModelManagementService();

            await VerifyRequst<ModelListResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   ModelListRequestParams requestParams = new ModelListRequestParams
                   {
                       OwnerUri = connectionUrl,
                       DatabaseName = databaseName,
                       TableName = tableName
                   };
                   await service.HandleModelListRequest(requestParams, requestContext);
                   return null;
               },
               verify: (actual =>
               {
                   Assert.NotNull(actual);
                   verify(actual);
               }));
        }

        [Fact]
        public async void VerifyDeleteRequestConnectionFailures()
        {
            RegisteredModel model = new RegisteredModel
            {
                Name = "name"
            };

            ModelManagementService service = new ModelManagementService();
            await VerifyError<ModelDeleteResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   ModelDeleteRequestParams requestParams = new ModelDeleteRequestParams
                   {
                       OwnerUri = "invalid connection",
                       Name = model.Name
                   };
                   await service.HandleModelDeleteRequest(requestParams, requestContext);
                   return null;
               });
        }

        [Fact]
        public async void VerifyUpdateRequestConnectionFailures()
        {
            RegisteredModel model = new RegisteredModel
            {
                Name = "name"
            };

            ModelManagementService service = new ModelManagementService();
            await VerifyError<ModelUpdateResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   ModelUpdateRequestParams requestParams = new ModelUpdateRequestParams
                   {
                       OwnerUri = "invalid connection",
                       Model = model
                   };
                   await service.HandleModelUpdateRequest(requestParams, requestContext);
                   return null;
               });
        }

        [Fact]
        public async void VerifyListRequestConnectionFailures()
        {
            ModelManagementService service = new ModelManagementService();
            await VerifyError<ModelListResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   ModelListRequestParams requestParams = new ModelListRequestParams
                   {
                       OwnerUri = "invalid connection"
                   };
                   await service.HandleModelListRequest(requestParams, requestContext);
                   return null;
               });
        }

        public async Task VerifyRequst<T>(Func<RequestContext<T>, string, Task<T>> test, Action<T> verify)
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                await RunAndVerify<T>(
               test: (requestContext) => test(requestContext, queryTempFile.FilePath),
               verify: verify);

                ModelManagementService.Instance.ConnectionServiceInstance.Disconnect(new DisconnectParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    Type = ServiceLayer.Connection.ConnectionType.Default
                });
            }
        }

        public async Task VerifyError<T>(Func<RequestContext<T>, string, Task<T>> test)
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                await RunAndVerifyError<T>(
               test: (requestContext) => test(requestContext, queryTempFile.FilePath));

                ModelManagementService.Instance.ConnectionServiceInstance.Disconnect(new DisconnectParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    Type = ServiceLayer.Connection.ConnectionType.Default
                });
            }
        }

        protected override RegisteredServiceProvider CreateServiceProviderWithMinServices()
        {
            return base.CreateProvider();
        }
    }
}
