//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Authentication.Utility
{
    /// <summary>
    /// Configuration used by <see cref="Authenticator"/> to perform Microsoft Entra authentication using MSAL.NET
    /// </summary>
    public class AuthenticatorConfiguration
    {
        /// <summary>
        /// Application Client ID to be used.
        /// </summary>
        public string AppClientId { get; set; } 

        /// <summary>
        /// Application name used for public client application instantiation.
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// Cache folder path, to be used by MSAL.NET to store encrypted token cache.
        /// </summary>
        public string CacheFolderPath { get; set; }

        /// <summary>
        /// File name to be used for token storage.
        /// Full path of file: <see cref="CacheFolderPath"/> \ <see cref="CacheFileName"/>
        /// </summary>
        public string CacheFileName { get; set; }

        /// <summary>
        /// Proxy URL defined by end user.
        /// </summary>
        public string? HttpProxyUrl { get; set; }

        /// <summary>
        /// Whether the proxy server certificate must be verified against list of configured CAs.
        /// </summary>
        public bool HttpProxyStrictSSL { get; set; }

        public AuthenticatorConfiguration(string appClientId, string appName, string cacheFolderPath, string cacheFileName, string? httpProxyUrl = null, bool httpProxyStrictSSL = true) {
            AppClientId = appClientId;
            AppName = appName;
            CacheFolderPath = cacheFolderPath;
            CacheFileName = cacheFileName;
            HttpProxyUrl = httpProxyUrl;
            HttpProxyStrictSSL = httpProxyStrictSSL;
        }
    }
}
