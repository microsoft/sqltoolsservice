//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    public class SecurityToken
    {
        /// <summary>
        /// Gets or sets the refresh token.
        /// </summary>
        public string Token { get; set; }

        /// <summmary>
        /// Gets or sets the token expiration, a Unix epoch 
        /// </summary>
        public int? ExpiresOn { get; set; }

        /// <summmary>
        /// Gets or sets the token type, e.g. 'Bearer'
        /// </summary>
        public string? TokenType { get; set; }
    }
}
