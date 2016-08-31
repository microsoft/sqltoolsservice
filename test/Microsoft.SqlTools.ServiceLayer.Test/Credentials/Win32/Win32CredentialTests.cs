//
// Code originally from http://credentialmanagement.codeplex.com/, 
// Licensed under the Apache License 2.0 
//

using System;
using System.Runtime.InteropServices;
using Microsoft.SqlTools.ServiceLayer.Credentials.Win32;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Credentials
{
    public class Win32CredentialTests
    {
        [Fact]
        public void Credential_Create_ShouldNotThrowNull()
        {
            RunIfWindows(() =>
            {
                Assert.NotNull(new Win32Credential());
            });
        }

        [Fact]
        public void Credential_Create_With_Username_ShouldNotThrowNull()
        {
            RunIfWindows(() =>
            {
                Assert.NotNull(new Win32Credential("username"));
            });
        }

        [Fact]
        public void Credential_Create_With_Username_And_Password_ShouldNotThrowNull()
        {
            RunIfWindows(() =>
            {
                Assert.NotNull(new Win32Credential("username", "password"));
            });
        }

        [Fact]
        public void Credential_Create_With_Username_Password_Target_ShouldNotThrowNull()
        {
            RunIfWindows(() =>
            {
                Assert.NotNull(new Win32Credential("username", "password", "target"));
            });
        }

        [Fact]
        public void Credential_ShouldBe_IDisposable()
        {
            RunIfWindows(() =>
            {
                Assert.True(new Win32Credential() is IDisposable, "Credential should implement IDisposable Interface.");
            });
        }
        
        [Fact]
        public void Credential_Dispose_ShouldNotThrowException()
        {
            RunIfWindows(() =>
            {
                new Win32Credential().Dispose();
            });
        }

        [Fact]
        public void Credential_ShouldThrowObjectDisposedException()
        {
            RunIfWindows(() =>
            {
                Win32Credential disposed = new Win32Credential { Password = "password" };
                disposed.Dispose();
                Assert.Throws<ObjectDisposedException>(() => disposed.Username = "username");
            });
        }

        [Fact]
        public void Credential_Save()
        {
            RunIfWindows(() =>
            {
                Win32Credential saved = new Win32Credential("username", "password", "target", CredentialType.Generic);
                saved.PersistanceType = PersistanceType.LocalComputer;
                Assert.True(saved.Save());
            });
        }
        
        [Fact]
        public void Credential_Delete()
        {
            RunIfWindows(() =>
            {
                new Win32Credential("username", "password", "target").Save();
                Assert.True(new Win32Credential("username", "password", "target").Delete());
            });
        }

        [Fact]
        public void Credential_Delete_NullTerminator()
        {
            RunIfWindows(() =>
            {
                Win32Credential credential = new Win32Credential((string)null, (string)null, "\0", CredentialType.None);
                credential.Description = (string)null;
                Assert.False(credential.Delete());
            });
        }
       
        [Fact]
        public void Credential_Load()
        {
            RunIfWindows(() =>
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
            RunIfWindows(() =>
            {
                new Win32Credential { Username = "username", Password = "password", Target = "target" }.Save();

                Win32Credential existingCred = new Win32Credential { Target = "target" };
                Assert.True(existingCred.Exists());

                existingCred.Delete();
            });
        }

        private static void RunIfWindows(Action test)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                test();
            }
        }
    }
}
