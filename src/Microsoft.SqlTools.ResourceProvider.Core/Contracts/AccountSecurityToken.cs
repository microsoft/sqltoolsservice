//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ResourceProvider.Core.Contracts
{
    /// <summary>
    /// Contains key information about a Token used to log in to a resource provider
    /// </summary>
    public class AccountSecurityToken
    {
        /// <summary>
        /// Expiration time for the token
        /// </summary>
        public string ExpiresOn { get; set; }

        /// <summary>
        /// URI defining the root for resource lookup
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// The actual token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// The type of token being sent - for example "Bearer" for most resource queries
        /// </summary>
        public string TokenType { get; set; }
    }
}