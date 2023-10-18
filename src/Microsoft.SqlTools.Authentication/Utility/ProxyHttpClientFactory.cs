//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Net;
using System.Net.Security;
using Microsoft.Identity.Client;
using SqlToolsLogger = Microsoft.SqlTools.Utility.Logger;

namespace Microsoft.SqlTools.Authentication.Utility
{
    /// <summary>
    /// Implements Http Client Factory for Microsoft Identity Client to support proxy authentication.
    /// </summary>
    public class HttpClientProxyFactory : IMsalHttpClientFactory
    {
        private readonly HttpClient s_httpClient;

        /// <summary>
        /// Default constructor to construct instance with proxy URL provided.
        /// </summary>
        /// <param name="proxyUrl">Proxy URL to be used for network access.</param>
        /// <param name="proxyStrictSSL">Whether or not proxy server certficate must be strictly validated.</param>
        public HttpClientProxyFactory(string? proxyUrl, bool proxyStrictSSL)
        {
            if (proxyUrl != null)
            {
                var webProxy = new WebProxy(
                    new Uri(proxyUrl),
                    BypassOnLocal: false);

                var proxyHttpClientHandler = new HttpClientHandler
                {
                    Proxy = webProxy,
                    UseProxy = true,
                    ServerCertificateCustomValidationCallback = (request, certs, chain, policyErrors) =>
                    {
                        if (policyErrors != SslPolicyErrors.None)
                        {
                            SqlToolsLogger.Error("Proxy Server Certificate Validation failed with error: " + policyErrors.ToString());
                        }

                        if (proxyStrictSSL)
                        {
                            return policyErrors != SslPolicyErrors.None;
                        }
                        // Bypass Proxy server certificate validation.
                        else
                        {
                            SqlToolsLogger.Information("Proxy Server Certificate Validation bypassed as requested.");
                            return true;
                        }
                    }
                };

                s_httpClient = new HttpClient(proxyHttpClientHandler);
                SqlToolsLogger.Verbose($"Received Http Proxy URL is used to instantiate HttpClientFactory");
            }
            else
            {
                s_httpClient = new HttpClient();
            }
        }

        /// <summary>
        /// Gets Http Client instance
        /// </summary>
        /// <returns></returns>
        public HttpClient GetHttpClient()
        {
            return s_httpClient;
        }
    }
}
