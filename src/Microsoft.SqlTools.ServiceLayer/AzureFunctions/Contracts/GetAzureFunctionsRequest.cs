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
        public string Name { get; }


        public HttpTriggerBinding? HttpTriggerBinding { get; }

        public AzureFunction(string name, HttpTriggerBinding? httpTriggerBinding)
        {
            this.Name = name;
            this.HttpTriggerBinding = httpTriggerBinding;
        }
    }

    public class HttpTriggerBinding
    {
        /// <summary>
        /// The route specified
        /// </summary>
        public string? Route { get; }

        /// <summary>
        /// The operations (methods) specified
        /// </summary>
        public string[]? Operations { get; }

        public HttpTriggerBinding(string? route, string[]? operations)
        {
            this.Route = route;
            this.Operations = operations;
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