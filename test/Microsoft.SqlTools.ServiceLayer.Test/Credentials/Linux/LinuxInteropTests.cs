//
// Code originally from http://credentialmanagement.codeplex.com/, 
// Licensed under the Apache License 2.0 
//

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.SqlTools.ServiceLayer.Credentials;
using Microsoft.SqlTools.ServiceLayer.Credentials.Linux;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Credentials
{
    public class LinuxInteropTests
    {
        [Fact]
        public void GetEUidREturnsInt()
        {
            RunIfLinux(() =>
            {
                Assert.NotNull(Interop.Sys.GetEUid());
            });
        }

        [Fact]
        public void GetPwUidRFindsHomeDir()
        {

            RunIfLinux(() =>
            {
                string userDir = LinuxCredentialStore.GetHomeDirectoryFromPw();
                Assert.StartsWith("/", userDir);
            });
        }
        

        private static void RunIfLinux(Action test)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                test();
            }
        }
    }
}

