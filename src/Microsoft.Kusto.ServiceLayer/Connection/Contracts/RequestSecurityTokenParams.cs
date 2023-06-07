//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.Connection.Contracts
{
    public class RequestSecurityTokenParams
    {
        /// <summary>
        /// Gets or sets the address of the authority to issue token.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets the provider that indicates the type of linked account to query.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the target resource that is the recipient of the requested token.
        /// </summary>
        public string Resource { get; set; }
        
        public string AccountId { get; set; }
    }
}