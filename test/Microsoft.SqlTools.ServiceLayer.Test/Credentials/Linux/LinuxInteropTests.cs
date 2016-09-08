//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Credentials;
using Microsoft.SqlTools.ServiceLayer.Credentials.Linux;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Credentials
{
    public class LinuxInteropTests
    {
        [Fact]
        public void GetEUidReturnsInt()
        {
            TestUtils.RunIfLinux(() =>
            {
                Assert.NotNull(Interop.Sys.GetEUid());
            });
        }

        [Fact]
        public void GetHomeDirectoryFromPwFindsHomeDir()
        {

            TestUtils.RunIfLinux(() =>
            {
                string userDir = LinuxCredentialStore.GetHomeDirectoryFromPw();
                Assert.StartsWith("/", userDir);
            });
        }
        
    }
}

