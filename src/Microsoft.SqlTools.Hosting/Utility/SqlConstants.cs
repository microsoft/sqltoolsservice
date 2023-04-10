//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Utility
{
    public class SqlConstants
    {
        // Authentication Types
        public const string Integrated = "Integrated";
        public const string SqlLogin = "SqlLogin";
        public const string AzureMFA = "AzureMFA";
        public const string dstsAuth = "dstsAuth";
        public const string ActiveDirectoryInteractive = "ActiveDirectoryInteractive";
        public const string ActiveDirectoryPassword = "ActiveDirectoryPassword";

        // Azure authentication (MSAL) constants
        public const string ApplicationClientId = "a69788c6-1d43-44ed-9ca3-b83e194da255";
        public const string AzureTokenFolder = "Azure Accounts";
        public const string AzureAccountProviderCredentials = "azureAccountProviderCredentials";
        public const string MsalCacheName = "accessTokenCache";
    }
}
