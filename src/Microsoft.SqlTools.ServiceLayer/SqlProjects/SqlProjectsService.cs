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
using Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects
{
    /// <summary>
    /// Main class for SqlProjects service
    /// </summary>
    class SqlProjectsService
    {
        private static readonly Lazy<SqlProjectsService> instance = new Lazy<SqlProjectsService>(() => new SqlProjectsService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static SqlProjectsService Instance => instance.Value;

        private Lazy<ConcurrentDictionary<string, SqlProject>> projects = new Lazy<ConcurrentDictionary<string, SqlProject>>(() => new ConcurrentDictionary<string, SqlProject>(StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// <see cref="ConcurrentDictionary{String, TSqlModel}"/> that maps Project URI to Project
        /// </summary>
        public ConcurrentDictionary<string, SqlProject> Projects => projects.Value;

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            // Project-level functions
            serviceHost.SetRequestHandler(OpenSqlProjectRequest.Type, HandleOpenSqlProjectRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(CloseSqlProjectRequest.Type, HandleCloseSqlProjectRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(NewSqlProjectRequest.Type, HandleNewSqlProjectRequest, isParallelProcessingSupported: true);

            // SQL object script calls
            serviceHost.SetRequestHandler(AddSqlObjectScriptRequest.Type, HandleAddSqlObjectScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeleteSqlObjectScriptRequest.Type, HandleDeleteSqlObjectScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(ExcludeSqlObjectScriptRequest.Type, HandleExcludeSqlObjectScriptRequest, isParallelProcessingSupported: false);
        }

        #region Handlers

        #region Project-level functions

        internal async Task HandleOpenSqlProjectRequest(SqlProjectParams requestParams, RequestContext<SqlProjectResult> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri), requestContext);
        }

        internal async Task HandleCloseSqlProjectRequest(SqlProjectParams requestParams, RequestContext<SqlProjectResult> requestContext)
        {
            await RunWithErrorHandling(() => Projects.TryRemove(requestParams.ProjectUri, out _), requestContext);
        }

        internal async Task HandleNewSqlProjectRequest(NewSqlProjectParams requestParams, RequestContext<SqlProjectResult> requestContext)
        {
            await RunWithErrorHandling(async () =>
            {
                await SqlProject.CreateProjectAsync(requestParams.ProjectUri, requestParams.SqlProjectType, requestParams.DatabaseSchemaProvider);
                GetProject(requestParams.ProjectUri); // load into the cache

            }, requestContext);
        }

        #endregion

        #region Sql object script calls

        internal async Task HandleAddSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<SqlProjectResult> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).SqlObjectScripts.Add(new SqlObjectScript(requestParams.Path)), requestContext);
        }

        internal async Task HandleDeleteSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<SqlProjectResult> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).SqlObjectScripts.Delete(requestParams.Path), requestContext);
        }

        internal async Task HandleExcludeSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<SqlProjectResult> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).SqlObjectScripts.Exclude(requestParams.Path), requestContext);
        }

        #endregion

        #endregion

        #region Helper methods

        private async Task RunWithErrorHandling(Action action, RequestContext<SqlProjectResult> requestContext)
        {
            await RunWithErrorHandling(async () => await Task.Run(action), requestContext);
        }

        private async Task RunWithErrorHandling(Func<Task> action, RequestContext<SqlProjectResult> requestContext)
        {
            try
            {
                await action();

                await requestContext.SendResult(new SqlProjectResult()
                {
                    Success = true,
                    ErrorMessage = null
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendResult(new SqlProjectResult()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        private SqlProject GetProject(string projectUri)
        {
            if (!Projects.ContainsKey(projectUri))
            {
                Projects[projectUri] = new SqlProject(projectUri);
            }

            return Projects[projectUri];
        }

        #endregion
    }
}
