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
        internal IProtocolEndpoint ServiceHost
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
            this.ServiceHost.SetRequestHandler(StartConversationRequest.Type, HandleStartConversationRequest, true);
            this.ServiceHost.SetRequestHandler(GetNextMessageRequest.Type, HandleGetNextMessageRequest, true);
        }

        private Task HandleRequest<T>(RequestContext<T> requestContext, Func<Task> action)
        {
            // The request handling will take some time to return, we need to use a separate task to run the request handler so that it won't block the main thread.
            // For any specific table designer instance, ADS UI can make sure there are at most one request being processed at any given time, so we don't have to worry about race conditions.
            Task.Run(async () =>
            {
                try
                {
                    await action();
                }
                catch (Exception e)
                {
                    await requestContext.SendError(e);
                }
            });
            return Task.CompletedTask;
        }

        private Task HandleStartConversationRequest(StartConversationParams conversationParams, RequestContext<StartConversationResponse> requestContext)
        {
            return this.HandleRequest<StartConversationResponse>(requestContext, async () =>
            {
                bool success = await this.copilotConversationManager.StartConversation(
                    conversationParams.ConversationUri,
                    conversationParams.ConnectionUri,
                    conversationParams.UserText
                );

                await requestContext.SendResult(new StartConversationResponse() { Success = success });
            });
        }

        private async Task HandleGetNextMessageRequest(GetNextMessageParams requestParams, RequestContext<GetNextMessageResponse> requestContext)
        {          
            await this.HandleRequest<GetNextMessageResponse>(requestContext, async () =>
            {
                GetNextMessageResponse response = await this.copilotConversationManager.GetNextMessage(
                    requestParams.ConversationUri, 
                    requestParams.UserText,
                    requestParams.Tool,
                    requestParams.ToolParameters);
                await requestContext.SendResult(response);
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
