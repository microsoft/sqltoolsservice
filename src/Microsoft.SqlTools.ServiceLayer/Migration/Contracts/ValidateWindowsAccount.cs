//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class ValidateWindowsAccountRequestParams
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ValidateWindowsAccountResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ValidateWindowsAccountRequest
    {
        public static readonly RequestType<ValidateWindowsAccountRequestParams, ValidateWindowsAccountResult> Type = RequestType<ValidateWindowsAccountRequestParams, ValidateWindowsAccountResult>.Create("migration/validateWindowsAccount");
    }
}