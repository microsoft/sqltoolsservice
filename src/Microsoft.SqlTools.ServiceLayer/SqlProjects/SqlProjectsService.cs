//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
namespace Microsoft.SqlTools.ServiceLayer.SqlProjects
{
    /// <summary>
    /// Main class for DacFx service
    /// </summary>
    class SqlProjectsService
    {
        private static readonly Lazy<SqlProjectsService> instance = new Lazy<SqlProjectsService>(() => new SqlProjectsService());

        /// <summary>
        /// <see cref="ConcurrentDictionary{String, TSqlModel}"/> that maps project uri to model
        /// </summary>
        public Lazy<ConcurrentDictionary<string, SqlProject>> projects = new Lazy<ConcurrentDictionary<string, SqlProject>>(() => new ConcurrentDictionary<string, SqlProject>());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static SqlProjectsService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(AddScriptObjectRequest.Type, this.HandleAddScriptObjectRequest, true);
        }

        public async Task HandleAddScriptObjectRequest(AddScriptObjectParams parameters, RequestContext<AddScriptObjectResult> requestContext)
        {
            await requestContext.SendResult(new AddScriptObjectResult()
            {
                Success = true,
                ErrorMessage = String.Empty
            });
        }
    }
}
