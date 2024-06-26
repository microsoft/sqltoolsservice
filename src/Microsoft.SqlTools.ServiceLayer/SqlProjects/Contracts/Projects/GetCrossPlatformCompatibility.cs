﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Get the cross-platform compatibility status for a project
    /// </summary>
    public class GetCrossPlatformCompatibilityRequest
    {
        public static readonly RequestType<SqlProjectParams, GetCrossPlatformCompatibilityResult> Type = RequestType<SqlProjectParams, GetCrossPlatformCompatibilityResult>.Create("sqlProjects/getCrossPlatformCompatibility");
    }

    /// <summary>
    /// Result containing whether the project is cross-platform compatible
    /// </summary>
    public class GetCrossPlatformCompatibilityResult : ResultStatus
    {
        /// <summary>
        /// Whether the project is cross-platform compatible
        /// </summary>
        public bool IsCrossPlatformCompatible { get; set; }
    }
}
