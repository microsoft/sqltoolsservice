//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters to get objects from SQL model
    /// </summary>
    public class GetObjectsFromTSqlModelParams
    {
        /// <summary>
        /// URI of the project file this model is for
        /// </summary>
        public string ProjectUri { get; set; }

        /// <summary>
        /// Object types to query
        /// </summary>
        public string[] ObjectTypes { get; set; }
    }

    /// <summary>
    /// Defines the get objects sql model request
    /// </summary>
    class GetObjectsFromTSqlModelRequest
    {
        public static readonly RequestType<GetObjectsFromTSqlModelParams, TSqlObjectInfo[]> Type =
            RequestType<GetObjectsFromTSqlModelParams, TSqlObjectInfo[]>.Create("dacFx/getObjectsFromTSqlModel");
    }
}
