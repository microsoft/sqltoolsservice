//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Credentials;
using Microsoft.SqlTools.ServiceLayer.Credentials.Contracts;
using Microsoft.SqlTools.ServiceLayer.Credentials.Linux;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Connection
{
    /// <summary>
    /// Credential Service tests that should pass on all platforms, regardless of backing store.
    /// These tests run E2E, storing values in the native credential store for whichever platform
    /// tests are being run on
    /// </summary>
    public class CredentialServiceTests : IDisposable
    {
        private static readonly StoreConfig config = new StoreConfig()
        {
            CredentialFolder = ".testsecrets", 
            CredentialFile = "sqltestsecrets.json", 
            IsRelativeToUserHomeDir = true
        };

        const string credentialId = "Microsoft_SqlToolsTest_TestId";
        const string password1 = "P@ssw0rd1";
        const string password2 = "2Pass2Furious";

        const string otherCredId = credentialId + "2345";
        const string otherPassword = credentialId + "2345";

        // Test-owned credential store used to clean up before/after tests to ensure code works as expected 
        // even if previous runs stopped midway through
        private ICredentialStore credStore;
        private CredentialService service;
        /// <summary>
        /// Constructor called once for every test
        /// </summary>
        public CredentialServiceTests()
        {
            credStore = CredentialService.GetStoreForOS(config);
            service = new CredentialService(credStore, config);
            DeleteDefaultCreds();
        }
        
        public void Dispose()
        {
            DeleteDefaultCreds();
        }

        private void DeleteDefaultCreds()
        {
            credStore.DeletePassword(credentialId);
            credStore.DeletePassword(otherCredId);

#if !WINDOWS_ONLY_BUILD
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string credsFolder = ((LinuxCredentialStore)credStore).CredentialFolderPath;
                if (Directory.Exists(credsFolder))
                {
                    Directory.Delete(credsFolder, true);
                }
            }
#endif
        }

        [Fact]
        public async Task SaveCredentialThrowsIfCredentialIdMissing()
        {
            object errorResponse = null;
            var contextMock = RequestContextMocks.Create<bool>(null).AddErrorHandling(obj => errorResponse = obj);

            await service.HandleSaveCredentialRequest(new Credential(null), contextMock.Object);
            TestUtils.VerifyErrorSent(contextMock);
            Assert.True(((string)errorResponse).Contains("ArgumentException"));
        }

        [Fact]
        public async Task SaveCredentialThrowsIfPasswordMissing()
        {
            object errorResponse = null;
            var contextMock = RequestContextMocks.Create<bool>(null).AddErrorHandling(obj => errorResponse = obj);
            
            await service.HandleSaveCredentialRequest(new Credential(credentialId), contextMock.Object);
            TestUtils.VerifyErrorSent(contextMock);
            Assert.True(((string)errorResponse).Contains("ArgumentException"));
        }
        
        [Fact]
        public async Task SaveCredentialWorksForSingleCredential()
        {
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password1), requestContext),
                verify: (actual => Assert.True(actual)));
        }

        [Fact]
        public async Task SaveCredentialSupportsSavingCredentialMultipleTimes()
        {
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password1), requestContext),
                verify: (actual => Assert.True(actual)));

            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password1), requestContext),
                verify: (actual => Assert.True(actual)));
        }

        [Fact]
        public async Task ReadCredentialWorksForSingleCredential()
        {
            // Given we have saved the credential
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password1), requestContext),
                verify: (actual => Assert.True(actual, "Expect Credential to be saved successfully")));


            // Expect read of the credential to return the password
            await TestUtils.RunAndVerify<Credential>(
                test: (requestContext) => service.HandleReadCredentialRequest(new Credential(credentialId, null), requestContext),
                verify: (actual =>
                {
                    Assert.Equal(password1, actual.Password);
                }));
        }

        [Fact]
        public async Task ReadCredentialWorksForMultipleCredentials()
        {

            // Given we have saved multiple credentials
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password1), requestContext),
                verify: (actual => Assert.True(actual, "Expect Credential to be saved successfully")));
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(otherCredId, otherPassword), requestContext),
                verify: (actual => Assert.True(actual, "Expect Credential to be saved successfully")));


            // Expect read of the credentials to return the right password
            await TestUtils.RunAndVerify<Credential>(
                test: (requestContext) => service.HandleReadCredentialRequest(new Credential(credentialId, null), requestContext),
                verify: (actual =>
                {
                    Assert.Equal(password1, actual.Password);
                }));
            await TestUtils.RunAndVerify<Credential>(
                test: (requestContext) => service.HandleReadCredentialRequest(new Credential(otherCredId, null), requestContext),
                verify: (actual =>
                {
                    Assert.Equal(otherPassword, actual.Password);
                }));
        }

        [Fact]
        public async Task ReadCredentialHandlesPasswordUpdate()
        {
            // Given we have saved twice with a different password
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password1), requestContext),
                verify: (actual => Assert.True(actual)));

            await TestUtils.RunAndVerify<bool>(
               test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password2), requestContext),
               verify: (actual => Assert.True(actual)));

            // When we read the value for this credential
            // Then we expect only the last saved password to be found
            await TestUtils.RunAndVerify<Credential>(
                test: (requestContext) => service.HandleReadCredentialRequest(new Credential(credentialId), requestContext),
                verify: (actual =>
                {
                    Assert.Equal(password2, actual.Password);
                }));
        }

        [Fact]
        public async Task ReadCredentialThrowsIfCredentialIsNull()
        {
            object errorResponse = null;
            var contextMock = RequestContextMocks.Create<Credential>(null).AddErrorHandling(obj => errorResponse = obj);

            // Verify throws on null, and this is sent as an error
            await service.HandleReadCredentialRequest(null, contextMock.Object);
            TestUtils.VerifyErrorSent(contextMock);
            Assert.True(((string)errorResponse).Contains("ArgumentNullException"));            
        }
        
        [Fact]
        public async Task ReadCredentialThrowsIfIdMissing()
        {
            object errorResponse = null;
            var contextMock = RequestContextMocks.Create<Credential>(null).AddErrorHandling(obj => errorResponse = obj);

            // Verify throws with no ID
            await service.HandleReadCredentialRequest(new Credential(), contextMock.Object);
            TestUtils.VerifyErrorSent(contextMock);
            Assert.True(((string)errorResponse).Contains("ArgumentException"));
        }

        [Fact]
        public async Task ReadCredentialReturnsNullPasswordForMissingCredential()
        {
            // Given a credential whose password doesn't exist
            string credWithNoPassword = "Microsoft_SqlTools_CredThatDoesNotExist";
            
            // When reading the credential
            // Then expect the credential to be returned but password left blank
            await TestUtils.RunAndVerify<Credential>(
                test: (requestContext) => service.HandleReadCredentialRequest(new Credential(credWithNoPassword, null), requestContext),
                verify: (actual =>
                {
                    Assert.NotNull(actual);
                    Assert.Equal(credWithNoPassword, actual.CredentialId);
                    Assert.Null(actual.Password);
                }));
        }
        
        [Fact]
        public async Task DeleteCredentialThrowsIfIdMissing()
        {
            object errorResponse = null;
            var contextMock = RequestContextMocks.Create<bool>(null).AddErrorHandling(obj => errorResponse = obj);

            // Verify throws with no ID
            await service.HandleDeleteCredentialRequest(new Credential(), contextMock.Object);
            TestUtils.VerifyErrorSent(contextMock);
            Assert.True(((string)errorResponse).Contains("ArgumentException"));
        }

        [Fact]
        public async Task DeleteCredentialReturnsTrueOnlyIfCredentialExisted()
        {
            // Save should be true
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password1), requestContext),
                verify: (actual => Assert.True(actual)));

            // Then delete - should return true
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleDeleteCredentialRequest(new Credential(credentialId), requestContext),
                verify: (actual => Assert.True(actual)));

            // Then delete - should return false as no longer exists
            await TestUtils.RunAndVerify<bool>(
               test: (requestContext) => service.HandleDeleteCredentialRequest(new Credential(credentialId), requestContext),
               verify: (actual => Assert.False(actual)));
        }

    }
}

