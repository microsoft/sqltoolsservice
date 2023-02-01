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
using Microsoft.SqlTools.ServiceLayer.Utility;

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

            // SQL object script functions
            serviceHost.SetRequestHandler(AddSqlObjectScriptRequest.Type, HandleAddSqlObjectScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeleteSqlObjectScriptRequest.Type, HandleDeleteSqlObjectScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(ExcludeSqlObjectScriptRequest.Type, HandleExcludeSqlObjectScriptRequest, isParallelProcessingSupported: false);

            // Folder functions
            serviceHost.SetRequestHandler(AddFolderRequest.Type, HandleAddFolderRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeleteFolderRequest.Type, HandleDeleteFolderRequest, isParallelProcessingSupported: false);

            // SQLCMD variable functions
            serviceHost.SetRequestHandler(AddSqlCmdVariableRequest.Type, HandleAddSqlCmdVariableRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeleteSqlCmdVariableRequest.Type, HandleDeleteSqlCmdVariableRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(UpdateSqlCmdVariableRequest.Type, HandleUpdateSqlCmdVariableRequest, isParallelProcessingSupported: false);
        }

        #region Handlers

        #region Project-level functions

        internal async Task HandleOpenSqlProjectRequest(SqlProjectParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri), requestContext);
        }

        internal async Task HandleCloseSqlProjectRequest(SqlProjectParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => Projects.TryRemove(requestParams.ProjectUri, out _), requestContext);
        }

        internal async Task HandleNewSqlProjectRequest(NewSqlProjectParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(async () =>
            {
                await SqlProject.CreateProjectAsync(requestParams.ProjectUri, requestParams.SqlProjectType, requestParams.DatabaseSchemaProvider);
                GetProject(requestParams.ProjectUri); // load into the cache

            }, requestContext);
        }

        #endregion

        #region SQL object script functions

        internal async Task HandleAddSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).SqlObjectScripts.Add(new SqlObjectScript(requestParams.Path)), requestContext);
        }

        internal async Task HandleDeleteSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).SqlObjectScripts.Delete(requestParams.Path), requestContext);
        }

        internal async Task HandleExcludeSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).SqlObjectScripts.Exclude(requestParams.Path), requestContext);
        }

        #endregion

        #region Database Reference calls

        internal async Task HandleAddSystemDatabaseReferenceRequest(AddSystemDatabaseReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).DatabaseReferences.Add(
                new SystemDatabaseReference(
                    requestParams.SystemDatabase,
                    requestParams.SuppressMissingDependencies,
                    requestParams.DatabaseVariable)),
                requestContext);
        }

        internal async Task HandleAddDacpacReferenceRequest(AddDacpacReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).DatabaseReferences.Add(
                new DacpacReference(
                    requestParams.DacpacPath,
                    requestParams.SuppressMissingDependencies,
                    requestParams.DatabaseVariable,
                    requestParams.ServerVariable)),
                requestContext);
        }

        internal async Task HandleAddSqlProjectReferenceRequest(AddSqlProjectReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).DatabaseReferences.Add(
                new SqlProjectReference(
                    requestParams.ProjectPath,
                    requestParams.ProjectGuid,
                    requestParams.SuppressMissingDependencies,
                    requestParams.DatabaseVariable,
                    requestParams.ServerVariable)),
                requestContext);
        }

        internal async Task HandleDeleteDatabaseReferenceRequest(DeleteDatabaseReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).DatabaseReferences.Delete(requestParams.Name), requestContext);
        }

        #endregion

        #region Folder functions

        internal async Task HandleAddFolderRequest(FolderParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).Folders.Add(new Folder(requestParams.Path)), requestContext);
        }

        internal async Task HandleDeleteFolderRequest(FolderParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).Folders.Delete(requestParams.Path), requestContext);
        }

        #endregion

        #region SQLCMD variable functions

        internal async Task HandleAddSqlCmdVariableRequest(AddSqlCmdVariableParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).SqlCmdVariables.Add(new SqlCmdVariable(requestParams.Name, requestParams.DefaultValue, requestParams.Value)), requestContext);
        }

        internal async Task HandleDeleteSqlCmdVariableRequest(DeleteSqlCmdVariableParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).SqlCmdVariables.Delete(requestParams.Name), requestContext);
        }

        internal async Task HandleUpdateSqlCmdVariableRequest(AddSqlCmdVariableParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                SqlProject project = GetProject(requestParams.ProjectUri);
                project.SqlCmdVariables.Delete(requestParams.Name); // idempotent (won't throw if doesn't exist)
                project.SqlCmdVariables.Add(new SqlCmdVariable(requestParams.Name, requestParams.DefaultValue, requestParams.Value));
            }, requestContext);
        }

        #endregion

        #endregion

        #region Helper methods

        private async Task RunWithErrorHandling(Action action, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(async () => await Task.Run(action), requestContext);
        }

        private async Task RunWithErrorHandling(Func<Task> action, RequestContext<ResultStatus> requestContext)
        {
            try
            {
                await action();

                await requestContext.SendResult(new ResultStatus()
                {
                    Success = true,
                    ErrorMessage = null
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendResult(new ResultStatus()
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
