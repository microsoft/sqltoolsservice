using Microsoft.SqlServer.Dac;
using System;

namespace Microsoft.Kusto.ServiceLayer.Utility
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
