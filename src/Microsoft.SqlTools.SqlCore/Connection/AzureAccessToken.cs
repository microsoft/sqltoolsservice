//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlTools.SqlCore.Connection
{
    public class AzureAccessToken : IRenewableToken
    {
        public DateTimeOffset TokenExpiry { get; set; }
        public string Resource { get; set; }
        public string Tenant { get; set; }
        public string UserId { get; set; }

        private string accessToken;

        public AzureAccessToken(string accessToken)
        {
            this.accessToken = accessToken;
        }

        public string GetAccessToken()
        {
            return this.accessToken;
        }
    }
}

    