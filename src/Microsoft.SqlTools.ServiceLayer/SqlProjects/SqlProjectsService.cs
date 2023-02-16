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
    public sealed class SqlProjectsService : BaseService
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
            serviceHost.SetRequestHandler(GetCrossPlatformCompatiblityRequest.Type, HandleGetCrossPlatformCompatibilityRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(UpdateProjectForCrossPlatformRequest.Type, HandleUpdateProjectForCrossPlatformRequest, isParallelProcessingSupported: false);

            // SQL object script functions
            serviceHost.SetRequestHandler(AddSqlObjectScriptRequest.Type, HandleAddSqlObjectScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeleteSqlObjectScriptRequest.Type, HandleDeleteSqlObjectScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(ExcludeSqlObjectScriptRequest.Type, HandleExcludeSqlObjectScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(MoveSqlObjectScriptRequest.Type, HandleMoveSqlObjectScriptRequest, isParallelProcessingSupported: false);

            // Pre/Post-deployment script functions
            serviceHost.SetRequestHandler(AddPreDeploymentScriptRequest.Type, HandleAddPreDeploymentScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeletePreDeploymentScriptRequest.Type, HandleDeletePreDeploymentScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(ExcludePreDeploymentScriptRequest.Type, HandleExcludePreDeploymentScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(MovePreDeploymentScriptRequest.Type, HandleMovePreDeploymentScriptRequest, isParallelProcessingSupported: false);

            serviceHost.SetRequestHandler(AddPostDeploymentScriptRequest.Type, HandleAddPostDeploymentScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeletePostDeploymentScriptRequest.Type, HandleDeletePostDeploymentScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(ExcludePostDeploymentScriptRequest.Type, HandleExcludePostDeploymentScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(MovePostDeploymentScriptRequest.Type, HandleMovePostDeploymentScriptRequest, isParallelProcessingSupported: false);

            // Folder functions
            serviceHost.SetRequestHandler(AddFolderRequest.Type, HandleAddFolderRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeleteFolderRequest.Type, HandleDeleteFolderRequest, isParallelProcessingSupported: false);

            // SQLCMD variable functions
            serviceHost.SetRequestHandler(AddSqlCmdVariableRequest.Type, HandleAddSqlCmdVariableRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeleteSqlCmdVariableRequest.Type, HandleDeleteSqlCmdVariableRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(UpdateSqlCmdVariableRequest.Type, HandleUpdateSqlCmdVariableRequest, isParallelProcessingSupported: false);

            // Database reference functions
            serviceHost.SetRequestHandler(AddSystemDatabaseReferenceRequest.Type, HandleAddSystemDatabaseReferenceRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(AddDacpacReferenceRequest.Type, HandleAddDacpacReferenceRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(AddSqlProjectReferenceRequest.Type, HandleAddSqlProjectReferenceRequest, isParallelProcessingSupported: false);
        }

        #region Handlers

        #region Project-level functions

        internal async Task HandleOpenSqlProjectRequest(SqlProjectParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri!), requestContext);
        }

        internal async Task HandleCloseSqlProjectRequest(SqlProjectParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => Projects.TryRemove(requestParams.ProjectUri!, out _), requestContext);
        }

        internal async Task HandleNewSqlProjectRequest(NewSqlProjectParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(async () =>
            {
                await SqlProject.CreateProjectAsync(requestParams.ProjectUri!, new CreateSqlProjectParams() { ProjectType = requestParams.SqlProjectType, DspVersion = requestParams.DatabaseSchemaProvider });
                GetProject(requestParams.ProjectUri!); // load into the cache
            }, requestContext);
        }

        internal async Task HandleGetCrossPlatformCompatibilityRequest(SqlProjectParams requestParams, RequestContext<GetCrossPlatformCompatiblityResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                return new GetCrossPlatformCompatiblityResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    IsCrossPlatformCompatible = GetProject(requestParams.ProjectUri!).CrossPlatformCompatible
                };
            }, requestContext);
        }

        internal async Task HandleUpdateProjectForCrossPlatformRequest(SqlProjectParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri!).UpdateForCrossPlatform(), requestContext);
        }

        #endregion

        #region Script/folder functions

        #region SQL object script functions

        internal async Task HandleAddSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri!).SqlObjectScripts.Add(new SqlObjectScript(requestParams.Path!)), requestContext);
        }

        internal async Task HandleDeleteSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri!).SqlObjectScripts.Delete(requestParams.Path!), requestContext);
        }

        internal async Task HandleExcludeSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri!).SqlObjectScripts.Exclude(requestParams.Path!), requestContext);
        }

        internal async Task HandleMoveSqlObjectScriptRequest(MoveItemParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).SqlObjectScripts.Move(requestParams.Path, requestParams.DestinationPath), requestContext);
        }

        #endregion

        #region Pre/Post-deployment script functions

        internal async Task HandleAddPreDeploymentScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).PreDeployScripts.Add(new PreDeployScript(requestParams.Path)), requestContext);
        }

        internal async Task HandleDeletePreDeploymentScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).PreDeployScripts.Delete(requestParams.Path), requestContext);
        }

        internal async Task HandleExcludePreDeploymentScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).PreDeployScripts.Exclude(requestParams.Path), requestContext);
        }

        internal async Task HandleMovePreDeploymentScriptRequest(MoveItemParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).PreDeployScripts.Move(requestParams.Path, requestParams.DestinationPath), requestContext);
        }

        internal async Task HandleAddPostDeploymentScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).PostDeployScripts.Add(new PostDeployScript(requestParams.Path)), requestContext);
        }

        internal async Task HandleDeletePostDeploymentScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).PostDeployScripts.Delete(requestParams.Path), requestContext);
        }

        internal async Task HandleExcludePostDeploymentScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).PostDeployScripts.Exclude(requestParams.Path), requestContext);
        }

        internal async Task HandleMovePostDeploymentScriptRequest(MoveItemParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).PostDeployScripts.Move(requestParams.Path, requestParams.DestinationPath), requestContext);
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

        #endregion

        #region Database Reference functions

        internal async Task HandleAddSystemDatabaseReferenceRequest(AddSystemDatabaseReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri!).DatabaseReferences.Add(
                new SystemDatabaseReference(
                    requestParams.SystemDatabase,
                    requestParams.SuppressMissingDependencies,
                    requestParams.DatabaseLiteral)),
                requestContext);
        }

        internal async Task HandleAddDacpacReferenceRequest(AddDacpacReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                requestParams.Validate();

                SqlProject project = GetProject(requestParams.ProjectUri!);
                DacpacReference reference;

                if (requestParams.DatabaseLiteral != null) // same server, different database via database name literal
                {
                    reference = new DacpacReference(
                        requestParams.DacpacPath,
                        requestParams.SuppressMissingDependencies,
                        requestParams.DatabaseLiteral);
                }
                else if (requestParams.DatabaseVariable != null) // different database, possibly different server via sqlcmdvar
                {
                    reference = new DacpacReference(
                        requestParams.DacpacPath,
                        requestParams.SuppressMissingDependencies,
                        project.SqlCmdVariables.Get(requestParams.DatabaseVariable!),
                        requestParams.ServerVariable != null ? project.SqlCmdVariables.Get(requestParams.ServerVariable) : null);
                }
                else // same database
                {
                    reference = new DacpacReference(requestParams.DacpacPath, requestParams.SuppressMissingDependencies);
                }

                project.DatabaseReferences.Add(reference);
            }, requestContext);
        }

        internal async Task HandleAddSqlProjectReferenceRequest(AddSqlProjectReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                requestParams.Validate();

                SqlProject project = GetProject(requestParams.ProjectUri!);
                SqlProjectReference reference;

                if (requestParams.DatabaseLiteral != null) // same server, different database via database name literal
                {
                    reference = new SqlProjectReference(
                        requestParams.ProjectPath,
                        requestParams.ProjectGuid,
                        requestParams.SuppressMissingDependencies,
                        requestParams.DatabaseLiteral);
                }
                else if (requestParams.DatabaseVariable != null) // different database, possibly different server via sqlcmdvar
                {
                    reference = new SqlProjectReference(
                        requestParams.ProjectPath,
                        requestParams.ProjectGuid, requestParams.SuppressMissingDependencies,
                        project.SqlCmdVariables.Get(requestParams.DatabaseVariable!),
                        requestParams.ServerVariable != null ? project.SqlCmdVariables.Get(requestParams.ServerVariable) : null);
                }
                else // same database
                {
                    reference = new SqlProjectReference(
                        requestParams.ProjectPath,
                        requestParams.ProjectGuid,
                        requestParams.SuppressMissingDependencies);
                }

                project.DatabaseReferences.Add(reference);
            }, requestContext);
        }

        internal async Task HandleDeleteDatabaseReferenceRequest(DeleteDatabaseReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri!).DatabaseReferences.Delete(requestParams.Name!), requestContext);
        }

        #endregion


        #region SQLCMD variable functions

        internal async Task HandleAddSqlCmdVariableRequest(AddSqlCmdVariableParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri!).SqlCmdVariables.Add(new SqlCmdVariable(requestParams.Name, requestParams.DefaultValue, requestParams.Value)), requestContext);
        }

        internal async Task HandleDeleteSqlCmdVariableRequest(DeleteSqlCmdVariableParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri!).SqlCmdVariables.Delete(requestParams.Name), requestContext);
        }

        internal async Task HandleUpdateSqlCmdVariableRequest(AddSqlCmdVariableParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                SqlProject project = GetProject(requestParams.ProjectUri!);
                project.SqlCmdVariables.Delete(requestParams.Name); // idempotent (won't throw if doesn't exist)
                project.SqlCmdVariables.Add(new SqlCmdVariable(requestParams.Name, requestParams.DefaultValue, requestParams.Value));
            }, requestContext);
        }

        #endregion

        #endregion

        #region Helper methods

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
