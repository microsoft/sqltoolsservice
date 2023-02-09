//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Data.SqlClient;

namespace Microsoft.SqlTools.Authentication.Sql
{
    /// <summary>
    /// Provides an implementation of <see cref="SqlAuthenticationProvider"/> for SQL Tools to be able to perform Federated authentication
    /// with Microsoft.Data.SqlClient integration only for "ActiveDirectory" authentication modes. 
    /// When registered, the SqlClient driver calls the <see cref="AcquireTokenAsync(SqlAuthenticationParameters)"/> API 
    /// with server-sent authority information to request access token when needed.
    /// </summary>
    public class AuthenticationProvider : SqlAuthenticationProvider
    {
        private const string s_defaultScopeSuffix = "/.default";

        /// <summary>
        /// Acquires access token with provided <paramref name="parameters"/>
        /// </summary>
        /// <param name="parameters">Authentication parameters</param>
        /// <returns>Authentication token containing access token and expiry date.</returns>
        public override async Task<SqlAuthenticationToken> AcquireTokenAsync(SqlAuthenticationParameters parameters)
        {
            // Setup scope
            string scope = parameters.Resource.EndsWith(s_defaultScopeSuffix) ? parameters.Resource : parameters.Resource + s_defaultScopeSuffix;
            string[] scopes = new string[] { scope };

            CancellationTokenSource cts = new CancellationTokenSource();

            // Use Connection timeout value to cancel token acquire request after certain period of time.
            cts.CancelAfter(parameters.ConnectionTimeout * 1000); // Convert to milliseconds

            /* We split audience from Authority URL here. Audience can be one of the following:
             * The Azure AD authority audience enumeration
             * The tenant ID, which can be:
             * - A GUID (the ID of your Azure AD instance), for single-tenant applications
             * - A domain name associated with your Azure AD instance (also for single-tenant applications)
             * One of these placeholders as a tenant ID in place of the Azure AD authority audience enumeration:
             * - `organizations` for a multitenant application
             * - `consumers` to sign in users only with their personal accounts
             * - `common` to sign in users with their work and school accounts or their personal Microsoft accounts
             * 
             * MSAL will throw a meaningful exception if you specify both the Azure AD authority audience and the tenant ID.
             * If you don't specify an audience, your app will target Azure AD and personal Microsoft accounts as an audience. (That is, it will behave as though `common` were specified.)
             * More information: https://docs.microsoft.com/azure/active-directory/develop/msal-client-application-configuration
            **/

            int seperatorIndex = parameters.Authority.LastIndexOf('/');
            string authority = parameters.Authority.Remove(seperatorIndex + 1);
            string audience = parameters.Authority.Substring(seperatorIndex + 1);
            string? userName = string.IsNullOrWhiteSpace(parameters.UserId) ? null : parameters.UserId;

            AuthenticationParams @params = new AuthenticationParams(
                AuthenticationMethod.ActiveDirectoryInteractive,
                authority,
                audience,
                scopes,
                userName!,
                parameters.ConnectionId);

            AccessToken? result = await new Authenticator().GetTokenAsync(@params, cts.Token);
            return new SqlAuthenticationToken(result!.Token, result!.ExpiresOn);
        }

        /// <summary>
        /// Whether or not provided <paramref name="authenticationMethod"/> is supported.
        /// </summary>
        /// <param name="authenticationMethod">SQL Authentication method</param>
        /// <returns></returns>
        public override bool IsSupported(SqlAuthenticationMethod authenticationMethod)
            => authenticationMethod == SqlAuthenticationMethod.ActiveDirectoryInteractive;

    }
}