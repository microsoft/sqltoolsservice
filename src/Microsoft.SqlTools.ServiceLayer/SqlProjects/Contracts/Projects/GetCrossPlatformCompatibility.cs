//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    public class GetCrossPlatformCompatiblityRequest
    {
        public static readonly RequestType<SqlProjectParams, GetCrossPlatformCompatiblityResult> Type = RequestType<SqlProjectParams, GetCrossPlatformCompatiblityResult>.Create("sqlProjects/getCrossPlatformCompatibility");
    }

    /// <summary>
    /// Result containing whether the project is cross-platform compatible
    /// </summary>
    public class GetCrossPlatformCompatiblityResult : ResultStatus
    {
        /// <summary>
        /// Whether the project is cross-platform compatible
        /// </summary>
        public bool IsCrossPlatformCompatible { get; set; }
    }
}
