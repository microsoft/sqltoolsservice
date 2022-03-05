//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.AzureFunctions.Contracts
{
    /// <summary>
    /// Parameters for getting the Azure functions in a file
    /// </summary>
    public class GetAzureFunctionsParams
    {
        /// <summary>
        /// Gets or sets the filePath
        /// </summary>
        public string filePath { get; set; }
    }

    /// <summary>
    /// Parameters returned from a get Azure functions request
    /// </summary>
    public class GetAzureFunctionsResult : ResultStatus
    {
        public string[] azureFunctions { get; set; }
    }

    /// <summary>
    /// Defines the get Azure functions request
    /// </summary>
    class GetAzureFunctionsRequest
    {
        public static readonly RequestType<GetAzureFunctionsParams, GetAzureFunctionsResult> Type =
            RequestType<GetAzureFunctionsParams, GetAzureFunctionsResult>.Create("azureFunctions/getAzureFunctions");
    }
}