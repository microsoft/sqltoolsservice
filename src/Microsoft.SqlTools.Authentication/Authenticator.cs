//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Microsoft.SqlTools.Authentication
{
    /// <summary>
    /// Provides APIs to acquire access token using MSAL.NET v4 with provided <see cref="AuthenticationParams"/>.
    /// </summary>
    public class Authenticator
    {
        private const string ApplicationClientId = "a69788c6-1d43-44ed-9ca3-b83e194da255";
        private const string ApplicationName = "azuredatastudio";
        private const string AzureTokenFolder = "Azure Accounts";
        private const string MSAL_CacheName = "azureTokenCacheMsal-azure_publicCloud";
        private const string RedirectUri = "http://localhost";

        private static ConcurrentDictionary<string, IPublicClientApplication> s_pcaMap
            = new ConcurrentDictionary<string, IPublicClientApplication>();

        #region Public APIs
        public Authenticator() { }

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
            IPublicClientApplication publicClientApplication = GetPublicClientAppInstance(@params.Authority, @params.Audience);

            var cachePath = Path.Combine(BuildDirectoryPath(), ApplicationName, AzureTokenFolder);
            var storageCreationProperties = new StorageCreationPropertiesBuilder(MSAL_CacheName, cachePath)
                .WithCacheChangedEvent(ApplicationClientId, @params.Authority + '/' + @params.Audience)
                .WithUnprotectedFile().Build();

            // This hooks up the cross-platform cache into MSAL
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageCreationProperties);
            cacheHelper.RegisterCache(publicClientApplication.UserTokenCache);

            AuthenticationResult? result;
            if (@params.AuthenticationMethod == AuthenticationMethod.ActiveDirectoryInteractive)
            {
                // Handle username format to extract email: "John Doe - johndoe@constoso.com"
                string username = @params.Username.Contains(" - ") ? @params.Username.Split(" - ")[1] : @params.Username;
                
                // Find account
                IEnumerator<IAccount>? accounts = (await publicClientApplication.GetAccountsAsync().ConfigureAwait(false)).GetEnumerator();
                IAccount? account = default;
                if (accounts.MoveNext())
                {
                    if (!string.IsNullOrEmpty(username))
                    {
                        do
                        {
                            IAccount? currentVal = accounts.Current;
                            if (string.Compare(username, currentVal.Username, StringComparison.InvariantCultureIgnoreCase) == 0)
                            {
                                account = currentVal;
                                break;
                            }
                        }
                        while (accounts.MoveNext());
                    }
                    else
                    {
                        account = accounts.Current;
                    }
                }

                if (null != account)
                {
                    try
                    {
                        // Fetch token silently
                        result = await publicClientApplication.AcquireTokenSilent(@params.Scopes, account)
                            .ExecuteAsync(cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception e) when (e is MsalUiRequiredException || e is MsalServiceException)
                    {
                        result = await AcquireTokenInteractiveAsync(publicClientApplication,
                            @params.Scopes, @params.ConnectionId, username,
                            @params.AuthenticationMethod, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                else
                {
                    // If no existing 'account' is found, we request user to sign in interactively.
                    result = await AcquireTokenInteractiveAsync(publicClientApplication,
                        @params.Scopes, @params.ConnectionId, username,
                        @params.AuthenticationMethod, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                throw new Exception($"Authentication Method ${@params.AuthenticationMethod} is not supported.");
            }

            return result != null ? new AccessToken(result.AccessToken, result.ExpiresOn) : null;
        }

        #endregion

        #region Private methods
        private IPublicClientApplication GetPublicClientAppInstance(string authority, string audience)
        {
            string authorityUrl = authority + '/' + audience;
            if (!s_pcaMap.TryGetValue(authorityUrl, out IPublicClientApplication? clientApplicationInstance))
            {
                clientApplicationInstance = CreatePublicClientAppInstance(authority, audience);
                s_pcaMap.TryAdd(authorityUrl, clientApplicationInstance);
            }
            return clientApplicationInstance;
        }

        private IPublicClientApplication CreatePublicClientAppInstance(string authority, string audience) =>
            PublicClientApplicationBuilder.Create(ApplicationClientId)
                .WithAuthority(authority, audience)
                .WithClientName(ApplicationName)
                .WithRedirectUri(RedirectUri)
                .WithClientId(ApplicationClientId)
                .WithLogging((logLevel, message, pii) =>
                {
                    switch (logLevel)
                    {
                        case LogLevel.Error:
                            if (!pii) Utility.Logger.Error(message);
                            break;
                        case LogLevel.Warning:
                            if (!pii) Utility.Logger.Warning(message);
                            break;
                        case LogLevel.Info:
                            if (!pii) Utility.Logger.Information(message);
                            break;
                        case LogLevel.Verbose:
                            if (!pii) Utility.Logger.Verbose(message);
                            break;
                        case LogLevel.Always:
                            if (!pii) Utility.Logger.Critical(message);
                            break;
                    }
                })
                .Build();

        private async Task<AuthenticationResult> AcquireTokenInteractiveAsync(IPublicClientApplication publicClientApplication, string[] scopes, Guid connectionId, string username, AuthenticationMethod authenticationMethod, CancellationToken cancellationToken)
        {
            try
            {
                CancellationTokenSource ctsInteractive = new CancellationTokenSource();
                /*
                 * On .NET Core, MSAL will start the system browser as a separate process. MSAL does not have control over this browser,
                 * but once the user finishes authentication, the web page is redirected in such a way that MSAL can intercept the Uri.
                 * MSAL cannot detect if the user navigates away or simply closes the browser. Apps using this technique are encouraged
                 * to define a timeout (via CancellationToken). We recommend a timeout of at least a few minutes, to take into account
                 * cases where the user is prompted to change password or perform 2FA.
                 *
                 * https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/System-Browser-on-.Net-Core#system-browser-experience
                 */
                ctsInteractive.CancelAfter(180000);

                return await publicClientApplication.AcquireTokenInteractive(scopes)
                    .WithCorrelationId(connectionId)
                    .WithLoginHint(username)
                    .ExecuteAsync(ctsInteractive.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new Exception($"Timeout occurred or operation canceled by user. Failed to acquire token interactively.");
            }
        }

        public string BuildDirectoryPath()
        {
            var homedir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            // Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
                if (appData != null)
                {
                    return appData;
                }
                else if (userProfile != null)
                {
                    return String.Join(Environment.GetEnvironmentVariable("USERPROFILE"), "AppData", "Roaming");
                }
                else
                {
                    throw new Exception("Not able to find APPDATA or USERPROFILE");
                }
            }

            // Mac
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return String.Join(homedir, "Library", "Application Support");
            }

            // Linux
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (xdgConfigHome != null)
                {
                    return xdgConfigHome;
                }
                else
                {
                    return String.Join(homedir, ".config");
                }
            }
            else
            {
                throw new Exception("Platform not supported");
            }
        }
        #endregion
    }
}
