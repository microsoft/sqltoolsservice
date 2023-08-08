//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    internal static class SmoExtensions
    {
        /// <summary>
        /// Updates access token on the connection context of <paramref name="sqlObj"/> instance.
        /// </summary>
        /// <param name="sqlObj">(this) SMO SQL Object containing connection context.</param>
        /// <param name="accessToken">Access token</param>
        public static void UpdateAccessToken(this SqlSmoObject sqlObj, string accessToken)
        {
            if (sqlObj != null && !string.IsNullOrEmpty(accessToken)
                && sqlObj.ExecutionManager != null
                && sqlObj.ExecutionManager.ConnectionContext != null)
            {
                sqlObj.ExecutionManager.ConnectionContext.AccessToken = new AzureAccessToken(accessToken);
            }
        }
    }
}
