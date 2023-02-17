//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Security;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
// using Microsoft.SqlTools.ServiceLayer.Utility;
using Moq;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Security
{
    /// <summary>
    /// Tests for the Login management component
    /// </summary>
    public class LoginTests
    {
        /// <summary>
        /// Test the basic Create Login method handler
        /// </summary>
        // [Test]
        public async Task TestHandleCreateLoginRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var contextId = System.Guid.NewGuid().ToString();

                var initializeLoginViewRequestParams = new InitializeLoginViewRequestParams
                {
                    ConnectionUri = connectionResult.ConnectionInfo.OwnerUri,
                    ContextId = contextId,
                    IsNewObject = true
                };

                var loginParams = new CreateLoginParams
                {
                    ContextId = contextId,
                    Login = SecurityTestUtils.GetTestLoginInfo()
                };

                var createLoginContext = new Mock<RequestContext<object>>();
                createLoginContext.Setup(x => x.SendResult(It.IsAny<object>()))
                    .Returns(Task.FromResult(new object()));
                var initializeLoginViewContext = new Mock<RequestContext<LoginViewInfo>>();
                initializeLoginViewContext.Setup(x => x.SendResult(It.IsAny<LoginViewInfo>()))
                    .Returns(Task.FromResult(new LoginViewInfo()));

                // call the create login method
                SecurityService service = new SecurityService();
                await service.HandleInitializeLoginViewRequest(initializeLoginViewRequestParams, initializeLoginViewContext.Object);
                await service.HandleCreateLoginRequest(loginParams, createLoginContext.Object);

                // cleanup created login
                var deleteParams = new DeleteLoginParams
                {
                    ConnectionUri = connectionResult.ConnectionInfo.OwnerUri,
                    Name = loginParams.Login.Name
                };

                var deleteContext = new Mock<RequestContext<object>>();
                deleteContext.Setup(x => x.SendResult(It.IsAny<object>()))
                    .Returns(Task.FromResult(new object()));

                // call the create login method
                await service.HandleDeleteLoginRequest(deleteParams, deleteContext.Object);
            }
        }
    }
}
