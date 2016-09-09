//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Data;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Extensions to IDbConnection to enable Async* methods found in DbConnection
    /// </summary>
    public static class IDbConnectionExtensions
    {
        public static async Task OpenAsync(this IDbConnection connection)
        {
            await Task.Run(() =>
            {
                connection.Open();
            });
        }
    }
}
