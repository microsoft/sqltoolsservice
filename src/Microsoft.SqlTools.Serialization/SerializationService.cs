//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Hosting.Contracts;

namespace Microsoft.SqlTools.Serialization
{
    /// <summary>
    /// Service responsible for securing credentials in a platform-neutral manner. This provides
    /// a generic API for read, save and delete credentials
    /// </summary>
    public class SerializationService
    {
        /// <summary>
        /// Singleton service instance
        /// </summary>
        private static Lazy<SerializationService> instance
            = new Lazy<SerializationService>(() => new SerializationService());

        /// <summary>
        /// Gets the singleton service instance
        /// </summary>
        public static SerializationService Instance
        {
            get
            {
                return instance.Value;
            }
        }

        /// <summary>
        /// Default constructor is private since it's a singleton class
        /// </summary>
        private SerializationService()
        {
        }

        public void InitializeService(IProtocolEndpoint serviceHost)
        {
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

        public async Task<SaveResultRequestResult> SaveAsAsync(SaveResultsInfo resultsInfo, RequestContext<SaveResultRequestResult> requestContext)
        {
            // TODO: Refactor currently available serialization code in sqltools to be utilized here
            switch (resultsInfo.SaveFormat) {
                case "json":
                    throw new NotImplementedException("Converting to " + resultsInfo.SaveFormat + " is not implemented.");
                    break;
                case "csv":
                    throw new NotImplementedException("Converting to " + resultsInfo.SaveFormat + " is not implemented.");
                    break;
                case "excel":
                    throw new NotImplementedException("Converting to " + resultsInfo.SaveFormat + " is not implemented.");
                    break;
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
