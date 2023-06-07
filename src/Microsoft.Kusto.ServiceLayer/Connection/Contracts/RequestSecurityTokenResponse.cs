//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.Connection.Contracts
{
    public class RequestSecurityTokenResponse
    {
        /// <summary>
        /// Gets or sets the key that uniquely identifies a particular linked account.
        /// </summary>
        public string AccountKey { get; set; }

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public string Token { get; set; }
    }
}