//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.Serialization
{
    /// <summary>
    /// Service responsible for securing credentials in a platform-neutral manner. This provides
    /// a generic API for read, save and delete credentials
    /// </summary>
    
    [Export(typeof(IHostedService))]
    public class SerializationService : HostedService<SerializationService>, IComposableService
    {
        public override void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Write(LogLevel.Verbose, "Serialization initialized");
            // Register request and event handlers with the Service Host
            serviceHost.SetRequestHandler(SaveAsRequest.Type, HandleSaveAsRequest);
        }

        public async Task HandleSaveAsRequest(SaveResultsInfo resultsInfo, RequestContext<SaveResultRequestResult> requestContext)
        {
            Func<Task<SaveResultRequestResult>> doSave = () =>
            {
                return SaveAsAsync(resultsInfo, requestContext);
            };

            await HandleRequest(doSave, requestContext, "HandleSaveAsRequest");
        }

        public Task<SaveResultRequestResult> SaveAsAsync(SaveResultsInfo resultsInfo, RequestContext<SaveResultRequestResult> requestContext)
        {
            // TODO: Refactor currently available serialization code in sqltools to be utilized here
            // Issue here: https://github.com/Microsoft/carbon/issues/1789
            switch (resultsInfo.SaveFormat) {
                case "json":
                    throw new NotImplementedException("Converting to " + resultsInfo.SaveFormat + " is not implemented.");
                case "csv":
                    throw new NotImplementedException("Converting to " + resultsInfo.SaveFormat + " is not implemented.");
                case "excel":
                    throw new NotImplementedException("Converting to " + resultsInfo.SaveFormat + " is not implemented.");
                default:
                    throw new NotImplementedException("Converting to " + resultsInfo.SaveFormat + " is not implemented.");

            }
        }

        private async Task HandleRequest<T>(Func<Task<T>> handler, RequestContext<T> requestContext, string requestType)
        {
            Logger.Write(LogLevel.Verbose, requestType);

            try
            {
                T result = await handler();
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

    }
}
