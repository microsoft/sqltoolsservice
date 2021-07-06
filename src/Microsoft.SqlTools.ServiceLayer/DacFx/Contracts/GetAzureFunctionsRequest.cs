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
    /// Parameters for inserting a sql binding
    /// </summary>
    public class GetAzureFunctionsParams
    {
        /// <summary>
        /// Gets or sets the filePath
        /// </summary>
        public string filePath { get; set;}
    }

    /// <summary>
    /// Parameters returned from a get azure functions request
    /// </summary>
    public class GetAzureFunctionsResult : ResultStatus
    {
        public string[] azureFunctions { get; set; }

    }

    /// <summary>
    /// Defines the get azure functions request
    /// </summary>
    class GetAzureFunctionsRequest
    {
        public static readonly RequestType<GetAzureFunctionsParams, GetAzureFunctionsResult> Type =
            RequestType<GetAzureFunctionsParams, GetAzureFunctionsResult>.Create("dacfx/getAzureFunctions");

    }
}
