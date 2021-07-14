//
// Code originally from http://credentialmanagement.codeplex.com/,
// Licensed under the Apache License 2.0
//

using System;
using Microsoft.SqlTools.Credentials.Win32;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Credentials.Win32
{
    [TestFixture]
    public class CredentialSetTests
    {
        [Test]
        public void CredentialSetCreate()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                Assert.NotNull(new CredentialSet());
            });
        }

        [Test]
        public void CredentialSetCreateWithTarget()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                Assert.NotNull(new CredentialSet("target"));
            });
        }

        [Test]
        public void CredentialSetShouldBeIDisposable()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                Assert.True(new CredentialSet() is IDisposable, "CredentialSet needs to implement IDisposable Interface.");
            });
        }

        [Test]
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
                Assert.That(set, Is.Not.Empty);

                credential.Delete();

                set.Dispose();
            });
        }

        [Test]
        public void CredentialSetLoadShouldReturnSelf()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                CredentialSet set = new CredentialSet();
                Assert.That(set.Load(), Is.SameAs(set));

                set.Dispose();
            });
        }

        [Test]
        public void CredentialSetLoadWithTargetFilter()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                Win32Credential credential = new Win32Credential
                                            {
                                                Username = "filteruser",
                                                Password = Guid.NewGuid().ToString(),
                                                Target = "filtertarget"
                                            };
                credential.Save();

                CredentialSet set = new CredentialSet("filtertarget");
                Assert.AreEqual(1, set.Load().Count);
                set.Dispose();
            });
        }

    }
}
