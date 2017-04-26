//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.TaskServices.Contracts;
using System;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    public class TaskService
    {
        private static readonly Lazy<TaskService> instance = new Lazy<TaskService>(() => new TaskService());

        /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        internal TaskService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static TaskService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost, SqlToolsContext context)
        {
            serviceHost.SetRequestHandler(ListTasksRequest.Type, HandleListTasksRequest);
        }

        /// <summary>
        /// Handles a list tasks request
        /// </summary>
        internal static async Task HandleListTasksRequest(
            ListTasksParams listTasksParams,
            RequestContext<ListTasksResponse> requestContext)
        {
            await requestContext.SendResult(new ListTasksResponse());
        }
    }
}
