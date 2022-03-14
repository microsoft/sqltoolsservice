//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac;
using System;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    class AccessTokenProvider : IUniversalAuthProvider
    {
        private string _accessToken;

        public AccessTokenProvider(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentNullException("accessToken");
            }

            _accessToken = accessToken;
        }

        public bool IsTokenExpired() { return false; }

        public string GetValidAccessToken() { return _accessToken; }
    }
}
