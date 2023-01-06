//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects
{
    /// <summary>
    /// Parameters for executing a query from a provided string
    /// </summary>
    public class AddScriptObjectParams
    {
        public string Path;
        public string? Script;

        public AddScriptObjectParams(string path, string? script = null)
        {
            Path = path; ;
            Script = script;
        }
    }

    public class AddScriptObjectResult : ResultStatus
    {
    }

    public class AddScriptObjectRequest
    {
        public static readonly
            RequestType<AddScriptObjectParams, ResultStatus> Type =
                RequestType<AddScriptObjectParams, ResultStatus>.Create("sqlProjects/addScriptObject");
    }
}
