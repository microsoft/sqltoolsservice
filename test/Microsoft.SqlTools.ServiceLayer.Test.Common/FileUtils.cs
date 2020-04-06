//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public static class FileUtils
    {
        public static string UserRootFolder
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Environment.GetEnvironmentVariable("USERPROFILE");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return Environment.GetEnvironmentVariable("HOME");
                }
                else
                {
                    return Environment.GetEnvironmentVariable("HOME");
                }
            }
        }

        public static string VsCodeSettingsFileName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Environment.GetEnvironmentVariable("APPDATA") + @"\Code\User\settings.json";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return Environment.GetEnvironmentVariable("HOME") + @"/Library/Application Support/Code/User/settings.json";
                }
                else
                {
                    return Environment.GetEnvironmentVariable("HOME") + @"/.config/Code/User/settings.json";
                }
            }
        }

        public static string TestServerNamesDefaultFileName
        {
            get
            {
                string testServerFileName = "testServerNames.json";
                return Path.Combine(TestServerNamesDefaultDirectory, testServerFileName);
            }
        }

        public static string TestServerNamesDefaultDirectory
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                }
                else
                {
                    return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                }
            }
        }
    }
}
