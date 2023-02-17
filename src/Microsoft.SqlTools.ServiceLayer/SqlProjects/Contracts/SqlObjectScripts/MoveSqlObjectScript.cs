//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Parameters for moving an item from Path to DestinationPath
    /// </summary>
    public class MoveItemParams : SqlProjectScriptParams
    {
        /// <summary>
        /// Destination path of the file or folder, relative to the .sqlproj
        /// </summary>
        public string DestinationPath { get; set; } 
    }

    public class MoveSqlObjectScriptRequest
    {
        public static readonly RequestType<MoveItemParams, ResultStatus> Type = RequestType<MoveItemParams, ResultStatus>.Create("sqlProjects/moveSqlObjectScript");
    }
}
