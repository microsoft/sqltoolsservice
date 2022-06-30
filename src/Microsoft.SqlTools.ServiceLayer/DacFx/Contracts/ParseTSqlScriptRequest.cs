//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a parse T-SQL script request.
    /// </summary>
    public class ParseTSqlScriptRequestParams
    {
        /// <summary>
        /// Gets or sets the script file path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the DSP.
        /// </summary>
        public string DatabaseSchemaProvider { get; set;}
    }

    /// <summary>
    /// Result for the ParseTSqlScript Request.
    /// </summary>
    public class ParseTSqlScriptResult
    {
        public bool ContainsCreateTableStatement { get; set; }
    }

    /// <summary>
    /// Defines the parse T-SQL script request type
    /// </summary>
    class ParseTSqlScriptRequest
    {
        public static readonly RequestType<ParseTSqlScriptRequestParams, ParseTSqlScriptResult> Type =
            RequestType<ParseTSqlScriptRequestParams, ParseTSqlScriptResult>.Create("dacfx/parseTSqlScript");
    }
}
