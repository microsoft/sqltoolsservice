//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Concurrent;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.SqlTools.Authentication.Utility;
using SqlToolsLogger = Microsoft.SqlTools.Utility.Logger;

namespace Microsoft.SqlTools.Authentication
{
    /// <summary>
    /// Provides APIs to acquire access token using MSAL.NET v4 with provided <see cref="AuthenticationParams"/>.
    /// </summary>
    public class Authenticator
    {
        private string applicationClientId;
        private string applicationName;
        private string cacheFolderPath;
        private string cacheFileName;
        private static ConcurrentDictionary<string, IPublicClientApplication> PublicClientAppMap
            = new ConcurrentDictionary<string, IPublicClientApplication>();

        #region Public APIs
        public Authenticator(string appClientId, string appName, string cacheFolderPath, string cacheFileName)
        {
            this.applicationClientId = appClientId;
            this.applicationName = appName;
            this.cacheFolderPath = cacheFolderPath;
            this.cacheFileName = cacheFileName;
        }

        public delegate Task<AccessToken> InteractiveAuthCallback(string authority, string resource, string username, string[] scopes);

        /// <summary>
        /// Acquires access token synchronously.
        /// </summary>
        /// <param name="params">Authentication parameters to be used for access token request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Access Token with expiry date</returns>
        public AccessToken? GetToken(AuthenticationParams @params, CancellationToken cancellationToken) =>
            GetTokenAsync(@params, cancellationToken).GetAwaiter().GetResult();

        /// <summary>
        /// Acquires access token asynchronously.
        /// </summary>
        /// <param name="params">Authentication parameters to be used for access token request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Access Token with expiry date</returns>
        /// <exception cref="Exception"></exception>
        public async Task<AccessToken?> GetTokenAsync(AuthenticationParams @params, CancellationToken cancellationToken)
        {
            SqlToolsLogger.Verbose($"{nameof(Authenticator)}.{nameof(GetTokenAsync)} | Received @params: {@params.ToLogString(SqlToolsLogger.IsPiiEnabled)}");

            IPublicClientApplication publicClientApplication = GetPublicClientAppInstance(@params.Authority, @params.Audience);

            // Storage creation properties are used to enable file system caching with MSAL
            var storageCreationProperties = new StorageCreationPropertiesBuilder(this.cacheFileName, this.cacheFolderPath)
                .WithUnprotectedFile().Build();

            // This hooks up the cross-platform cache into MSAL
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageCreationProperties);
            cacheHelper.RegisterCache(publicClientApplication.UserTokenCache);

            AccessToken? accessToken;
            if (@params.AuthenticationMethod == AuthenticationMethod.ActiveDirectoryInteractive)
            {
                // Find account
                IEnumerator<IAccount>? accounts = (await publicClientApplication.GetAccountsAsync().ConfigureAwait(false)).GetEnumerator();
                IAccount? account = default;

                if (!string.IsNullOrEmpty(@params.UserName) && accounts.MoveNext())
                {
                    // Handle username format to extract email: "John Doe - johndoe@constoso.com"
                    string username = @params.UserName.Contains(" - ") ? @params.UserName.Split(" - ")[1] : @params.UserName;
                    if (!Utils.isValidEmail(username))
                    {
                        SqlToolsLogger.Pii($"{nameof(Authenticator)}.{nameof(GetTokenAsync)} | Unexpected username format, email not retreived: {@params.UserName}. " +
                            $"Accepted formats are: 'johndoe@org.com' or 'John Doe - johndoe@org.com'.");
                        throw new Exception($"Invalid email address format for user: [{username}]");
                    }

                    do
                    {
                        IAccount? currentVal = accounts.Current;
                        if (string.Compare(username, currentVal.Username, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            account = currentVal;
                            SqlToolsLogger.Verbose($"{nameof(Authenticator)}.{nameof(GetTokenAsync)} | User account found in MSAL Cache: {account.HomeAccountId}");
                            break;
                        }
                    }
                    while (accounts.MoveNext());

                    if (null != account)
                    {
                        try
                        {
                            // Fetch token silently
                            var result = await publicClientApplication.AcquireTokenSilent(@params.Scopes, account)
                                .ExecuteAsync(cancellationToken: cancellationToken)
                                .ConfigureAwait(false);
                            accessToken = new AccessToken(result!.AccessToken, result!.ExpiresOn);
                        }
                        catch (MsalUiRequiredException)
                        {
                            SqlToolsLogger.Verbose($"{nameof(Authenticator)}.{nameof(GetTokenAsync)} | Silent authentication failed to resource {@params.Authority} for ConnectionId {@params.ConnectionId}, proceeding to run callback.");
                            accessToken = await AcquireAccessTokenFromCallback(@params);
                        }
                    }
                    else
                    {
                        SqlToolsLogger.Error($"{nameof(Authenticator)}.{nameof(GetTokenAsync)} | Account not found in MSAL cache for user.");
                        accessToken = await AcquireAccessTokenFromCallback(@params);
                    }
                }
                else
                {
                    SqlToolsLogger.Error($"{nameof(Authenticator)}.{nameof(GetTokenAsync)} | User account not received.");
                    throw new Exception("User account not received.");
                }
            }
            else
            {
                SqlToolsLogger.Error($"{nameof(Authenticator)}.{nameof(GetTokenAsync)} | Authentication Method ${@params.AuthenticationMethod} is not supported.");
                throw new Exception($"Authentication Method ${@params.AuthenticationMethod} is not supported.");
            }

            return accessToken;
        }

        #endregion

        #region Private methods

        private async Task<AccessToken?> AcquireAccessTokenFromCallback(AuthenticationParams @params)
        {
            if (@params.interactiveAuthCallback != null)
            {
                return await @params.interactiveAuthCallback(@params.Authority, @params.Resource, @params.UserName, @params.Scopes);
            }
            else
            {
                throw new Exception($"{nameof(Authenticator)}.{nameof(GetTokenAsync)} | Silent authentication failed to resource {@params.Authority} for ConnectionId {@params.ConnectionId}. Authentication Callback not available.");
            }
        }

        private IPublicClientApplication GetPublicClientAppInstance(string authority, string audience)
        {
            string authorityUrl = authority + '/' + audience;
            if (!PublicClientAppMap.TryGetValue(authorityUrl, out IPublicClientApplication? clientApplicationInstance))
            {
                clientApplicationInstance = CreatePublicClientAppInstance(authority, audience);
                PublicClientAppMap.TryAdd(authorityUrl, clientApplicationInstance);
            }
            return clientApplicationInstance;
        }

        private IPublicClientApplication CreatePublicClientAppInstance(string authority, string audience) =>
            PublicClientApplicationBuilder.Create(this.applicationClientId)
                .WithAuthority(authority, audience)
                .WithClientName(this.applicationName)
                .WithLogging(Utils.MSALLogCallback)
                .WithDefaultRedirectUri()
                .Build();

        #endregion
    }
}
