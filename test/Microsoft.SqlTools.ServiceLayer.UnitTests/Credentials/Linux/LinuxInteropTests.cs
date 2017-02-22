//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Credentials;
using Microsoft.SqlTools.ServiceLayer.Credentials.Linux;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Credentials.Linux
{
    public class LinuxInteropTests
    {
        [Fact]
        public void GetEUidReturnsInt()
        {
#if !WINDOWS_ONLY_BUILD           
            TestUtils.RunIfLinux(() =>
            {
                Assert.NotNull(Interop.Sys.GetEUid());
            });
#endif           
        }

        [Fact]
        public void GetHomeDirectoryFromPwFindsHomeDir()
        {
#if !WINDOWS_ONLY_BUILD
            TestUtils.RunIfLinux(() =>
            {
                string userDir = LinuxCredentialStore.GetHomeDirectoryFromPw();
                Assert.StartsWith("/", userDir);
            });
#endif
        }
    }
}

