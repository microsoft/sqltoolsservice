//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System.Collections.Generic;
using Microsoft.SqlTools.ResourceProvider.Core.Contracts;

namespace Microsoft.SqlTools.ResourceProvider.Core.Authentication
{
    /// <summary>
    /// Contains an account and related token information usable for login purposes
    /// </summary>
    public class AccountTokenWrapper
    {
        public AccountTokenWrapper(Account account, Dictionary<string, AccountSecurityToken> securityTokenMappings)
        {
            Account = account;
            SecurityTokenMappings = securityTokenMappings;
        }
        /// <summary>
        /// Account defining a connected service login
        /// </summary>
        public Account Account { get; private set; }
        /// <summary>
        /// Token mappings from tentant ID to their access token
        /// </summary>
        public Dictionary<string, AccountSecurityToken> SecurityTokenMappings { get; private set; }
    }
}
