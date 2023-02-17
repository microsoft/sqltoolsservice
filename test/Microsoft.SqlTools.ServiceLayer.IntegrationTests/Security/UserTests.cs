//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Security;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Security
{
    /// <summary>
    /// Tests for the User management component
    /// </summary>
    public class UserTests
    {
        /// <summary>
        /// Test the basic Create User method handler
        /// </summary>
        // [Test]
        public async Task TestHandleCreateUserRequest()
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

                var userParams = new CreateUserParams
                {
                    ContextId = connectionResult.ConnectionInfo.OwnerUri,
                    User = SecurityTestUtils.GetTestUserInfo(loginParams.Login.Name)
                };

                var createUserContext = new Mock<RequestContext<CreateUserResult>>();
                createUserContext.Setup(x => x.SendResult(It.IsAny<CreateUserResult>()))
                    .Returns(Task.FromResult(new object()));

                // call the create login method
                await service.HandleCreateUserRequest(userParams, createUserContext.Object);

                // verify the result
                createUserContext.Verify(x => x.SendResult(It.Is<CreateUserResult>
                    (p => p.Success && p.User.Name != string.Empty)));
            }
        }
    }
}
