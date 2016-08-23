//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Extension methods to ConnectParams
    /// </summary>
    public static class ConnectParamsExtensions
    {
        /// <summary>
        /// Check that the fields in ConnectParams are all valid
        /// </summary>
        public static bool IsValid(this ConnectParams parameters)
        {
            return !(
                string.IsNullOrEmpty(parameters.OwnerUri) ||
                parameters.Connection == null ||
                string.IsNullOrEmpty(parameters.Connection.Password) ||
                string.IsNullOrEmpty(parameters.Connection.ServerName) ||
                string.IsNullOrEmpty(parameters.Connection.UserName)
            );
        }
    }
}
