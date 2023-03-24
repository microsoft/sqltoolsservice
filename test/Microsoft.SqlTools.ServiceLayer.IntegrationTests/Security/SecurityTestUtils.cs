//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
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

        internal static string GetLoginURN(string name)
        {
            return string.Format("Server/Login[@Name='{0}']", name);
        }

        internal static string GetUserURN(string database, string name)
        {
            return string.Format("Server/Database[@Name='{0}']/User[@Name='{1}']", database, name);
        }

        internal static string GetCredentialURN(string name)
        {
            return string.Format("Server/Credential[@Name = '{0}']", name);
        }

        internal static LoginInfo GetTestLoginInfo()
        {
            return new LoginInfo()
            {
                Name = "TestLoginName_" + new Random().NextInt64(10000000, 90000000).ToString(),
                AuthenticationType = LoginAuthenticationType.Sql,
                WindowsGrantAccess = true,
                MustChangePassword = false,
                IsEnabled = false,
                IsLockedOut = false,
                EnforcePasswordPolicy = false,
                EnforcePasswordExpiration = false,
                Password = "placeholder" + new Random().NextInt64(10000000, 90000000).ToString() + "!*PLACEHOLDER",
                OldPassword = "placeholder" + new Random().NextInt64(10000000, 90000000).ToString() + "!*PLACEHOLDER",
                DefaultLanguage = "English - us_english",
                DefaultDatabase = "master"
            };
        }

        internal static UserInfo GetTestUserInfo(DatabaseUserType userType, string userName = null, string loginName = null)
        {
            return new UserInfo()
            {
                Type = userType,
                AuthenticationType = ServerAuthenticationType.Sql,
                Name = userName ?? "TestUserName_" + new Random().NextInt64(10000000, 90000000).ToString(),
                LoginName = loginName,
                Password = "placeholder" + new Random().NextInt64(10000000, 90000000).ToString() + "!*PLACEHOLDER",
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

        public static async Task<CredentialInfo> SetupCredential(TestConnectionResult connectionResult)
        {
            var service = new SecurityService();
            var credential = SecurityTestUtils.GetTestCredentialInfo();
            await SecurityTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, SecurityTestUtils.GetCredentialURN(credential.Name));
            await SecurityTestUtils.CreateCredential(service, connectionResult, credential);
            return credential;
        }

        public static async Task CleanupCredential(
            TestConnectionResult connectionResult,
            CredentialInfo credential)
        {
            var service = new SecurityService();
            await SecurityTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, SecurityTestUtils.GetCredentialURN(credential.Name));
        }

        internal static async Task<LoginInfo> CreateLogin(SecurityService service, TestConnectionResult connectionResult)
        {
            string contextId = System.Guid.NewGuid().ToString();
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

        internal static async Task<UserInfo> CreateUser(
            UserServiceHandlerImpl service,
            TestConnectionResult connectionResult,
            DatabaseUserType userType,
            string userName = null,
            string loginName = null,
            string databaseName = "master")
        {
            string contextId = System.Guid.NewGuid().ToString();
            var initializeViewRequestParams = new InitializeUserViewParams
            {
                ConnectionUri = connectionResult.ConnectionInfo.OwnerUri,
                ContextId = contextId,
                IsNewObject = true,
                Database = databaseName
            };

            var initializeUserViewContext = new Mock<RequestContext<UserViewInfo>>();
            initializeUserViewContext.Setup(x => x.SendResult(It.IsAny<UserViewInfo>()))
                .Returns(Task.FromResult(new UserViewInfo()));

            await service.HandleInitializeUserViewRequest(initializeViewRequestParams, initializeUserViewContext.Object);

            var userParams = new CreateUserParams
            {
                ContextId = contextId,
                User = SecurityTestUtils.GetTestUserInfo(userType, userName, loginName)
            };

            var createUserContext = new Mock<RequestContext<CreateUserResult>>();
            createUserContext.Setup(x => x.SendResult(It.IsAny<CreateUserResult>()))
                .Returns(Task.FromResult(new object()));

            // call the create login method
            await service.HandleCreateUserRequest(userParams, createUserContext.Object);

            // verify the result
            createUserContext.Verify(x => x.SendResult(It.Is<CreateUserResult>
                (p => p.Success && p.User.Name != string.Empty)));

            var disposeViewRequestParams = new DisposeUserViewRequestParams
            {
                ContextId = contextId
            };

            var disposeUserViewContext = new Mock<RequestContext<ResultStatus>>();
            disposeUserViewContext.Setup(x => x.SendResult(It.IsAny<ResultStatus>()))
                .Returns(Task.FromResult(new object()));

            await service.HandleDisposeUserViewRequest(disposeViewRequestParams, disposeUserViewContext.Object);

            return userParams.User;
        }

        internal static async Task<UserInfo> UpdateUser(
            UserServiceHandlerImpl service,
            TestConnectionResult connectionResult,
            UserInfo user)
        {
            string contextId = System.Guid.NewGuid().ToString();
            var initializeViewRequestParams = new InitializeUserViewParams
            {
                ConnectionUri = connectionResult.ConnectionInfo.OwnerUri,
                ContextId = contextId,
                IsNewObject = false,
                Database = "master",
                Name = user.Name
            };

            var initializeUserViewContext = new Mock<RequestContext<UserViewInfo>>();
            initializeUserViewContext.Setup(x => x.SendResult(It.IsAny<UserViewInfo>()))
                .Returns(Task.FromResult(new UserViewInfo()));

            await service.HandleInitializeUserViewRequest(initializeViewRequestParams, initializeUserViewContext.Object);

            // update the user
            user.DatabaseRoles = new string[] { "db_datareader" };
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

            var disposeViewRequestParams = new DisposeUserViewRequestParams
            {
                ContextId = contextId
            };

            var disposeUserViewContext = new Mock<RequestContext<ResultStatus>>();
            disposeUserViewContext.Setup(x => x.SendResult(It.IsAny<ResultStatus>()))
                .Returns(Task.FromResult(new object()));

            await service.HandleDisposeUserViewRequest(disposeViewRequestParams, disposeUserViewContext.Object);

            return updateParams.User;
        }

        internal static async Task DropObject(string connectionUri, string objectUrn)
        {
            ObjectManagementService objectManagementService = new ObjectManagementService();
            var dropParams = new DropRequestParams
            {
                ConnectionUri = connectionUri,
                ObjectUrn = objectUrn
            };

            var dropRequestContext = new Mock<RequestContext<bool>>();
            dropRequestContext.Setup(x => x.SendResult(It.IsAny<bool>()))
                .Returns(Task.FromResult(true));

            await objectManagementService.HandleDropRequest(dropParams, dropRequestContext.Object);
        }
    }
}
