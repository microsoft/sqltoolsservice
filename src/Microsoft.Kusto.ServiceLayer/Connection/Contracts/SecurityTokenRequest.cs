//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// SecurityToken Request mapping entry 
    /// </summary>
    public class SecurityTokenRequest
    {
        public static readonly
            RequestType<RequestSecurityTokenParams, RequestSecurityTokenResponse> Type =
                RequestType<RequestSecurityTokenParams, RequestSecurityTokenResponse>.Create(
                    "account/securityTokenRequest");
    }
}