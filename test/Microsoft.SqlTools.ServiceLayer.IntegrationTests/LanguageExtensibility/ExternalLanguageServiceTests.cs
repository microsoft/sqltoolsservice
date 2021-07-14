//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.LanguageExtensibility;
using Microsoft.SqlTools.ServiceLayer.LanguageExtensibility.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Microsoft.SqlTools.ServiceLayer.UnitTests;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.LanguageExtensibility
{
    public class ExternalLanguageServiceTests : ServiceTestBase
    {

        [Test]
        public async Task VerifyExternalLanguageStatusRequest()
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

                await ExternalLanguageService.Instance.HandleExternalLanguageStatusRequest(requestParams, requestContext.Object);
                Assert.NotNull(result);

                ExternalLanguageService.Instance.ConnectionServiceInstance.Disconnect(new DisconnectParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    Type = ServiceLayer.Connection.ConnectionType.Default
                });
            }
        }

        [Test]
        public async Task VerifyExternalLanguageDeleteRequest()
        {
            ExternalLanguage language = new ExternalLanguage
            {
                Name = "name"
            };
            Mock<ExternalLanguageOperations> operations = new Mock<ExternalLanguageOperations>();
            operations.Setup(x => x.DeleteLanguage(It.IsAny<IDbConnection>(), language.Name));
            ExternalLanguageService service = new ExternalLanguageService()
            {
                ExternalLanguageOperations = operations.Object
            };
            await VerifyRequst<ExternalLanguageDeleteResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   ExternalLanguageDeleteRequestParams requestParams = new ExternalLanguageDeleteRequestParams
                   {
                       OwnerUri = connectionUrl,
                       LanguageName = language.Name
                   };
                   await service.HandleExternalLanguageDeleteRequest(requestParams, requestContext);
                   return null;
               },
               verify: (actual =>
               {
                   Assert.NotNull(actual);
               }));
        }

        [Test]
        public async Task VerifyExternalLanguageDeleteRequestFailures()
        {
            ExternalLanguage language = new ExternalLanguage
            {
                Name = "name"
            };
            Mock<ExternalLanguageOperations> operations = new Mock<ExternalLanguageOperations>();
            operations.Setup(x => x.DeleteLanguage(It.IsAny<IDbConnection>(), language.Name)).Throws(new Exception("Error"));
            ExternalLanguageService service = new ExternalLanguageService()
            {
                ExternalLanguageOperations = operations.Object
            };
            await VerifyError<ExternalLanguageDeleteResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   ExternalLanguageDeleteRequestParams requestParams = new ExternalLanguageDeleteRequestParams
                   {
                       OwnerUri = connectionUrl,
                       LanguageName = language.Name
                   };
                   await service.HandleExternalLanguageDeleteRequest(requestParams, requestContext);
                   return null;
               });
        }

        [Test]
        public async Task VerifyExternalLanguageDeleteRequestConnectionFailures()
        {
            ExternalLanguage language = new ExternalLanguage
            {
                Name = "name"
            };
            Mock<ExternalLanguageOperations> operations = new Mock<ExternalLanguageOperations>();
            ExternalLanguageService service = new ExternalLanguageService()
            {
                ExternalLanguageOperations = operations.Object
            };
            await VerifyError<ExternalLanguageDeleteResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   ExternalLanguageDeleteRequestParams requestParams = new ExternalLanguageDeleteRequestParams
                   {
                       OwnerUri = "invalid connection",
                       LanguageName = language.Name
                   };
                   await service.HandleExternalLanguageDeleteRequest(requestParams, requestContext);
                   return null;
               });
        }

        [Test]
        public async Task VerifyExternalLanguageUpdateRequest()
        {
            ExternalLanguage language = new ExternalLanguage
            {
                Name = "name"
            };
            Mock<ExternalLanguageOperations> operations = new Mock<ExternalLanguageOperations>();
            operations.Setup(x => x.UpdateLanguage(It.IsAny<IDbConnection>(), language));
            ExternalLanguageService service = new ExternalLanguageService()
            {
                ExternalLanguageOperations = operations.Object
            };
            await VerifyRequst<ExternalLanguageUpdateResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   ExternalLanguageUpdateRequestParams requestParams = new ExternalLanguageUpdateRequestParams
                   {
                       OwnerUri = connectionUrl,
                       Language = language
                   };
                   await service.HandleExternalLanguageUpdateRequest(requestParams, requestContext);
                   return null;
               },
               verify: (actual =>
               {
                   Assert.NotNull(actual);
               }));
        }

        [Test]
        public async Task VerifyExternalLanguageUpdateRequestFailures()
        {
            ExternalLanguage language = new ExternalLanguage
            {
                Name = "name"
            };
            Mock<ExternalLanguageOperations> operations = new Mock<ExternalLanguageOperations>();
            operations.Setup(x => x.UpdateLanguage(It.IsAny<IDbConnection>(), language)).Throws(new Exception("Error"));
            ExternalLanguageService service = new ExternalLanguageService()
            {
                ExternalLanguageOperations = operations.Object
            };
            await VerifyError<ExternalLanguageUpdateResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   ExternalLanguageUpdateRequestParams requestParams = new ExternalLanguageUpdateRequestParams
                   {
                       OwnerUri = connectionUrl,
                       Language = language
                   };
                   await service.HandleExternalLanguageUpdateRequest(requestParams, requestContext);
                   return null;
               });
        }

        [Test]
        public async Task VerifyExternalLanguageUpdateRequestConnectionFailures()
        {
            ExternalLanguage language = new ExternalLanguage
            {
                Name = "name"
            };
            Mock<ExternalLanguageOperations> operations = new Mock<ExternalLanguageOperations>();
            ExternalLanguageService service = new ExternalLanguageService()
            {
                ExternalLanguageOperations = operations.Object
            };
            await VerifyError<ExternalLanguageUpdateResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   ExternalLanguageUpdateRequestParams requestParams = new ExternalLanguageUpdateRequestParams
                   {
                       OwnerUri = "invalid connection",
                       Language = language
                   };
                   await service.HandleExternalLanguageUpdateRequest(requestParams, requestContext);
                   return null;
               });
        }

        [Test]
        public async Task VerifyExternalLanguageListRequest()
        {
            ExternalLanguage language = new ExternalLanguage
            {
                Name = "name"
            };
            Mock<ExternalLanguageOperations> operations = new Mock<ExternalLanguageOperations>();
            operations.Setup(x => x.GetLanguages(It.IsAny<IDbConnection>())).Returns(() => new List<ExternalLanguage> { language });
            ExternalLanguageService service = new ExternalLanguageService()
            {
                ExternalLanguageOperations = operations.Object
            };
            await VerifyRequst<ExternalLanguageListResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   ExternalLanguageListRequestParams requestParams = new ExternalLanguageListRequestParams
                   {
                       OwnerUri = connectionUrl
                   };
                   await service.HandleExternalLanguageListRequest(requestParams, requestContext);
                   return null;
               },
               verify: (actual =>
               {
                   Assert.NotNull(actual);
               }));
        }

        [Test]
        public async Task VerifyExternalLanguagListRequestFailures()
        {
            ExternalLanguage language = new ExternalLanguage
            {
                Name = "name"
            };
            Mock<ExternalLanguageOperations> operations = new Mock<ExternalLanguageOperations>();
            operations.Setup(x => x.GetLanguages(It.IsAny<IDbConnection>())).Throws(new Exception("Error"));
            ExternalLanguageService service = new ExternalLanguageService()
            {
                ExternalLanguageOperations = operations.Object
            };
            await VerifyError<ExternalLanguageListResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   ExternalLanguageListRequestParams requestParams = new ExternalLanguageListRequestParams
                   {
                       OwnerUri = connectionUrl
                   };
                   await service.HandleExternalLanguageListRequest(requestParams, requestContext);
                   return null;
               });
        }

        [Test]
        public async Task VerifyExternalLanguagListRequestConnectionFailures()
        {
            ExternalLanguage language = new ExternalLanguage
            {
                Name = "name"
            };
            Mock<ExternalLanguageOperations> operations = new Mock<ExternalLanguageOperations>();
            ExternalLanguageService service = new ExternalLanguageService()
            {
                ExternalLanguageOperations = operations.Object
            };
            await VerifyError<ExternalLanguageListResponseParams>(
               test: async (requestContext, connectionUrl) =>
               {
                   ExternalLanguageListRequestParams requestParams = new ExternalLanguageListRequestParams
                   {
                       OwnerUri = "invalid connection"
                   };
                   await service.HandleExternalLanguageListRequest(requestParams, requestContext);
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

                ExternalLanguageService.Instance.ConnectionServiceInstance.Disconnect(new DisconnectParams
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

                ExternalLanguageService.Instance.ConnectionServiceInstance.Disconnect(new DisconnectParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    Type = ServiceLayer.Connection.ConnectionType.Default
                });
            }
        }

        [Test]
        public async Task VerifyExternalLanguageStatusRequestSendErrorGivenInvalidConnection()
        {
            ExternalLanguageStatusResponseParams result = null;
            var requestContext = RequestContextMocks.Create<ExternalLanguageStatusResponseParams>(r => result = r).AddErrorHandling(null);
            requestContext.Setup(x => x.SendError(It.IsAny<Exception>())).Returns(Task.FromResult(true));

            ExternalLanguageStatusRequestParams requestParams = new ExternalLanguageStatusRequestParams
            {
                OwnerUri = "invalid uri",
                LanguageName = "Python"
            };

            await ExternalLanguageService.Instance.HandleExternalLanguageStatusRequest(requestParams, requestContext.Object);
            requestContext.Verify(x => x.SendError(It.IsAny<Exception>()));
        }

        protected override RegisteredServiceProvider CreateServiceProviderWithMinServices()
        {
            return base.CreateProvider();
        }
    }
}
