//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Migration.Contracts
{
    public class ValidateNetworkFileShareRequestParams
    {
        public string Path { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ValidateNetworkFileShareRequest
    {
        public static readonly RequestType<ValidateNetworkFileShareRequestParams, bool> Type = RequestType<ValidateNetworkFileShareRequestParams, bool>.Create("migration/validateNetworkFileShare");
    }
}