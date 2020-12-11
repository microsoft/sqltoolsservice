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
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ModelManagement
{
    public class ModelManagementServiceTests : ServiceTestBase
    {

        [Test]
        public async Task VerifyDeleteModelRequest()
        {
            DeleteModelRequestParams requestParams = new DeleteModelRequestParams
            {
                DatabaseName = "db name",
                SchemaName = "dbo",
                TableName = "table name",
                ModelId = 1
            };
            Mock<ModelOperations> operations = new Mock<ModelOperations>();
            operations.Setup(x => x.DeleteModel(It.IsAny<IDbConnection>(), requestParams));
            ModelManagementService service = new ModelManagementService()
            {
                ModelOperations = operations.Object
            };

            await VerifyRequst<DeleteModelResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   requestParams.OwnerUri = connectionUrl;
                   await service.HandleDeleteModelRequest(requestParams, requestContext);
                   return null;
               },
               verify: (actual =>
               {
                   Assert.NotNull(actual);
                   Assert.True(actual.Success);
                   Assert.True(string.IsNullOrWhiteSpace(actual.ErrorMessage));
               }));
        }

        [Test]
        public async Task VerifyImportModelRequest()
        {
            ImportModelRequestParams requestParams = new ImportModelRequestParams
            {
                DatabaseName = "db name",
                SchemaName = "dbo",
                TableName = "table name",
                Model = new ModelMetadata()
            };
            Mock<ModelOperations> operations = new Mock<ModelOperations>();
            operations.Setup(x => x.ImportModel(It.IsAny<IDbConnection>(), requestParams));
            ModelManagementService service = new ModelManagementService()
            {
                ModelOperations = operations.Object
            };

            await VerifyRequst<ImportModelResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   requestParams.OwnerUri = connectionUrl;
                   await service.HandleModelImportRequest(requestParams, requestContext);
                   return null;
               },
               verify: (actual =>
               {
                   Assert.NotNull(actual);
                   Assert.True(actual.Success);
                   Assert.True(string.IsNullOrWhiteSpace(actual.ErrorMessage));
               }));
        }

        [Test]
        public async Task VerifyUpdateModelRequest()
        {
            UpdateModelRequestParams requestParams = new UpdateModelRequestParams
            {
                DatabaseName = "db name",
                SchemaName = "dbo",
                TableName = "table name",
                Model = new ModelMetadata()
            };
            Mock<ModelOperations> operations = new Mock<ModelOperations>();
            operations.Setup(x => x.UpdateModel(It.IsAny<IDbConnection>(), requestParams));
            ModelManagementService service = new ModelManagementService()
            {
                ModelOperations = operations.Object
            };

            await VerifyRequst<UpdateModelResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   requestParams.OwnerUri = connectionUrl;
                   await service.HandleUpdateModelRequest(requestParams, requestContext);
                   return null;
               },
               verify: (actual =>
               {
                   Assert.NotNull(actual);
                   Assert.True(actual.Success);
                   Assert.True(string.IsNullOrWhiteSpace(actual.ErrorMessage));
               }));
        }

        [Test]
        public async Task VerifyDownloadModelRequest()
        {
            DownloadModelRequestParams requestParams = new DownloadModelRequestParams
            {
                DatabaseName = "db name",
                SchemaName = "dbo",
                TableName = "table name",
                ModelId = 1
            };
            Mock<ModelOperations> operations = new Mock<ModelOperations>();
            operations.Setup(x => x.DownloadModel(It.IsAny<IDbConnection>(), requestParams)).Returns(() => "file path");
            ModelManagementService service = new ModelManagementService()
            {
                ModelOperations = operations.Object
            };

            await VerifyRequst<DownloadModelResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   requestParams.OwnerUri = connectionUrl;
                   await service.HandleDownloadModelRequest(requestParams, requestContext);
                   return null;
               },
               verify: (actual =>
               {
                   Assert.NotNull(actual);
                   Assert.AreEqual(actual.FilePath, "file path");
                   Assert.True(actual.Success);
                   Assert.True(string.IsNullOrWhiteSpace(actual.ErrorMessage));
               }));
        }

        [Test]
        public async Task VerifyModelTableRequest()
        {
            VerifyModelTableRequestParams requestParams = new VerifyModelTableRequestParams
            {
                DatabaseName = "db name",
                SchemaName = "dbo",
                TableName = "table name"
            };
            Mock<ModelOperations> operations = new Mock<ModelOperations>();
            operations.Setup(x => x.VerifyImportTable(It.IsAny<IDbConnection>(), requestParams)).Returns(() => true);
            ModelManagementService service = new ModelManagementService()
            {
                ModelOperations = operations.Object
            };

            await VerifyRequst<VerifyModelTableResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   requestParams.OwnerUri = connectionUrl;
                   await service.HandleVerifyModelTableRequest(requestParams, requestContext);
                   return null;
               },
               verify: (actual =>
               {
                   Assert.NotNull(actual);
                   Assert.AreEqual(actual.Verified, true);
                   Assert.True(actual.Success);
                   Assert.True(string.IsNullOrWhiteSpace(actual.ErrorMessage));
               }));
        }

        [Test]
        public async Task VerifyConfigureModelTableRequest()
        {
            ConfigureModelTableRequestParams requestParams = new ConfigureModelTableRequestParams
            {
                DatabaseName = "db name",
                SchemaName = "dbo",
                TableName = "table name"
            };
            Mock<ModelOperations> operations = new Mock<ModelOperations>();
            operations.Setup(x => x.ConfigureImportTable(It.IsAny<IDbConnection>(), requestParams));
            ModelManagementService service = new ModelManagementService()
            {
                ModelOperations = operations.Object
            };

            await VerifyRequst<ConfigureModelTableResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   requestParams.OwnerUri = connectionUrl;
                   await service.HandleConfigureModelTableRequest(requestParams, requestContext);
                   return null;
               },
               verify: (actual =>
               {
                   Assert.NotNull(actual);
                   Assert.True(actual.Success);
                   Assert.True(string.IsNullOrWhiteSpace(actual.ErrorMessage));
               }));
        }

        [Test]
        public async Task VerifyGetModelRequest()
        {
            GetModelsRequestParams requestParams = new GetModelsRequestParams
            {
                DatabaseName = "db name",
                SchemaName = "dbo",
                TableName = "table name",
            };
            Mock<ModelOperations> operations = new Mock<ModelOperations>();
            operations.Setup(x => x.GetModels(It.IsAny<IDbConnection>(), requestParams)).Returns(() => new List<ModelMetadata> { new ModelMetadata() });
            ModelManagementService service = new ModelManagementService()
            {
                ModelOperations = operations.Object
            };

            await VerifyRequst<GetModelsResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   requestParams.OwnerUri = connectionUrl;
                   await service.HandleGetModelsRequest(requestParams, requestContext);
                   return null;
               },
               verify: (actual =>
               {
                   Assert.NotNull(actual);
                   Assert.True(actual.Models.Count == 1);
                   Assert.True(actual.Success);
                   Assert.True(string.IsNullOrWhiteSpace(actual.ErrorMessage));
               }));
        }

        [Test]
        public async Task VerifyRequestFailedResponse()
        {
            DeleteModelRequestParams requestParams = new DeleteModelRequestParams
            {
                DatabaseName = "db name",
                SchemaName = "dbo",
                TableName = "table name",
                ModelId = 1
            };
            Mock<ModelOperations> operations = new Mock<ModelOperations>();
            operations.Setup(x => x.DeleteModel(It.IsAny<IDbConnection>(), requestParams)).Throws(new ApplicationException("error"));
            ModelManagementService service = new ModelManagementService()
            {
                ModelOperations = operations.Object
            };

            await VerifyRequst<DeleteModelResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   requestParams.OwnerUri = connectionUrl;
                   await service.HandleDeleteModelRequest(requestParams, requestContext);
                   return null;
               },
               verify: (actual =>
               {
                   Assert.NotNull(actual);
                   Assert.False(actual.Success);
                   Assert.False(string.IsNullOrWhiteSpace(actual.ErrorMessage));
               }));
        }

        [Test]
        public async Task VerifyInvalidConnectionResponse()
        {
            DeleteModelRequestParams requestParams = new DeleteModelRequestParams
            {
                DatabaseName = "db name",
                SchemaName = "dbo",
                TableName = "table name",
                ModelId = 1
            };
            Mock<ModelOperations> operations = new Mock<ModelOperations>();
            operations.Setup(x => x.DeleteModel(It.IsAny<IDbConnection>(), requestParams)).Throws(new ApplicationException("error"));
            ModelManagementService service = new ModelManagementService()
            {
                ModelOperations = operations.Object
            };

            await VerifyRequst<DeleteModelResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   requestParams.OwnerUri = "Invalid connection uri";
                   await service.HandleDeleteModelRequest(requestParams, requestContext);
                   return null;
               },
               verify: (actual =>
               {
                   Assert.NotNull(actual);
                   Assert.False(actual.Success);
                   Assert.False(string.IsNullOrWhiteSpace(actual.ErrorMessage));
               }));
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

        protected override RegisteredServiceProvider CreateServiceProviderWithMinServices()
        {
            return base.CreateProvider();
        }
    }
}
