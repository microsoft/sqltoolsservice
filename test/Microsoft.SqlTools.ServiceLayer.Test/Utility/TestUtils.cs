using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

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

        /// <summary>
        /// Wait for a condition to be true for a limited amount of time.
        /// </summary>
        /// <param name="condition">Function that returns a boolean on a condition</param>
        /// <param name="intervalMilliseconds">Number of milliseconds to wait between test intervals.</param>
        /// <param name="intervalCount">Number of test intervals to perform before giving up.</param>
        /// <returns>True if the condition was met before the test interval limit.</returns>
        public static bool WaitFor(Func<bool> condition, int intervalMilliseconds = 10, int intervalCount = 200)
        {
            int count = 0;
            while (count++ < intervalCount && !condition.Invoke())
            {
                Thread.Sleep(intervalMilliseconds);
            }

            return (count < intervalCount);
        }
    }
}
