//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Copilot.Contracts
{
    /// <summary>
    /// Parameters to the <see cref="RunQueryToolRequest"/>.
    /// </summary>
    public class RunQueryToolParams
    {
        public string ConnectionUri { get; set; }

        public string Query { get; set; }
    }

    public class RunQueryToolResponse
    {
        /// <summary>
        /// Flag indicating whether the query tool was successfully run.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The result of the query tool execution.
        /// </summary>
        public string Result { get; set; }
    }
   
    /// <summary>
    /// Request to start a conversation with the Copilot service.
    /// </summary>
    public class RunQueryToolRequest
    {
        public static readonly
            RequestType<RunQueryToolParams, RunQueryToolResponse> Type =
            RequestType<RunQueryToolParams, RunQueryToolResponse>.Create("copilot/tools/runquery");
    }
}
