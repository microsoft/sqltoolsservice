//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Credentials;
using Microsoft.SqlTools.ServiceLayer.Credentials.Contracts;
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
        const string credentialId = "Microsoft_SqlToolsTest_TestId";
        const string password1 = "P@ssw0rd1";
        const string password2 = "2Pass2Furious";

        // Test-owned credential store used to clean up before/after tests to ensure code works as expected 
        // even if previous runs stopped midway through
        private ICredentialStore credStore;
        private CredentialService service;
        /// <summary>
        /// Constructor called once for every test
        /// </summary>
        public CredentialServiceTests()
        {
            credStore = CredentialService.GetStoreForOS();
            service = new CredentialService(credStore);
            DeleteDefaultCreds();
        }
        
        public void Dispose()
        {
            DeleteDefaultCreds();
        }

        private void DeleteDefaultCreds()
        {
            credStore.DeletePassword(credentialId);
        }

        [Fact]
        public async Task SaveCredential_Should_Throw_If_CredentialId_Missing()
        {
            object errorResponse = null;
            var contextMock = RequestContextMocks.Create<bool>(null).AddErrorHandling(obj => errorResponse = obj);

            await service.HandleSaveCredentialRequest(new Credential(null), contextMock.Object);
            VerifyErrorSent(contextMock);
            Assert.True(((string)errorResponse).Contains("ArgumentException"));
        }

        [Fact]
        public async Task SaveCredential_Should_Throw_If_Password_Missing()
        {
            object errorResponse = null;
            var contextMock = RequestContextMocks.Create<bool>(null).AddErrorHandling(obj => errorResponse = obj);
            
            await service.HandleSaveCredentialRequest(new Credential(credentialId), contextMock.Object);
            VerifyErrorSent(contextMock);
            Assert.True(((string)errorResponse).Contains("ArgumentException"));
        }
        
        [Fact]
        public async Task SaveCredential_Returns_True_When_Credential_And_Password_Defined()
        {
            await RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password1), requestContext),
                verify: (actual => Assert.True(actual)));
        }

        [Fact]
        public async Task SaveCredential_Returns_True_When_Password_Saved_Twice()
        {
            await RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password1), requestContext),
                verify: (actual => Assert.True(actual)));

            await RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password1), requestContext),
                verify: (actual => Assert.True(actual)));
        }

        [Fact]
        public async Task ReadCredential_Returns_Password_For_Credential()
        {
            // Given we have saved the credential
            await RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password1), requestContext),
                verify: (actual => Assert.True(actual, "Expect Credential to be saved successfully")));


            // Expect read of the credential to return the password
            await RunAndVerify<Credential>(
                test: (requestContext) => service.HandleReadCredentialRequest(new Credential(credentialId, null), requestContext),
                verify: (actual =>
                {
                    Assert.Equal(password1, actual.Password);
                }));
        }

        [Fact]
        public async Task ReadCredential_Finds_Last_Saved_Password()
        {
            // Given we have saved twice with a different password
            await RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password1), requestContext),
                verify: (actual => Assert.True(actual)));

            await RunAndVerify<bool>(
               test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password2), requestContext),
               verify: (actual => Assert.True(actual)));

            // When we read the value for this credential
            // Then we expect only the last saved password to be found
            await RunAndVerify<Credential>(
                test: (requestContext) => service.HandleReadCredentialRequest(new Credential(credentialId), requestContext),
                verify: (actual =>
                {
                    Assert.Equal(password2, actual.Password);
                }));
        }

        [Fact]
        public async Task ReadCredential_Should_Throw_If_Credential_Is_Null()
        {
            object errorResponse = null;
            var contextMock = RequestContextMocks.Create<Credential>(null).AddErrorHandling(obj => errorResponse = obj);

            // Verify throws on null, and this is sent as an error
            await service.HandleReadCredentialRequest(null, contextMock.Object);
            VerifyErrorSent(contextMock);
            Assert.True(((string)errorResponse).Contains("ArgumentNullException"));            
        }
        
        [Fact]
        public async Task ReadCredential_Should_Throw_If_Id_Missing()
        {
            object errorResponse = null;
            var contextMock = RequestContextMocks.Create<Credential>(null).AddErrorHandling(obj => errorResponse = obj);

            // Verify throws with no ID
            await service.HandleReadCredentialRequest(new Credential(), contextMock.Object);
            VerifyErrorSent(contextMock);
            Assert.True(((string)errorResponse).Contains("ArgumentException"));
        }

        [Fact]
        public async Task ReadCredential_Should_Allow_No_Password_Found()
        {
            // Given a credential whose password doesn't exist
            string credWithNoPassword = "Microsoft_SqlTools_CredThatDoesNotExist";
            
            // When reading the credential
            // Then expect the credential to be returned but password left blank
            await RunAndVerify<Credential>(
                test: (requestContext) => service.HandleReadCredentialRequest(new Credential(credWithNoPassword, null), requestContext),
                verify: (actual =>
                {
                    Assert.NotNull(actual);
                    Assert.Equal(credWithNoPassword, actual.CredentialId);
                    Assert.Null(actual.Password);
                }));
        }
        
        [Fact]
        public async Task DeleteCredential_Should_Throw_If_Id_Missing()
        {
            object errorResponse = null;
            var contextMock = RequestContextMocks.Create<bool>(null).AddErrorHandling(obj => errorResponse = obj);

            // Verify throws with no ID
            await service.HandleDeleteCredentialRequest(new Credential(), contextMock.Object);
            VerifyErrorSent(contextMock);
            Assert.True(((string)errorResponse).Contains("ArgumentException"));
        }

        [Fact]
        public async Task DeleteCredential_Returns_True_When_Password_Deleted()
        {
            // Save should be true
            await RunAndVerify<bool>(
                test: (requestContext) => service.HandleSaveCredentialRequest(new Credential(credentialId, password1), requestContext),
                verify: (actual => Assert.True(actual)));

            // Then delete - should return true
            await RunAndVerify<bool>(
                test: (requestContext) => service.HandleDeleteCredentialRequest(new Credential(credentialId), requestContext),
                verify: (actual => Assert.True(actual)));

            // Then delete - should return false as no longer exists
            await RunAndVerify<bool>(
               test: (requestContext) => service.HandleDeleteCredentialRequest(new Credential(credentialId), requestContext),
               verify: (actual => Assert.False(actual)));
        }

        private async Task RunAndVerify<T>(Func<RequestContext<T>, Task> test, Action<T> verify)
        {
            T result = default(T);
            var contextMock = RequestContextMocks.Create<T>(r => result = r).AddErrorHandling(null);
            await test(contextMock.Object);
            VerifyResult(contextMock, verify, result);
        }

        private void VerifyErrorSent<T>(Mock<RequestContext<T>> contextMock)
        {
            contextMock.Verify(c => c.SendResult(It.IsAny<T>()), Times.Never);
            contextMock.Verify(c => c.SendError(It.IsAny<string>()), Times.Once);
        }

        private void VerifyResult<T, U>(Mock<RequestContext<T>> contextMock, U expected, U actual)
        {
            contextMock.Verify(c => c.SendResult(It.IsAny<T>()), Times.Once);
            Assert.Equal(expected, actual);
            contextMock.Verify(c => c.SendError(It.IsAny<string>()), Times.Never);
        }

        private void VerifyResult<T>(Mock<RequestContext<T>> contextMock, Action<T> verify, T actual)
        {
            contextMock.Verify(c => c.SendResult(It.IsAny<T>()), Times.Once);
            contextMock.Verify(c => c.SendError(It.IsAny<string>()), Times.Never);
            verify(actual);
        }

    }
}
