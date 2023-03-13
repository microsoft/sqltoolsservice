//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Concurrent;
using Microsoft.Identity.Client;
using Microsoft.SqlTools.Authentication.Utility;
using SqlToolsLogger = Microsoft.SqlTools.Utility.Logger;

namespace Microsoft.SqlTools.Authentication
{
    /// <summary>
    /// Provides APIs to acquire access token using MSAL.NET v4 with provided <see cref="AuthenticationParams"/>.
    /// </summary>
    public class Authenticator
    {
        private AuthenticatorConfiguration configuration;

        private MSALEncryptedCacheHelper msalEncryptedCacheHelper;

        private static ConcurrentDictionary<string, IPublicClientApplication> PublicClientAppMap
            = new ConcurrentDictionary<string, IPublicClientApplication>();

        #region Public APIs
        public Authenticator(AuthenticatorConfiguration configuration, MSALEncryptedCacheHelper.IVKeyReadCallback callback)
        {
            this.configuration = configuration;
            this.msalEncryptedCacheHelper = new(configuration, callback);
        }

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
                        throw new Exception($"Invalid email address format for user: [{username}] received for Azure Active Directory authentication.");
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
                        catch (Exception e)
                        {
                            SqlToolsLogger.Verbose($"{nameof(Authenticator)}.{nameof(GetTokenAsync)} | Silent authentication failed for resource {@params.Resource} for ConnectionId {@params.ConnectionId}.");
                            SqlToolsLogger.Error(e);
                            throw;
                        }
                    }
                    else
                    {
                        SqlToolsLogger.Error($"{nameof(Authenticator)}.{nameof(GetTokenAsync)} | Account not found in MSAL cache for user.");
                        throw new Exception($"User account '{username}' not found in MSAL cache, please add linked account or refresh account credentials.");
                    }
                }
                else
                {
                    SqlToolsLogger.Error($"{nameof(Authenticator)}.{nameof(GetTokenAsync)} | User account not received.");
                    throw new Exception($"User account not received.");
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
        private IPublicClientApplication GetPublicClientAppInstance(string authority, string audience)
        {
            string authorityUrl = authority + '/' + audience;
            if (!PublicClientAppMap.TryGetValue(authorityUrl, out IPublicClientApplication? clientApplicationInstance))
            {
                clientApplicationInstance = CreatePublicClientAppInstance(authority, audience);
                this.msalEncryptedCacheHelper.RegisterCache(clientApplicationInstance.UserTokenCache);
                PublicClientAppMap.TryAdd(authorityUrl, clientApplicationInstance);
            }
            return clientApplicationInstance;
        }

        private IPublicClientApplication CreatePublicClientAppInstance(string authority, string audience) =>
            PublicClientApplicationBuilder.Create(this.configuration.AppClientId)
                .WithAuthority(authority, audience)
                .WithClientName(this.configuration.AppName)
                .WithLogging(Utils.MSALLogCallback)
                .WithDefaultRedirectUri()
                .Build();

        #endregion
    }
}
