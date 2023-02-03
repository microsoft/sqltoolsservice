//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Azure.Core;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.Threading;
using System.Threading.Tasks;


public class AuthenticationProvider : SqlAuthenticationProvider {

    // <summary>
    /// This is a static cache instance meant to hold instances of "PublicClientApplication" mapping to information available in PublicClientAppKey.
    /// The purpose of this cache is to allow re-use of Access Tokens fetched for a user interactively or with any other mode
    /// to avoid interactive authentication request every-time, within application scope making use of MSAL's userTokenCache.
    /// </summary>
    // private static ConcurrentDictionary<PublicClientAppKey, IPublicClientApplication> s_pcaMap
    //     = new ConcurrentDictionary<PublicClientAppKey, IPublicClientApplication>();
    private static readonly string clientId = "a69788c6-1d43-44ed-9ca3-b83e194da255";
    private static readonly string redirectUri = "http://localhost/redirect";
    private static readonly string s_defaultScopeSuffix = "/.default";   
    public string homedir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    IPublicClientApplication publicClientApplication = PublicClientApplicationBuilder.Create(clientId)
                .WithClientName("Sql Tools Service")
                .WithRedirectUri(redirectUri)
                .Build();

    // Cache name: azureTokenCacheMsal-azure_publicCloud for MSAL token cache

    // TODO: 
    // Implement fetching access token in a separate class (anything related to identity client), maybe that can be moved to hosting in the future
    // helper class will be fetching token, understands clientId, redirectUri - PCA should be singleton

    var path = BuildDirectoryPath();
    var storageProperties =
     new StorageCreationPropertiesBuilder("azureTokenCacheMsal-azure_publicCloud", $"{path}/azuredatastudio/Azure Accounts")
    //  .WithLinuxKeyring(
    //      Config.LinuxKeyRingSchema,
    //      Config.LinuxKeyRingCollection,
    //      Config.LinuxKeyRingLabel,
    //      Config.LinuxKeyRingAttr1,
    //      Config.LinuxKeyRingAttr2)
     .WithMacKeyChain(
         Config.KeyChainServiceName,
         Config.KeyChainAccountName)
     .Build();


    
    public override async Task<SqlAuthenticationToken> AcquireTokenAsync(SqlAuthenticationParameters parameters)
    {
        AuthenticationResult result;   
        CancellationTokenSource cts = new CancellationTokenSource();
        // Use Connection timeout value to cancel token acquire request after certain period of time.
        cts.CancelAfter(parameters.ConnectionTimeout * 1000); // Convert to milliseconds

        // Setup scope
        string scope = parameters.Resource.EndsWith(s_defaultScopeSuffix) ? parameters.Resource : parameters.Resource + s_defaultScopeSuffix;
        string[] scopes = new string[] { scope };
        TokenRequestContext tokenRequestContext = new(scopes);


        if (parameters.AuthenticationMethod == SqlAuthenticationMethod.ActiveDirectoryInteractive)
        {
            // Find account
            System.Collections.Generic.IEnumerator<IAccount> accounts = (await publicClientApplication.GetAccountsAsync().ConfigureAwait(false)).GetEnumerator();
            IAccount account = default;
            if (accounts.MoveNext())
            {
                if (!string.IsNullOrEmpty(parameters.UserId))
                {
                    do
                    {
                        IAccount currentVal = accounts.Current;
                        if (string.Compare(parameters.UserId, currentVal.Username, StringComparison.InvariantCultureIgnoreCase) == 0)
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
                    // Fetch token
                    result = await publicClientApplication.AcquireTokenSilent(scopes, account).ExecuteAsync(cancellationToken: cts.Token).ConfigureAwait(false);
                }
                catch (MsalUiRequiredException)
                {
                    // TODO: implement this method
                    result = await AcquireTokenInteractiveDeviceFlowAsync(publicClientApplication, scopes, parameters.ConnectionId, parameters.UserId, parameters.AuthenticationMethod, cts).ConfigureAwait(false);
                }
            }
            else
            {
                // If no existing 'account' is found, we request user to sign in interactively.
                result = await AcquireTokenInteractiveDeviceFlowAsync(publicClientApplication, scopes, parameters.ConnectionId, parameters.UserId, parameters.AuthenticationMethod, cts).ConfigureAwait(false);
                // SqlClientEventSource.Log.TryTraceEvent("AcquireTokenAsync | Acquired access token (interactive) for {0} auth mode. Expiry Time: {1}", parameters.AuthenticationMethod, result?.ExpiresOn);
            }
        }
        else 
        {
            throw new Exception();
        }

        return new SqlAuthenticationToken(result.AccessToken, result.ExpiresOn);

    }

    public override bool IsSupported(SqlAuthenticationMethod authenticationMethod)
    {
        throw new System.NotImplementedException();
    }

    public string BuildDirectoryPath() 
    {
        // Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return System.GetEnvironmentVariable("APPDATA") || System.join(System.GetEnvironmentVariable("USERPROFILE"), "AppData", "Roaming");
        }

        // Mac
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return System.join(homedir, "Library", "Application Support");
        }

        // Linux
        else if (var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) 
        {
            return System.GetEnvironmentVariable("XDG_CONFIG_HOME") || System.join(homedir, ".config");
        }
        else
        {
            throw new Error("Platform not supported");
        }

    }

    private Task<AuthenticationResult> AcquireTokenInteractiveDeviceFlowAsync(object app, string[] scopes, Guid connectionId, string userId, SqlAuthenticationMethod authenticationMethod, CancellationTokenSource cts)
    {
        throw new NotImplementedException();
    }



}