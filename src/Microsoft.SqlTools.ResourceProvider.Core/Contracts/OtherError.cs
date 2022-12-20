//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ResourceProvider.Core.Contracts
{
    public class CanHandleOtherErrorRequest
    {
        public static readonly
            RequestType<HandleOtherErrorParams, ProviderErrorCode> Type =
            RequestType<HandleOtherErrorParams, ProviderErrorCode>.Create("resource/handleOtherError");
    }

    public class HandleOtherErrorParams
    {
        /// <summary>
        /// The error code used to defined the error type
        /// </summary>
        public int ErrorCode { get; set; }
        /// <summary>
        /// The error message associated with the error.
        /// </summary>
        public string ErrorMessage { get; set; }
        /// <summary>
        /// The connection type, for example MSSQL
        /// </summary>
        public string ConnectionTypeId { get; set; }
    }

    public enum ProviderErrorCode {
        noErrorOrUnsupported = 0,
		passwordReset = 1,
    }
}