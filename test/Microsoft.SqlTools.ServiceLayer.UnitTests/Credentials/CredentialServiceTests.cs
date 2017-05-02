//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.SqlTools.Credentials;
using Microsoft.SqlTools.Credentials.Contracts;
using Microsoft.SqlTools.Credentials.Linux;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Credentials
{
    /// <summary>
    /// Credential Service tests that should pass on all platforms, regardless of backing store.
    /// These tests run E2E, storing values in the native credential store for whichever platform
    /// tests are being run on
    /// </summary>
    public class CredentialServiceTests : IDisposable
    {
        private static readonly StoreConfig Config = new StoreConfig
        {
            CredentialFolder = ".testsecrets", 
            CredentialFile = "sqltestsecrets.json", 
            IsRelativeToUserHomeDir = true
        };

        private const string CredentialId = "Microsoft_SqlToolsTest_TestId";
        private const string Password1 = "P@ssw0rd1";
        private const string Password2 = "2Pass2Furious";

        private const string OtherCredId = CredentialId + "2345";
        private const string OtherPassword = CredentialId + "2345";

        // Test-owned credential store used to clean up before/after tests to ensure code works as expected 
        // even if previous runs stopped midway through
        private readonly ICredentialStore credStore;
        private readonly CredentialService service;
        /// <summary>
        /// Constructor called once for every test
        /// </summary>
        public CredentialServiceTests()
        {
            credStore = CredentialService.GetStoreForOS(Config);
            service = new CredentialService(credStore, Config);
            DeleteDefaultCreds();
        }
        
        public void Dispose()
        {
            DeleteDefaultCreds();
        }

        private void DeleteDefaultCreds()
        {
            credStore.DeletePassword(CredentialId);
            credStore.DeletePassword(OtherCredId);

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
            string errorResponse = null;
            var contextMock = RequestContextMocks.Create<bool>(null).AddErrorHandling((msg, code, obj) => errorResponse = msg);

            await service.HandleSaveCredentialRequest(new Credential(null), contextMock.Object);
            TestUtils.VerifyErrorSent(contextMock);
            Assert.Contains("ArgumentException", errorResponse);
        }

        [Fact]
        public async Task SaveCredentialThrowsIfPasswordMissing()
        {
            string errorResponse = null;
            var contextMock = RequestContextMocks.Create<bool>(null).AddErrorHandling((msg, code, obj) => errorResponse = msg);
            
            await service.HandleSaveCredentialRequest(new Credential(CredentialId), contextMock.Object);
            TestUtils.VerifyErrorSent(contextMock);
            Assert.True(errorResponse.Contains("ArgumentException") || errorResponse.Contains("ArgumentNullException"));
        }

        [Fact]
        public async Task SaveCredentialWorksForSingleCredential()
        {
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(CredentialId, Password1), requestContext),
                verify: Assert.True);
        }

        [Fact]
        public async Task SaveCredentialWorksForEmptyPassword()
        {
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(CredentialId, ""), requestContext),
                verify: Assert.True);
        }

        [Fact]
        public async Task SaveCredentialSupportsSavingCredentialMultipleTimes()
        {
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(CredentialId, Password1), requestContext),
                verify: Assert.True);

            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(CredentialId, Password1), requestContext),
                verify: Assert.True);
        }

        [Fact]
        public async Task ReadCredentialWorksForSingleCredential()
        {
            // Given we have saved the credential
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(CredentialId, Password1), requestContext),
                verify: (actual => Assert.True(actual, "Expect Credential to be saved successfully")));


            // Expect read of the credential to return the password
            await TestUtils.RunAndVerify<Credential>(
                test: (requestContext) => service.HandleReadCredentialRequest(new Credential(CredentialId, null), requestContext),
                verify: (actual =>
                {
                    Assert.Equal(Password1, actual.Password);
                }));
        }

        [Fact]
        public async Task ReadCredentialWorksForMultipleCredentials()
        {

            // Given we have saved multiple credentials
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(CredentialId, Password1), requestContext),
                verify: (actual => Assert.True(actual, "Expect Credential to be saved successfully")));
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(OtherCredId, OtherPassword), requestContext),
                verify: actual => Assert.True(actual, "Expect Credential to be saved successfully"));


            // Expect read of the credentials to return the right password
            await TestUtils.RunAndVerify<Credential>(
                test: (requestContext) => service.HandleReadCredentialRequest(new Credential(CredentialId, null), requestContext),
                verify: (actual =>
                {
                    Assert.Equal(Password1, actual.Password);
                }));
            await TestUtils.RunAndVerify<Credential>(
                test: (requestContext) => service.HandleReadCredentialRequest(new Credential(OtherCredId, null), requestContext),
                verify: (actual =>
                {
                    Assert.Equal(OtherPassword, actual.Password);
                }));
        }

        [Fact]
        public async Task ReadCredentialHandlesPasswordUpdate()
        {
            // Given we have saved twice with a different password
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(CredentialId, Password1), requestContext),
                verify: Assert.True);

            await TestUtils.RunAndVerify<bool>(
               test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(CredentialId, Password2), requestContext),
               verify: Assert.True);

            // When we read the value for this credential
            // Then we expect only the last saved password to be found
            await TestUtils.RunAndVerify<Credential>(
                test: (requestContext) => service.HandleReadCredentialRequest(new Credential(CredentialId), requestContext),
                verify: (actual =>
                {
                    Assert.Equal(Password2, actual.Password);
                }));
        }

        [Fact]
        public async Task ReadCredentialThrowsIfCredentialIsNull()
        {
            string errorResponse = null;
            var contextMock = RequestContextMocks.Create<Credential>(null).AddErrorHandling((msg, code, obj) => errorResponse = msg);

            // Verify throws on null, and this is sent as an error
            await service.HandleReadCredentialRequest(null, contextMock.Object);
            TestUtils.VerifyErrorSent(contextMock);
            Assert.Contains("ArgumentNullException", errorResponse);
        }
        
        [Fact]
        public async Task ReadCredentialThrowsIfIdMissing()
        {
            string errorResponse = null;
            var contextMock = RequestContextMocks.Create<Credential>(null).AddErrorHandling((msg, code, obj) => errorResponse = msg);

            // Verify throws with no ID
            await service.HandleReadCredentialRequest(new Credential(), contextMock.Object);
            TestUtils.VerifyErrorSent(contextMock);
            Assert.Contains("ArgumentException", errorResponse);
        }

        [Fact]
        public async Task ReadCredentialReturnsNullPasswordForMissingCredential()
        {
            // Given a credential whose password doesn't exist
            const string credWithNoPassword = "Microsoft_SqlTools_CredThatDoesNotExist";

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
            var contextMock = RequestContextMocks.Create<bool>(null).AddErrorHandling((msg, code, obj) => errorResponse = msg);

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
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(CredentialId, Password1), requestContext),
                verify: Assert.True);

            // Then delete - should return true
            await TestUtils.RunAndVerify<bool>(
                test: (requestContext) => service.HandleDeleteCredentialRequest(new Credential(CredentialId), requestContext),
                verify: Assert.True);

            // Then delete - should return false as no longer exists
            await TestUtils.RunAndVerify<bool>(
               test: (requestContext) => service.HandleDeleteCredentialRequest(new Credential(CredentialId), requestContext),
               verify: Assert.False);
        }

    }
}

