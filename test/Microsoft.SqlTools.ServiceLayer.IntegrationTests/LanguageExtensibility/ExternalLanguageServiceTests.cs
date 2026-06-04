//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.LanguageExtensibility;
using Microsoft.SqlTools.ServiceLayer.LanguageExtensibility.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
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
                ExternalLanguageStatusRequestParams requestParams = new ExternalLanguageStatusRequestParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    LanguageName = "Python"
                };

                ExternalLanguageStatusResponseParams result = await ExternalLanguageService.Instance.HandleExternalLanguageStatusRequest(requestParams);
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
               test: async (connectionUrl) =>
               {
                   ExternalLanguageDeleteRequestParams requestParams = new ExternalLanguageDeleteRequestParams
                   {
                       OwnerUri = connectionUrl,
                       LanguageName = language.Name
                   };
                   return await service.HandleExternalLanguageDeleteRequest(requestParams);
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
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                ExternalLanguageDeleteRequestParams requestParams = new ExternalLanguageDeleteRequestParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    LanguageName = language.Name
                };
                Assert.ThrowsAsync<Exception>(async () => await service.HandleExternalLanguageDeleteRequest(requestParams));
            }
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
               test: async (connectionUrl) =>
               {
                   ExternalLanguageDeleteRequestParams requestParams = new ExternalLanguageDeleteRequestParams
                   {
                       OwnerUri = "invalid connection",
                       LanguageName = language.Name
                   };
                   return await service.HandleExternalLanguageDeleteRequest(requestParams);
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
               test: async (connectionUrl) =>
               {
                   ExternalLanguageUpdateRequestParams requestParams = new ExternalLanguageUpdateRequestParams
                   {
                       OwnerUri = connectionUrl,
                       Language = language
                   };
                   return await service.HandleExternalLanguageUpdateRequest(requestParams);
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
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                ExternalLanguageUpdateRequestParams requestParams = new ExternalLanguageUpdateRequestParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    Language = language
                };
                Assert.ThrowsAsync<Exception>(async () => await service.HandleExternalLanguageUpdateRequest(requestParams));
            }
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
               test: async (connectionUrl) =>
               {
                   ExternalLanguageUpdateRequestParams requestParams = new ExternalLanguageUpdateRequestParams
                   {
                       OwnerUri = "invalid connection",
                       Language = language
                   };
                   return await service.HandleExternalLanguageUpdateRequest(requestParams);
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
               test: async (connectionUrl) =>
               {
                   ExternalLanguageListRequestParams requestParams = new ExternalLanguageListRequestParams
                   {
                       OwnerUri = connectionUrl
                   };
                   return await service.HandleExternalLanguageListRequest(requestParams);
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
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var requestParams = new ExternalLanguageListRequestParams
                {
                    OwnerUri = queryTempFile.FilePath
                };
                Assert.ThrowsAsync<Exception>(async () => await service.HandleExternalLanguageListRequest(requestParams));
            }
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
               test: async (connectionUrl) =>
               {
                   ExternalLanguageListRequestParams requestParams = new ExternalLanguageListRequestParams
                   {
                       OwnerUri = "invalid connection"
                   };
                   return await service.HandleExternalLanguageListRequest(requestParams);
               });
        }

        public async Task VerifyRequst<T>(Func<string, Task<T>> test, Action<T> verify)
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                await RunAndVerify<T>(
               test: () => test(queryTempFile.FilePath),
               verify: verify);

                ExternalLanguageService.Instance.ConnectionServiceInstance.Disconnect(new DisconnectParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    Type = ServiceLayer.Connection.ConnectionType.Default
                });
            }
        }

        public async Task VerifyError<T>(Func<string, Task<T>> test)
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                RunAndVerifyError<T>(
               test: () => test(queryTempFile.FilePath));

                ExternalLanguageService.Instance.ConnectionServiceInstance.Disconnect(new DisconnectParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    Type = ServiceLayer.Connection.ConnectionType.Default
                });
            }
        }

        [Test]
        public void VerifyExternalLanguageStatusRequestThrowsRpcErrorGivenInvalidConnection()
        {
            ExternalLanguageStatusRequestParams requestParams = new ExternalLanguageStatusRequestParams
            {
                OwnerUri = "invalid uri",
                LanguageName = "Python"
            };

            Assert.ThrowsAsync<Exception>(async () => await ExternalLanguageService.Instance.HandleExternalLanguageStatusRequest(requestParams));
        }

        protected override RegisteredServiceProvider CreateServiceProviderWithMinServices()
        {
            return base.CreateProvider();
        }
    }
}
