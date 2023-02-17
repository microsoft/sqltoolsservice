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
                LoginName = "TestLoginName_" + new Random().NextInt64(10000000,90000000).ToString(),
                LoginType= LoginType.Sql,
                CertificateName = "Test Cert",        
                AsymmetricKeyName = "Asymmetric Test Cert",
                WindowsGrantAccess = true,
                MustChange = false,
                IsDisabled = false,
                IsLockedOut = false,
                EnforcePolicy = false,
                EnforceExpiration = false,
                WindowsAuthSupported = false,
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
                OwnedSchemas = new string[] { "dbo" }
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
    }
}
