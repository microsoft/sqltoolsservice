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
using Microsoft.SqlTools.ServiceLayer.Utility;
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
                var userParams = new CreateUserParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    User = SecurityTestUtils.GetTestUserInfo()
                };

                var createContext = new Mock<RequestContext<CreateUserResult>>();
                createContext.Setup(x => x.SendResult(It.IsAny<CreateUserResult>()))
                    .Returns(Task.FromResult(new object()));

                // call the create login method
                SecurityService service = new SecurityService();
                await service.HandleCreateUserRequest(userParams, createContext.Object);

                // verify the result
                createContext.Verify(x => x.SendResult(It.Is<CreateUserResult>
                    (p => p.Success && p.User.LoginName != string.Empty)));
            }
        }
    }
}
