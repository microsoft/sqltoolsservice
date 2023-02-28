//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Security;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Moq;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Security
{
    public static class SecurityTestUtils
    {
        public static string TestCredentialName = "Current User";

        internal static string GetCurrentUserIdentity()
        {               
            return string.Format(@"{0}\{1}", Environment.UserDomainName, Environment.UserName);
        }

        internal static LoginInfo GetTestLoginInfo()
        {
            return new LoginInfo()
            {
                Name = "TestLoginName_" + new Random().NextInt64(10000000,90000000).ToString(),
                AuthenticationType= LoginAuthenticationType.Sql,
                WindowsGrantAccess = true,
                MustChangePassword = false,
                IsEnabled = false,
                IsLockedOut = false,
                EnforcePasswordPolicy = false,
                EnforcePasswordExpiration = false,
                Password = "placeholder",                
                OldPassword = "placeholder",
                DefaultLanguage = "us_english",
                DefaultDatabase = "master"
            };
        }

        internal static UserInfo GetTestUserInfo(string loginName)
        {
            return new UserInfo()
            {
                Type = DatabaseUserType.WithLogin,
                Name = "TestUserName_" + new Random().NextInt64(10000000,90000000).ToString(),
                LoginName = loginName,
                Password = "placeholder",
                DefaultSchema = "dbo",
                OwnedSchemas = new string[] { "" }
            };
        }

        internal static CredentialInfo GetTestCredentialInfo()
        {
            return new CredentialInfo()
            {
                Identity = GetCurrentUserIdentity(),
                Name = TestCredentialName
            };
        }

        internal static async Task CreateCredential(
            SecurityService service, 
            TestConnectionResult connectionResult,
            CredentialInfo credential)
        {
            var context = new Mock<RequestContext<CredentialResult>>();
            await service.HandleCreateCredentialRequest(new CreateCredentialParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Credential = credential
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task UpdateCredential(
            SecurityService service, 
            TestConnectionResult connectionResult,
            CredentialInfo credential)
        {
            var context = new Mock<RequestContext<CredentialResult>>();
            await service.HandleUpdateCredentialRequest(new UpdateCredentialParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Credential = credential
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task DeleteCredential(
            SecurityService service, 
            TestConnectionResult connectionResult, 
            CredentialInfo credential)
        {
            var context = new Mock<RequestContext<ResultStatus>>();
            await service.HandleDeleteCredentialRequest(new DeleteCredentialParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Credential = credential
            }, context.Object);
            context.VerifyAll();
        }

        public static async Task<CredentialInfo> SetupCredential(TestConnectionResult connectionResult)
        {
            var service = new SecurityService();
            var credential = SecurityTestUtils.GetTestCredentialInfo();
            await SecurityTestUtils.DeleteCredential(service, connectionResult, credential);
            await SecurityTestUtils.CreateCredential(service, connectionResult, credential);
            return credential;
        }

        public static async Task CleanupCredential(
            TestConnectionResult connectionResult,
            CredentialInfo credential)
        {
            var service = new SecurityService();
            await SecurityTestUtils.DeleteCredential(service, connectionResult, credential);
        }

        public static async Task<LoginInfo> CreateLogin(SecurityService service, TestConnectionResult connectionResult, string contextId)
        {
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
            await service.HandleInitializeLoginViewRequest(initializeLoginViewRequestParams, initializeLoginViewContext.Object);
            await service.HandleCreateLoginRequest(loginParams, createLoginContext.Object);

            return loginParams.Login;
        }

        public static async Task DeleteLogin(SecurityService service, TestConnectionResult connectionResult, LoginInfo login)
        {
            // cleanup created login
            var deleteParams = new DeleteLoginParams
            {
                ConnectionUri = connectionResult.ConnectionInfo.OwnerUri,
                Name = login.Name
            };

            var deleteContext = new Mock<RequestContext<object>>();
            deleteContext.Setup(x => x.SendResult(It.IsAny<object>()))
                .Returns(Task.FromResult(new object()));

            // call the create login method
            await service.HandleDeleteLoginRequest(deleteParams, deleteContext.Object);
        }

        public static async Task<UserInfo> CreateUser(
            SecurityService service, 
            TestConnectionResult connectionResult, 
            string contextId,
            LoginInfo login)
        {
            var userParams = new CreateUserParams
            {
                ContextId = contextId,
                User = SecurityTestUtils.GetTestUserInfo(login.Name)
            };

            var createUserContext = new Mock<RequestContext<CreateUserResult>>();
            createUserContext.Setup(x => x.SendResult(It.IsAny<CreateUserResult>()))
                .Returns(Task.FromResult(new object()));

            // call the create login method
            await service.HandleCreateUserRequest(userParams, createUserContext.Object);

            // verify the result
            createUserContext.Verify(x => x.SendResult(It.Is<CreateUserResult>
                (p => p.Success && p.User.Name != string.Empty)));

            return userParams.User;             
        }

        public static async Task UpdateUser(
            SecurityService service, 
            TestConnectionResult connectionResult, 
            string contextId,
            UserInfo user)
        {
            // update the user
            user.OwnedSchemas = new string[] { "dbo" };
            var updateParams = new UpdateUserParams
            {
                ContextId = contextId,
                User = user
            };
            var updateUserContext = new Mock<RequestContext<ResultStatus>>();
            // call the create login method
            await service.HandleUpdateUserRequest(updateParams, updateUserContext.Object);
            // verify the result
            updateUserContext.Verify(x => x.SendResult(It.Is<ResultStatus>(p => p.Success)));         
        }

        public static async Task DeleteUser(SecurityService service, TestConnectionResult connectionResult, UserInfo user)
        {
            // cleanup created user
            var deleteParams = new DeleteUserParams
            {
                ConnectionUri = connectionResult.ConnectionInfo.OwnerUri,
                Name = user.Name,
                Database = connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName
            };

            var deleteContext = new Mock<RequestContext<ResultStatus>>();
            deleteContext.Setup(x => x.SendResult(It.IsAny<ResultStatus>()))
                .Returns(Task.FromResult(new object()));

            // call the create user method
            await service.HandleDeleteUserRequest(deleteParams, deleteContext.Object);

            deleteContext.Verify(x => x.SendResult(It.Is<ResultStatus>(p => p.Success)));
        }       
    }
}
