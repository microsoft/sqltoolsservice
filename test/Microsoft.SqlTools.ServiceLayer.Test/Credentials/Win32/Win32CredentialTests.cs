//
// Code originally from http://credentialmanagement.codeplex.com/, 
// Licensed under the Apache License 2.0 
//

using System;
using Microsoft.SqlTools.Credentials.Win32;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Credentials
{
    public class Win32CredentialTests
    {
        [Fact]
        public void Credential_Create_ShouldNotThrowNull()
        {
            TestUtils.RunIfWindows(() =>
            {
                Assert.NotNull(new Win32Credential());
            });
        }

        [Fact]
        public void Credential_Create_With_Username_ShouldNotThrowNull()
        {
            TestUtils.RunIfWindows(() =>
            {
                Assert.NotNull(new Win32Credential("username"));
            });
        }

        [Fact]
        public void Credential_Create_With_Username_And_Password_ShouldNotThrowNull()
        {
            TestUtils.RunIfWindows(() =>
            {
                Assert.NotNull(new Win32Credential("username", "password"));
            });
        }

        [Fact]
        public void Credential_Create_With_Username_Password_Target_ShouldNotThrowNull()
        {
            TestUtils.RunIfWindows(() =>
            {
                Assert.NotNull(new Win32Credential("username", "password", "target"));
            });
        }

        [Fact]
        public void Credential_ShouldBe_IDisposable()
        {
            TestUtils.RunIfWindows(() =>
            {
                Assert.True(new Win32Credential() is IDisposable, "Credential should implement IDisposable Interface.");
            });
        }
        
        [Fact]
        public void Credential_Dispose_ShouldNotThrowException()
        {
            TestUtils.RunIfWindows(() =>
            {
                new Win32Credential().Dispose();
            });
        }

        [Fact]
        public void Credential_ShouldThrowObjectDisposedException()
        {
            TestUtils.RunIfWindows(() =>
            {
                Win32Credential disposed = new Win32Credential { Password = "password" };
                disposed.Dispose();
                Assert.Throws<ObjectDisposedException>(() => disposed.Username = "username");
            });
        }

        [Fact]
        public void Credential_Save()
        {
            TestUtils.RunIfWindows(() =>
            {
                Win32Credential saved = new Win32Credential("username", "password", "target", CredentialType.Generic);
                saved.PersistanceType = PersistanceType.LocalComputer;
                Assert.True(saved.Save());
            });
        }
        
        [Fact]
        public void Credential_Delete()
        {
            TestUtils.RunIfWindows(() =>
            {
                new Win32Credential("username", "password", "target").Save();
                Assert.True(new Win32Credential("username", "password", "target").Delete());
            });
        }

        [Fact]
        public void Credential_Delete_NullTerminator()
        {
            TestUtils.RunIfWindows(() =>
            {
                Win32Credential credential = new Win32Credential((string)null, (string)null, "\0", CredentialType.None);
                credential.Description = (string)null;
                Assert.False(credential.Delete());
            });
        }
       
        [Fact]
        public void Credential_Load()
        {
            TestUtils.RunIfWindows(() =>
            {
                Win32Credential setup = new Win32Credential("username", "password", "target", CredentialType.Generic);
                setup.Save();

                Win32Credential credential = new Win32Credential { Target = "target", Type = CredentialType.Generic };
                Assert.True(credential.Load());

                Assert.NotEmpty(credential.Username);
                Assert.NotNull(credential.Password);
                Assert.Equal("username", credential.Username);
                Assert.Equal("password", credential.Password);
                Assert.Equal("target", credential.Target);
            });
        }

        [Fact]
        public void Credential_Exists_Target_ShouldNotBeNull()
        {
            TestUtils.RunIfWindows(() =>
            {
                new Win32Credential { Username = "username", Password = "password", Target = "target" }.Save();

                Win32Credential existingCred = new Win32Credential { Target = "target" };
                Assert.True(existingCred.Exists());

                existingCred.Delete();
            });
        }
    }
}
