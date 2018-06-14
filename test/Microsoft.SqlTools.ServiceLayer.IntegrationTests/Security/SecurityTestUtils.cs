//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
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
