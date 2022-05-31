//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

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
        public string FilePath { get; set; }
    }

    public class AzureFunction
    {
        /// <summary>
        /// The name of the function
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The route of the HttpTrigger binding if one exists on this function
        /// </summary>
        public string? Route { get; set; }

        public AzureFunction(string name, string? route)
        {
            this.Name = name;
            this.Route = route;
        }
    }

    /// <summary>
    /// Parameters returned from a get Azure functions request
    /// </summary>
    public class GetAzureFunctionsResult
    {
        public AzureFunction[] AzureFunctions { get; set; }

        public GetAzureFunctionsResult(AzureFunction[] azureFunctions)
        {
            this.AzureFunctions = azureFunctions;
        }
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