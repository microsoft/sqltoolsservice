//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Credentials;
using Microsoft.SqlTools.Credentials.Linux;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Credentials.Linux
{
    [TestFixture]
    public class LinuxInteropTests
    {
        [Test]
        public void GetEUidReturnsInt()
        {
#if !WINDOWS_ONLY_BUILD           
            RunIfWrapper.RunIfLinux(() =>
            {
                Assert.NotNull(Interop.Sys.GetEUid());
            });
#endif           
        }

        [Test]
        public void GetHomeDirectoryFromPwFindsHomeDir()
        {
#if !WINDOWS_ONLY_BUILD
            RunIfWrapper.RunIfLinux(() =>
            {
                string userDir = LinuxCredentialStore.GetHomeDirectoryFromPw();
                Assert.That(userDir, Does.StartWith("/"), "GetHomeDirectoryFromPw");
            });
#endif
        }
    }
}

