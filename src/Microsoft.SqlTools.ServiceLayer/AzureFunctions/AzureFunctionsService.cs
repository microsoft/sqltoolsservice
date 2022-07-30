//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
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
            serviceHost.SetRequestHandler(AddSqlBindingRequest.Type, this.HandleAddSqlBindingRequest);
            serviceHost.SetRequestHandler(GetAzureFunctionsRequest.Type, this.HandleGetAzureFunctionsRequest);
        }

        /// <summary>
        /// Handles request to add sql binding into Azure Functions
        /// </summary>
        public async Task HandleAddSqlBindingRequest(AddSqlBindingParams parameters, RequestContext<ResultStatus> requestContext)
        {
            AddSqlBindingOperation operation = new AddSqlBindingOperation(parameters);
            ResultStatus result = operation.AddBinding();

            await requestContext.SendResult(result);
        }

        /// <summary>
        /// Handles request to get the names of the Azure functions in a file
        /// </summary>
        public async Task HandleGetAzureFunctionsRequest(GetAzureFunctionsParams parameters, RequestContext<GetAzureFunctionsResult> requestContext)
        {
            GetAzureFunctionsOperation operation = new GetAzureFunctionsOperation(parameters);
            GetAzureFunctionsResult result = operation.GetAzureFunctions();

            await requestContext.SendResult(result);
        }
    }
}
