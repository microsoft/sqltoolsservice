using System;
using System.Runtime.InteropServices;

namespace Microsoft.SqlTools.ServiceLayer.Test.Utility
{
    public static class TestUtils
    {

        public static void RunIfLinux(Action test)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
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
