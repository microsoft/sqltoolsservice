//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Parameters for setting one or more generic properties in a .sqlproj file.
    /// </summary>
    public class SetProjectPropertiesParams : SqlProjectParams
    {
        /// <summary>
        /// Map of property names to their new values.
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = null!;
    }

    /// <summary>
    /// Set one or more generic properties in a .sqlproj file
    /// </summary>
    public class SetProjectPropertiesRequest
    {
        public static readonly RequestType<SetProjectPropertiesParams, ResultStatus> Type =
            RequestType<SetProjectPropertiesParams, ResultStatus>.Create("sqlProjects/setProjectProperties");
    }
}
