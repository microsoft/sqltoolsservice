//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.InteropServices;

namespace Microsoft.SqlTools.Shared.Utility
{
    public static class Utils
    {
        /// <summary>
        /// Builds directory path based on environment settings.
        /// </summary>
        /// <returns>Application directory path</returns>
        /// <exception cref="Exception">When called on unsupported platform.</exception>
        public static string BuildAppDirectoryPath()
        {
            var homedir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            // Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
                if (appData != null)
                {
                    return appData;
                }
                else if (userProfile != null)
                {
                    return string.Join(Environment.GetEnvironmentVariable("USERPROFILE"), "AppData", "Roaming");
                }
                else
                {
                    throw new Exception("Not able to find APPDATA or USERPROFILE");
                }
            }

            // Mac
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return string.Join(homedir, "Library", "Application Support");
            }

            // Linux
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (xdgConfigHome != null)
                {
                    return xdgConfigHome;
                }
                else
                {
                    return string.Join(homedir, ".config");
                }
            }
            else
            {
                throw new Exception("Platform not supported");
            }
        }
    }
}
