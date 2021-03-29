//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class ValidateFileShareRequestParams
    {
        public string Path { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ValidateFileShareResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ValidateFileShareRequest
    {
        public static readonly RequestType<ValidateFileShareRequestParams, ValidateFileShareResult> Type = RequestType<ValidateFileShareRequestParams, ValidateFileShareResult>.Create("migration/validateFileShare");
    }
}