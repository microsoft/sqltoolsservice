//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.AzureFunctions.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.AzureFunctions
{
    /// <summary>
    /// Main class for Azure Functions service
    /// </summary>
    class AzureFunctionsService
    {
        private static readonly Lazy<AzureFunctionsService> instance = new Lazy<AzureFunctionsService>(() => new AzureFunctionsService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static AzureFunctionsService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.RegisterRequestHandler(AddSqlBindingRequest.Type, this.HandleAddSqlBindingRequest);
            serviceHost.RegisterRequestHandler(GetAzureFunctionsRequest.Type, this.HandleGetAzureFunctionsRequest);
        }

        /// <summary>
        /// Handles request to add sql binding into Azure Functions
        /// </summary>
        public async Task<ResultStatus> HandleAddSqlBindingRequest(AddSqlBindingParams parameters)
        {
            AddSqlBindingOperation operation = new AddSqlBindingOperation(parameters);
            ResultStatus result = operation.AddBinding();

            return result;
        }

        /// <summary>
        /// Handles request to get the names of the Azure functions in a file
        /// </summary>
        public async Task<GetAzureFunctionsResult> HandleGetAzureFunctionsRequest(GetAzureFunctionsParams parameters)
        {
            GetAzureFunctionsOperation operation = new GetAzureFunctionsOperation(parameters);
            GetAzureFunctionsResult result = operation.GetAzureFunctions();

            return result;
        }
    }
}
