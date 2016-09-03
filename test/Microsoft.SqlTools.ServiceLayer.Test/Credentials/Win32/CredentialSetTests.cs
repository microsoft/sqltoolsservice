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
    public class CredentialSetTests
    {
        [Fact]
        public void CredentialSetCreate()
        {
            RunIfWindows(() => 
            {
                Assert.NotNull(new CredentialSet());
            });
        }

        [Fact]
        public void CredentialSetCreateWithTarget()
        {
            RunIfWindows(() => 
            {
                Assert.NotNull(new CredentialSet("target"));
            });
        }

        [Fact]
        public void CredentialSetShouldBeIDisposable()
        {
            RunIfWindows(() => 
            {
                Assert.True(new CredentialSet() is IDisposable, "CredentialSet needs to implement IDisposable Interface.");
            });
        }

        [Fact]
        public void CredentialSetLoad()
        {
            RunIfWindows(() => 
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
            RunIfWindows(() => 
            {
                CredentialSet set = new CredentialSet();
                Assert.IsType<CredentialSet>(set.Load());

                set.Dispose();
            });
        }

        [Fact]
        public void CredentialSetLoadWithTargetFilter()
        {
            RunIfWindows(() => 
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

        private static void RunIfWindows(Action test)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                test();
            }
        }
    }
}
