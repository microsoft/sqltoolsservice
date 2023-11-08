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
                WebProxy webProxy;
                var url = new Uri(proxyUrl);

                if (url.UserInfo != null)
                {
                    // Extract username, password from userInfo.
                    // Expected format of userInfo = user@fqdn:password
                    // We do not expect username to contain colon (:) based on regex used by VS Code to validate Http.Proxy URL:
                    // (https?|socks|socks4a?|socks5h?)://([^:]*(:[^@]*)?@)?([^:]+|\\[[:0-9a-fA-F]+\\])(:\\d+)?/?$|^$
                    var separator = url.UserInfo.IndexOf(':');
                    if (separator != -1)
                    {
                        var username = url.UserInfo.Substring(0, separator);
                        var password = url.UserInfo.Substring(++separator);
                        var cleanProxyUrl = ClearCredentials(url);

                        // TODO: Support caching credentials when ADS supports the same.
                        // TODO: Support no_proxy bypass list in future.
                        webProxy = new WebProxy(
                            cleanProxyUrl, false, null, new NetworkCredential(username, password));
                        SqlToolsLogger.Information($"Network Credentials extracted from proxy Url: {cleanProxyUrl}");
                    }
                    else
                    {
                        throw new Exception("User credentials received in invalid format in Proxy URL, expected Proxy URL format: http(s)://user@fqdn:password@proxyhost:port");
                    }
                }
                else
                {
                    webProxy = new WebProxy(proxyUrl, false);
                    SqlToolsLogger.Information($"Web Proxy instantiated with Proxy Url: {proxyUrl}");
                }

                var proxyHttpClientHandler = new HttpClientHandler
                {
                    Proxy = webProxy,
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                    UseProxy = true,
                    ServerCertificateCustomValidationCallback = (request, certs, chain, policyErrors) =>
                    {
                        if (policyErrors != SslPolicyErrors.None)
                        {
                            SqlToolsLogger.Error("Proxy Server Certificate Validation failed with error: " + policyErrors.ToString());
                        }

                        if (proxyStrictSSL)
                        {
                            return policyErrors == SslPolicyErrors.None;
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

        /// <summary>
        /// Creates and returns a credential-free URL for web proxy use.
        /// </summary>
        /// <param name="url">URI instance.</param>
        /// <returns></returns>
        private string ClearCredentials(Uri url)
        {
            return string.Concat(
                url.Scheme, "://",
                url.Host, (url.IsDefaultPort ? "" : ":" + url.Port));
        }
    }
}
