//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Copilot.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Copilot
{
    /// <summary>
    /// Class that handles the Copilot related requests
    /// </summary>
    public sealed class CopilotService : IDisposable
    {
        private bool disposed = false;
        private static readonly Lazy<CopilotService> instance = new Lazy<CopilotService>(() => new CopilotService());

        private CopilotConversationManager copilotConversationManager = new CopilotConversationManager();

        public CopilotService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static CopilotService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Gets the CopilotConversationManager instance
        /// </summary>
        public CopilotConversationManager ConversationManager
        {
            get { return this.copilotConversationManager; }
        }

        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// </summary>
        internal IRpcServiceHost ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;
            this.ServiceHost.RegisterRequestHandler(StartConversationRequest.Type, HandleStartConversationRequest);
            this.ServiceHost.RegisterRequestHandler(GetNextMessageRequest.Type, HandleGetNextMessageRequest);
        }

        private async Task<T> HandleRequest<T>(Func<Task<T>> action)
        {
            // The request handling will take some time to return, we need to use a separate task to run the request handler so that it won't block the main thread.
            // For any specific table designer instance, ADS UI can make sure there are at most one request being processed at any given time, so we don't have to worry about race conditions.
            try
            {
                return await action();
            }
            catch (Exception e)
            {
                throw RpcErrorException.Create(e);
            }
        }

        private Task<StartConversationResponse> HandleStartConversationRequest(StartConversationParams conversationParams)
        {
            return this.HandleRequest<StartConversationResponse>(async () =>
            {
                bool success = await this.copilotConversationManager.StartConversation(
                    conversationParams.ConversationUri,
                    conversationParams.ConnectionUri,
                    conversationParams.UserText
                );

                return new StartConversationResponse() { Success = success };
            });
        }

        private Task<GetNextMessageResponse> HandleGetNextMessageRequest(GetNextMessageParams requestParams)
        {          
            return this.HandleRequest<GetNextMessageResponse>(async () =>
            {
                GetNextMessageResponse response = await this.copilotConversationManager.GetNextMessage(
                    requestParams.ConversationUri, 
                    requestParams.UserText,
                    requestParams.Tool,
                    requestParams.ToolParameters);
                return response;
            });
        }

        /// <summary>
        /// Disposes the service
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }
    }
}
