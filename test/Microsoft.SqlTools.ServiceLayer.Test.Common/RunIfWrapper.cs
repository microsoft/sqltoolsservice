//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public class RunIfWrapper
    {
        public static void RunIfLinux(Action test)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                test();
            }
        }

        public static void RunIfLinuxOrOSX(Action test)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                test();
            }
        }

        public static void RunIfWindows(Action test)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                test();
            }
        }
    }
}
