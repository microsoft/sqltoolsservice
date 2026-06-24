//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.SqlCore.Connection;

namespace Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel
{
    internal static class SmoExtensions
    {
        /// <summary>
        /// Updates access token on the connection context of <paramref name="sqlObj"/> instance.
        ///
        /// No-op when the existing <see cref="ServerConnection.AccessToken"/> is a
        /// <see cref="CallbackAzureAccessToken"/>: the callback already refreshes tokens on its
        /// own.
        /// </summary>
        /// <param name="sqlObj">(this) SMO SQL Object containing connection context.</param>
        /// <param name="accessToken">Access token</param>
        public static void UpdateAccessToken(this SqlSmoObject sqlObj, string accessToken)
        {
            if (sqlObj?.ExecutionManager?.ConnectionContext == null
                || string.IsNullOrEmpty(accessToken))
            {
                return;
            }

            if (sqlObj.ExecutionManager.ConnectionContext.AccessToken is CallbackAzureAccessToken)
            {
                return;
            }

            sqlObj.ExecutionManager.ConnectionContext.AccessToken = new AzureAccessToken(accessToken);
        }
    }
}
