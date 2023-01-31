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
using NUnit.Framework;

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
                var loginParams = new CreateLoginParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Login = SecurityTestUtils.GetTestLoginInfo()
                };

                var createContext = new Mock<RequestContext<CreateLoginResult>>();
                createContext.Setup(x => x.SendResult(It.IsAny<CreateLoginResult>()))
                    .Returns(Task.FromResult(new object()));

                // call the create login method
                SecurityService service = new SecurityService();
                await service.HandleCreateLoginRequest(loginParams, createContext.Object);

                // verify the result
                createContext.Verify(x => x.SendResult(It.Is<CreateLoginResult>
                    (p => p.Success && p.Login.LoginName != string.Empty)));

                // cleanup created login
                var deleteParams = new DeleteLoginParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    LoginName = loginParams.Login.LoginName
                };

                var deleteContext = new Mock<RequestContext<ResultStatus>>();
                deleteContext.Setup(x => x.SendResult(It.IsAny<ResultStatus>()))
                    .Returns(Task.FromResult(new object()));

                // call the create login method
                await service.HandleDeleteLoginRequest(deleteParams, deleteContext.Object);

                // verify the result
                deleteContext.Verify(x => x.SendResult(It.Is<ResultStatus>(p => p.Success)));
            }
        }
    }
}
