//
// Code originally from http://credentialmanagement.codeplex.com/, 
// Licensed under the Apache License 2.0 
//

using System;
using Microsoft.SqlTools.Credentials.Win32;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Credentials.Win32
{
    public class CredentialSetTests
    {
        [Fact]
        public void CredentialSetCreate()
        {
            RunIfWrapper.RunIfWindows(() => 
            {
                Assert.NotNull(new CredentialSet());
            });
        }

        [Fact]
        public void CredentialSetCreateWithTarget()
        {
            RunIfWrapper.RunIfWindows(() => 
            {
                Assert.NotNull(new CredentialSet("target"));
            });
        }

        [Fact]
        public void CredentialSetShouldBeIDisposable()
        {
            RunIfWrapper.RunIfWindows(() => 
            {
                Assert.True(new CredentialSet() is IDisposable, "CredentialSet needs to implement IDisposable Interface.");
            });
        }

        [Fact]
        public void CredentialSetLoad()
        {
            RunIfWrapper.RunIfWindows(() => 
            {
                Win32Credential credential = new Win32Credential
                                            {
                                                Username = "username",
                                                Password = "password",
                                                Target = "target",
                                                Type = CredentialType.Generic
                                            };
                credential.Save();

                CredentialSet set = new CredentialSet();
                set.Load();
                Assert.NotNull(set);
                Assert.NotEmpty(set);

                credential.Delete();

                set.Dispose();
            });
        }

        [Fact]
        public void CredentialSetLoadShouldReturnSelf()
        {
            RunIfWrapper.RunIfWindows(() => 
            {
                CredentialSet set = new CredentialSet();
                Assert.IsType<CredentialSet>(set.Load());

                set.Dispose();
            });
        }

        [Fact]
        public void CredentialSetLoadWithTargetFilter()
        {
            RunIfWrapper.RunIfWindows(() => 
            {
                Win32Credential credential = new Win32Credential
                                            {
                                                Username = "filteruser",
                                                Password = "filterpassword",
                                                Target = "filtertarget"
                                            };
                credential.Save();

                CredentialSet set = new CredentialSet("filtertarget");
                Assert.Equal(1, set.Load().Count);
                set.Dispose();
            });
        }

    }
}
