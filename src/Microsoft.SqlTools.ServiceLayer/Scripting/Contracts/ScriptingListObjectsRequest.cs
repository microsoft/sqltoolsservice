//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Parameters for a list objects request.
    /// </summary>
    public class ScriptingListObjectsParams
    {
        public string ConnectionString { get; set; }
    }

    /// <summary>
    /// Parameters for list object result.
    /// </summary>
    public class ScriptingListObjectsResult
    {
        public string OperationId { get; set; }
    }

    public class ScriptingListObjectsRequest
    {
        public static readonly RequestType<ScriptingListObjectsParams, ScriptingListObjectsResult> Type = 
            RequestType<ScriptingListObjectsParams, ScriptingListObjectsResult>.Create("scripting/listObjects");
    }
}
