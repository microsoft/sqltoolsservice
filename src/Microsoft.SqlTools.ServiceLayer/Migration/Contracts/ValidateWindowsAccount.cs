//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class ValidateWindowsAccountRequestParams
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ValidateWindowsAccountRequest
    {
        public static readonly RequestType<ValidateWindowsAccountRequestParams, bool> Type = RequestType<ValidateWindowsAccountRequestParams, bool>.Create("migration/validateWindowsAccount");
    }
}