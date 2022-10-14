//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Connect request mapping entry 
    /// </summary>
    public class ChangePasswordRequest
    {
        public static readonly
            RequestType<ChangePasswordParams, bool> Type =
            RequestType<ChangePasswordParams, bool>.Create("connection/changepassword");
    }
}
