//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a parse tsql request.
    /// </summary>
    public class ParseTSqlParams
    {
        /// <summary>
        /// Gets or sets the create streaming job TSQL.  Should not be used if Statement is set.
        /// </summary>
        public string ObjectTsql { get; set;}
    }

    /// <summary>
    /// Parameters returned from a DacFx parse tsql request.
    /// </summary>
    public class ParseTSqlResult : ResultStatus
    {
        public string objectName { get; set; }

        public bool isTable { get; set; }
    }

    /// <summary>
    /// Defines the DacFx parse tsql request type
    /// </summary>
    class ParseTSqlRequest
    {
        public static readonly RequestType<ParseTSqlParams, ParseTSqlResult> Type =
            RequestType<ParseTSqlParams, ParseTSqlResult>.Create("dacfx/parsetsql");
    }
}
