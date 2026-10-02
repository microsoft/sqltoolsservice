//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.SqlCore.IntelliSense;
using Microsoft.SqlTools.SqlCore.SchemaCompare;
using Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlProjects.References;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects
{
    /// <summary>
    /// Main class for SqlProjects service
    /// </summary>
    public sealed class SqlProjectsService : BaseService, IProjectIntelliSenseService
    {
        private static readonly Lazy<SqlProjectsService> instance = new Lazy<SqlProjectsService>(() => new SqlProjectsService());
        private const string RunSqlCodeAnalysisPropertyName = "RunSqlCodeAnalysis";
        private const string SqlCodeAnalysisRulesPropertyName = "SqlCodeAnalysisRules";
        private const string ProjectGuidPropertyName = "ProjectGuid";

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static SqlProjectsService Instance => instance.Value;

        /// <summary>
        /// One host per project URI. A host owns everything the service holds for that project and runs
        /// every operation on it one at a time. Close removes the host; the next request creates a new one.
        /// </summary>
        private readonly ConcurrentDictionary<string, ProjectHost> hosts = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Builds a project's IntelliSense model from a snapshot of the project. Tests replace it to control
        /// when a build finishes.
        /// </summary>
        internal Func<SqlProject, TSqlModel> IntelliSenseModelBuilder { get; set; } = TSqlModelBuilder.LoadModel;

        /// <summary>
        /// Snapshot of the loaded projects by URI, for diagnostics and tests.
        /// </summary>
        internal IReadOnlyDictionary<string, SqlProject> Projects => hosts.Values
            .Select(host => (host.ProjectUri, Project: host.LoadedProject))
            .Where(entry => entry.Project != null)
            .ToDictionary(entry => entry.ProjectUri, entry => entry.Project!, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The host for a project, if the service has one. For tests.
        /// </summary>
        internal ProjectHost? GetHost(string projectUri) => hosts.TryGetValue(projectUri, out ProjectHost? host) ? host : null;

        /// <summary>
        /// Resolves project database references to dacpacs and referenced projects, using the system dacpacs shipped with STS.
        /// </summary>
        internal ProjectReferenceResolver ReferenceResolver { get; set; } = new ProjectReferenceResolver(ProjectReferenceResolver.DefaultSystemDacpacsRoot);

        /// <summary>
        /// Resolved references per project file.
        /// </summary>
        private readonly ProjectReferenceCache referenceCache = new();

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            // Project-level functions
            serviceHost.SetRequestHandler(OpenSqlProjectRequest.Type, HandleOpenSqlProjectRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(CloseSqlProjectRequest.Type, HandleCloseSqlProjectRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(CreateSqlProjectRequest.Type, HandleCreateSqlProjectRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetCrossPlatformCompatibilityRequest.Type, HandleGetCrossPlatformCompatibilityRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(UpdateProjectForCrossPlatformRequest.Type, HandleUpdateProjectForCrossPlatformRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(GetProjectPropertiesRequest.Type, HandleGetProjectPropertiesRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetProjectModelRequest.Type, HandleGetProjectModelRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(FindProjectForFileRequest.Type, HandleFindProjectForFileRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(SetDatabaseSourceRequest.Type, HandleSetDatabaseSourceRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(SetDatabaseSchemaProviderRequest.Type, HandleSetDatabaseSchemaProviderRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(SetProjectPropertiesRequest.Type, HandleSetProjectPropertiesRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(UpdateCodeAnalysisRulesRequest.Type, HandleUpdateCodeAnalysisRulesRequest, isParallelProcessingSupported: false);

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

            // None script functions
            serviceHost.SetRequestHandler(AddNoneItemRequest.Type, HandleAddNoneItemRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeleteNoneItemRequest.Type, HandleDeleteNoneItemRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(ExcludeNoneItemRequest.Type, HandleExcludeNoneItemRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(MoveNoneItemRequest.Type, HandleMoveNoneItemRequest, isParallelProcessingSupported: false);

            // Folder functions
            serviceHost.SetRequestHandler(AddFolderRequest.Type, HandleAddFolderRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeleteFolderRequest.Type, HandleDeleteFolderRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(ExcludeFolderRequest.Type, HandleExcludeFolderRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(MoveFolderRequest.Type, HandleMoveFolderRequest, isParallelProcessingSupported: false);

            // SQLCMD variable functions
            serviceHost.SetRequestHandler(GetSqlCmdVariablesRequest.Type, HandleGetSqlCmdVariablesRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(AddSqlCmdVariableRequest.Type, HandleAddSqlCmdVariableRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeleteSqlCmdVariableRequest.Type, HandleDeleteSqlCmdVariableRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(UpdateSqlCmdVariableRequest.Type, HandleUpdateSqlCmdVariableRequest, isParallelProcessingSupported: false);

            // Database reference functions
            serviceHost.SetRequestHandler(GetDatabaseReferencesRequest.Type, HandleGetDatabaseReferencesRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(AddSystemDatabaseReferenceRequest.Type, HandleAddSystemDatabaseReferenceRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(AddDacpacReferenceRequest.Type, HandleAddDacpacReferenceRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(AddSqlProjectReferenceRequest.Type, HandleAddSqlProjectReferenceRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(AddNugetPackageReferenceRequest.Type, HandleAddNugetPackageReferenceRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeleteDatabaseReferenceRequest.Type, HandleDeleteDatabaseReferenceRequest, isParallelProcessingSupported: false);
        }

        #region Handlers

        #region Project-level functions

        internal async Task HandleOpenSqlProjectRequest(SqlProjectParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host =>
            {
                host.GetProject();
                // Build the IntelliSense model in the background so .sql files in this project get
                // completions without a live server connection. Reuses a model that's already built or building.
                host.EnsureIntelliSense();
            }, requestContext);
        }

        internal async Task HandleCloseSqlProjectRequest(SqlProjectParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(async () =>
            {
                if (!hosts.TryGetValue(requestParams.ProjectUri, out ProjectHost? host))
                {
                    return;
                }

                try
                {
                    await host.RunAsync(() =>
                    {
                        try
                        {
                            host.Close();
                        }
                        finally
                        {
                            // Remove the host even if teardown fails, so later requests get a new host
                            hosts.TryRemove(new KeyValuePair<string, ProjectHost>(requestParams.ProjectUri, host));
                        }
                        return Task.FromResult(true);
                    });
                }
                catch (ProjectHostClosedException)
                {
                    // Already closed
                }
            }, requestContext);
        }

        /// <summary>
        /// Returns the project's resolved database references, from the cache when none of the files they depend on changed.
        /// Reads files only; call on a background thread.
        /// </summary>
        internal ResolvedProjectReferences ResolveProjectReferences(string projectFilePath)
        {
            // Open a separate instance: the shared project objects are not safe to use from background threads.
            return referenceCache.GetOrResolve(
                projectFilePath,
                () => ReferenceResolver.Resolve(SqlProject.OpenProject(projectFilePath, onlyLoadProperties: true), projectFilePath));
        }

        /// <summary>
        /// Returns the database references of <paramref name="project"/>, already loaded from <paramref name="projectFilePath"/>,
        /// from the cache when none of the files they depend on changed. Doesn't read the .sqlproj again, so it's safe to call
        /// while requests may be writing it, as long as nothing else uses <paramref name="project"/> at the same time.
        /// </summary>
        internal ResolvedProjectReferences ResolveProjectReferences(SqlProject project, string projectFilePath)
        {
            return referenceCache.GetOrResolve(projectFilePath, () => ReferenceResolver.Resolve(project, projectFilePath));
        }

        /// <summary>
        /// Called after the project's database references or SQLCMD variables change: drops the cached resolution and
        /// reloads the references into the project's IntelliSense model in the background.
        /// </summary>
        private void OnDatabaseReferencesChanged(string projectUri)
        {
            referenceCache.Invalidate(ProjectHost.ToLocalPath(projectUri));
            if (hosts.TryGetValue(projectUri, out ProjectHost? host))
            {
                // Logs its own failures
                _ = host.ReloadReferencesAsync();
            }
        }

        /// <summary>
        /// Returns the database references and SQLCMD variable values of the project at <paramref name="projectFilePath"/>
        /// for a Schema Compare project endpoint.
        /// </summary>
        /// <returns>The references, or null when the project could not be read, in which case the comparison runs without them.</returns>
        internal SchemaCompareProjectReferences? GetSchemaCompareProjectReferences(string projectFilePath)
        {
            ResolvedProjectReferences resolved;
            try
            {
                resolved = ResolveProjectReferences(projectFilePath);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to resolve database references of {projectFilePath} for Schema Compare: {ex}");
                return null;
            }

            foreach (ResolvedDatabaseReference reference in resolved.References.Where(reference => reference.ModelReference == null))
            {
                Logger.Warning($"Database reference not loaded for Schema Compare in {projectFilePath}: {reference.Name}: {reference.Message}");
            }

            return new SchemaCompareProjectReferences(resolved.ModelReferences, resolved.SqlCmdVariables);
        }

        internal async Task HandleCreateSqlProjectRequest(Contracts.CreateSqlProjectParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(async () =>
            {
                SqlServer.Dac.Projects.CreateSqlProjectParams createParams = new()
                {
                    ProjectType = requestParams.SqlProjectType,
                    TargetPlatform = requestParams.DatabaseSchemaProvider == null ? null : Utilities.DatabaseSchemaProviderToSqlPlatform(requestParams.DatabaseSchemaProvider),
                    BuildSdkVersion = requestParams.BuildSdkVersion
                };

                // Create and load in one operation so a concurrent read waits for the finished project
                await RunOnProjectAsync(requestParams.ProjectUri, async host =>
                {
                    await SqlProject.CreateProjectAsync(requestParams.ProjectUri, createParams);
                    host.ForgetProject();
                    host.GetProject(); // load into the cache
                    return true;
                });
            }, requestContext);
        }

        internal async Task HandleGetCrossPlatformCompatibilityRequest(SqlProjectParams requestParams, RequestContext<GetCrossPlatformCompatibilityResult> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host =>
            {
                return new GetCrossPlatformCompatibilityResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    IsCrossPlatformCompatible = host.GetProject(onlyLoadProperties: true).CrossPlatformCompatible
                };
            }, requestContext);
        }

        internal async Task HandleUpdateProjectForCrossPlatformRequest(SqlProjectParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject(onlyLoadProperties: true).UpdateForCrossPlatform(), requestContext);
        }

        internal async Task HandleGetProjectPropertiesRequest(SqlProjectParams requestParams, RequestContext<GetProjectPropertiesResult> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => BuildProjectPropertiesResult(host.GetProject(onlyLoadProperties: true)), requestContext);
        }

        internal async Task HandleGetProjectModelRequest(SqlProjectParams requestParams, RequestContext<GetProjectModelResult> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host =>
            {
                // Load the full project once, rather than properties-only followed by a reload for the items
                SqlProject project = host.GetProject();

                return new GetProjectModelResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Properties = BuildProjectPropertiesResult(project),
                    IsCrossPlatformCompatible = project.CrossPlatformCompatible,
                    SqlCmdVariables = project.SqlCmdVariables.ToArray(),
                    DatabaseReferences = BuildDatabaseReferencesResult(project),
                    SqlObjectScripts = project.SqlObjectScripts.Select(x => x.Path).ToArray(),
                    PreDeploymentScripts = project.PreDeployScripts.Select(x => x.Path).ToArray(),
                    PostDeploymentScripts = project.PostDeployScripts.Select(x => x.Path).ToArray(),
                    NoneItems = project.NoneItems.Select(x => x.Path).Where(p => !IsGlobPattern(p)).ToArray(),
                    Folders = project.Folders.Select(x => x.Path).ToArray()
                };
            }, requestContext);
        }

        internal async Task HandleFindProjectForFileRequest(FindProjectForFileParams requestParams, RequestContext<FindProjectForFileResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                string? projectUri = FindProjectForFile(requestParams.FilePath);

                return new FindProjectForFileResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    ProjectUri = projectUri,
                    IsLoaded = projectUri != null && GetHost(projectUri)?.LoadedProject != null
                };
            }, requestContext);
        }

        internal async Task HandleSetDatabaseSourceRequest(SetDatabaseSourceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject(onlyLoadProperties: true).Properties.DatabaseSource = requestParams.DatabaseSource, requestContext);
        }

        internal async Task HandleSetDatabaseSchemaProviderRequest(SetDatabaseSchemaProviderParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject(onlyLoadProperties: true).Properties.TargetSqlPlatform = Utilities.DatabaseSchemaProviderToSqlPlatform(requestParams.DatabaseSchemaProvider), requestContext);
        }

        internal async Task HandleSetProjectPropertiesRequest(SetProjectPropertiesParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host =>
            {
                SqlProject project = host.GetProject(onlyLoadProperties: true);

                // First pass: apply all DacFx-managed properties so they are fully flushed
                // to disk before any raw XML edits are made against the same file.
                foreach (KeyValuePair<string, string> entry in requestParams.Properties)
                {
                    if (!string.Equals(entry.Key, ProjectGuidPropertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        project.Properties.SetProperty(entry.Key, entry.Value);
                    }
                }

                // Second pass: apply XML-only properties (e.g. ProjectGuid) now that DacFx
                // has finished writing, then evict the cached project so the next load picks
                // up the updated file.
                foreach (KeyValuePair<string, string> entry in requestParams.Properties)
                {
                    if (string.Equals(entry.Key, ProjectGuidPropertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        SetReadOnlyPropertyInXml(requestParams.ProjectUri, entry.Key, entry.Value);
                        host.ForgetProject();
                    }
                }
            }, requestContext);
        }

        internal async Task HandleUpdateCodeAnalysisRulesRequest(UpdateCodeAnalysisRulesParams requestParams, RequestContext<UpdateCodeAnalysisRulesResult> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host =>
            {
                SqlProject project = host.GetProject(onlyLoadProperties: true);

                if (requestParams.RunSqlCodeAnalysis.HasValue)
                {
                    project.Properties.SetProperty(RunSqlCodeAnalysisPropertyName, requestParams.RunSqlCodeAnalysis.Value ? "True" : "False");
                }

                // Only modify SqlCodeAnalysisRules when the caller explicitly provided rules.
                // A null Rules list means "leave existing overrides untouched".
                if (requestParams.Rules != null)
                {
                    string rulesValue = BuildCodeAnalysisRulesXmlValue(requestParams.Rules);
                    if (string.IsNullOrEmpty(rulesValue))
                    {
                        project.Properties.DeleteProperty(SqlCodeAnalysisRulesPropertyName);
                    }
                    else
                    {
                        project.Properties.SetProperty(SqlCodeAnalysisRulesPropertyName, rulesValue);
                    }
                }

                return new UpdateCodeAnalysisRulesResult()
                {
                    Success = true,
                    ErrorMessage = null
                };
            }, requestContext);
        }

        internal static string BuildCodeAnalysisRulesXmlValue(IEnumerable<CodeAnalysisRuleOverride> rules)
        {
            CodeAnalysisRuleSettings settings = new();
            foreach (CodeAnalysisRuleOverride rule in rules)
            {
                if (string.IsNullOrWhiteSpace(rule?.RuleId))
                {
                    continue;
                }

                bool enabled;
                SqlRuleProblemSeverity severity;
                switch (rule.Severity?.ToLowerInvariant())
                {
                    case "disabled":
                    case "none":
                        enabled = false;
                        severity = SqlRuleProblemSeverity.Warning;
                        break;
                    case "error":
                        enabled = true;
                        severity = SqlRuleProblemSeverity.Error;
                        break;
                    default:
                        // Warning (the DacFx default) and any unrecognized severity produce no
                        // override entry — the rule inherits its default behaviour from DacFx.
                        continue;
                }

                settings.Add(new RuleConfiguration(rule.RuleId, enabled, severity));
            }

            return settings.ConvertToSettingsString();
        }

        #endregion

        #region Script/folder functions

        #region SQL object script functions

        internal async Task HandleAddSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => RunOnProjectAsync(requestParams.ProjectUri, async host =>
            {
                host.GetProject().SqlObjectScripts.Add(new SqlObjectScript(requestParams.Path));
                // Incrementally update the IntelliSense model for the new file.
                await host.UpdateIntelliSenseAsync(requestParams.Path, deleted: false);
                return true;
            }), requestContext);
        }

        internal async Task HandleDeleteSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => RunOnProjectAsync(requestParams.ProjectUri, async host =>
            {
                host.GetProject().SqlObjectScripts.Delete(requestParams.Path);
                // Incrementally remove the deleted file's objects from the IntelliSense model.
                await host.UpdateIntelliSenseAsync(requestParams.Path, deleted: true);
                return true;
            }), requestContext);
        }

        internal async Task HandleExcludeSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => RunOnProjectAsync(requestParams.ProjectUri, async host =>
            {
                host.GetProject().SqlObjectScripts.Exclude(requestParams.Path);
                // Remove the excluded file's objects from the IntelliSense model.
                await host.UpdateIntelliSenseAsync(requestParams.Path, deleted: true);
                return true;
            }), requestContext);
        }

        internal async Task HandleMoveSqlObjectScriptRequest(MoveItemParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => RunOnProjectAsync(requestParams.ProjectUri, async host =>
            {
                host.GetProject().SqlObjectScripts.Move(requestParams.Path, requestParams.DestinationPath, requestParams.MetadataOnly);
                // The IntelliSense model is path-keyed, so a rename is a delete + add:
                // (1) Purge the old path's objects from the model and source location index.
                await host.UpdateIntelliSenseAsync(requestParams.Path, deleted: true);
                // (2) Read the file at its new path and re-register its objects under the new key.
                await host.UpdateIntelliSenseAsync(requestParams.DestinationPath, deleted: false);
                return true;
            }), requestContext);
        }

        internal async Task UpdateProjectIntelliSenseAsync(string projectUri, string filePathOrUri, bool deleted, string? sqlTextOverride = null)
        {
            // Only a project the service already holds can have IntelliSense; don't create a host for a stray update
            if (!hosts.TryGetValue(projectUri, out ProjectHost? host))
            {
                return;
            }

            try
            {
                await host.RunAsync(async () =>
                {
                    await host.UpdateIntelliSenseAsync(filePathOrUri, deleted, sqlTextOverride);
                    return true;
                });
            }
            catch (ProjectHostClosedException)
            {
                // Closed; its model is gone
            }
        }

        /// <summary>
        /// Attempts to determine whether <paramref name="name"/> (e.g. "dbo.Foo") is defined
        /// in two or more source files in this project.
        /// </summary>
        /// <returns>
        /// <c>true</c> when duplicate status could be determined from current IntelliSense state;
        /// otherwise <c>false</c> (unknown / no state).
        /// </returns>
        internal bool TryIsDuplicate(string projectUri, string name, out bool isDuplicate)
        {
            isDuplicate = false;
            if (GetHost(projectUri)?.IntelliSense is not ProjectIntelliSense state
                || string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            isDuplicate = state.Provider.IsDuplicate(name);
            return true;
        }

        /// <summary>
        /// Returns the <see cref="TSqlModelMetadataProvider"/> for the given project URI without
        /// going through the binding queue.  Used by Find All References so that a concurrent
        /// save (which rebuilds the binding context) cannot cause FAR to return empty results.
        /// </summary>
        internal bool TryGetProvider(string projectUri, out TSqlModelMetadataProvider? provider)
        {
            if (GetHost(projectUri)?.IntelliSense is ProjectIntelliSense state)
            {
                provider = state.Provider;
                return true;
            }
            provider = null;
            return false;
        }

        /// <summary>
        /// Returns a snapshot of all file URIs registered for the given project, excluding <paramref name="excludeUri"/>.
        /// Used to re-trigger diagnostics on sibling files after a save updates <c>_duplicates</c>.
        /// </summary>
        internal IReadOnlyList<string> GetSiblingProjectFileUris(string projectUri, string excludeUri)
        {
            if (GetHost(projectUri)?.IntelliSense is not ProjectIntelliSense state)
                return Array.Empty<string>();
            lock (state.FileUris)
            {
                return state.FileUris
                    .Where(u => !string.Equals(u, excludeUri, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        // Explicit IProjectIntelliSenseService implementation. These forward to the existing
        // internal members so the language service can consume the projects service through the
        // lib-side abstraction without widening this class's public surface.
        Task IProjectIntelliSenseService.UpdateProjectIntelliSenseAsync(string projectUri, string filePathOrUri, bool deleted, string sqlTextOverride)
            => UpdateProjectIntelliSenseAsync(projectUri, filePathOrUri, deleted, sqlTextOverride);

        bool IProjectIntelliSenseService.TryIsDuplicate(string projectUri, string name, out bool isDuplicate)
            => TryIsDuplicate(projectUri, name, out isDuplicate);

        IReadOnlyList<string> IProjectIntelliSenseService.GetSiblingProjectFileUris(string projectUri, string excludeUri)
            => GetSiblingProjectFileUris(projectUri, excludeUri);

        #endregion

        #region Pre/Post-deployment script functions

        internal async Task HandleAddPreDeploymentScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().PreDeployScripts.Add(new PreDeployScript(requestParams.Path)), requestContext);
        }

        internal async Task HandleDeletePreDeploymentScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().PreDeployScripts.Delete(requestParams.Path), requestContext);
        }

        internal async Task HandleExcludePreDeploymentScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().PreDeployScripts.Exclude(requestParams.Path), requestContext);
        }

        internal async Task HandleMovePreDeploymentScriptRequest(MoveItemParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().PreDeployScripts.Move(requestParams.Path, requestParams.DestinationPath), requestContext);
        }

        internal async Task HandleAddPostDeploymentScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().PostDeployScripts.Add(new PostDeployScript(requestParams.Path)), requestContext);
        }

        internal async Task HandleDeletePostDeploymentScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().PostDeployScripts.Delete(requestParams.Path), requestContext);
        }

        internal async Task HandleExcludePostDeploymentScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().PostDeployScripts.Exclude(requestParams.Path), requestContext);
        }

        internal async Task HandleMovePostDeploymentScriptRequest(MoveItemParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().PostDeployScripts.Move(requestParams.Path, requestParams.DestinationPath), requestContext);
        }

        #endregion

        #region None script functions

        internal async Task HandleAddNoneItemRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().NoneItems.Add(new NoneItem(requestParams.Path)), requestContext);
        }

        internal async Task HandleDeleteNoneItemRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().NoneItems.Delete(requestParams.Path), requestContext);
        }

        internal async Task HandleExcludeNoneItemRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().NoneItems.Exclude(requestParams.Path), requestContext);
        }

        internal async Task HandleMoveNoneItemRequest(MoveItemParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().NoneItems.Move(requestParams.Path, requestParams.DestinationPath), requestContext);
        }

        #endregion

        #region Folder functions

        internal async Task HandleAddFolderRequest(FolderParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().Folders.Add(new Folder(requestParams.Path)), requestContext);
        }

        internal async Task HandleDeleteFolderRequest(FolderParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().Folders.Delete(requestParams.Path), requestContext);
        }

        internal async Task HandleExcludeFolderRequest(FolderParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().Folders.Exclude(requestParams.Path), requestContext);
        }

        internal async Task HandleMoveFolderRequest(MoveFolderParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject().Folders.Move(requestParams.Path, requestParams.DestinationPath), requestContext);
        }

        #endregion

        #endregion

        #region Database reference functions

        internal async Task HandleGetDatabaseReferencesRequest(SqlProjectParams requestParams, RequestContext<GetDatabaseReferencesResult> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => BuildDatabaseReferencesResult(host.GetProject(onlyLoadProperties: true)), requestContext);
        }

        internal async Task HandleAddSystemDatabaseReferenceRequest(AddSystemDatabaseReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject(onlyLoadProperties: true).DatabaseReferences.Add(
                new SystemDatabaseReference(
                    requestParams.SystemDatabase,
                    requestParams.SuppressMissingDependencies,
                    requestParams.DatabaseLiteral,
                    requestParams.ReferenceType)),
                requestContext);
            OnDatabaseReferencesChanged(requestParams.ProjectUri);
        }

        internal async Task HandleAddDacpacReferenceRequest(AddDacpacReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host =>
            {
                requestParams.Validate();

                SqlProject project = host.GetProject(onlyLoadProperties: true);
                DacpacReference reference;

                if (!string.IsNullOrWhiteSpace(requestParams.DatabaseLiteral)) // same server, different database via database name literal
                {
                    reference = new DacpacReference(
                        requestParams.DacpacPath,
                        requestParams.SuppressMissingDependencies,
                        requestParams.DatabaseLiteral);
                }
                else if (!string.IsNullOrWhiteSpace(requestParams.DatabaseVariable)) // different database, possibly different server via sqlcmdvar
                {
                    reference = new DacpacReference(
                        requestParams.DacpacPath,
                        requestParams.SuppressMissingDependencies,
                        project.SqlCmdVariables.Get(requestParams.DatabaseVariable),
                        requestParams.ServerVariable != null ? project.SqlCmdVariables.Get(requestParams.ServerVariable) : null);
                }
                else // same database
                {
                    reference = new DacpacReference(requestParams.DacpacPath, requestParams.SuppressMissingDependencies);
                }

                project.DatabaseReferences.Add(reference);
            }, requestContext);
            OnDatabaseReferencesChanged(requestParams.ProjectUri);
        }

        internal async Task HandleAddSqlProjectReferenceRequest(AddSqlProjectReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host =>
            {
                requestParams.Validate();

                SqlProject project = host.GetProject(onlyLoadProperties: true);
                SqlProjectReference reference;

                if (!string.IsNullOrWhiteSpace(requestParams.DatabaseLiteral)) // same server, different database via database name literal
                {
                    reference = new SqlProjectReference(
                        requestParams.ProjectPath,
                        requestParams.ProjectGuid,
                        requestParams.SuppressMissingDependencies,
                        requestParams.DatabaseLiteral);
                }
                else if (!string.IsNullOrWhiteSpace(requestParams.DatabaseVariable)) // different database, possibly different server via sqlcmdvar
                {
                    reference = new SqlProjectReference(
                        requestParams.ProjectPath,
                        requestParams.ProjectGuid, requestParams.SuppressMissingDependencies,
                        project.SqlCmdVariables.Get(requestParams.DatabaseVariable),
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
            OnDatabaseReferencesChanged(requestParams.ProjectUri);
        }

        internal async Task HandleAddNugetPackageReferenceRequest(AddNugetPackageReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host =>
            {
                requestParams.Validate();

                SqlProject project = host.GetProject(onlyLoadProperties: true);
                NugetPackageReference reference;

                if (!string.IsNullOrWhiteSpace(requestParams.DatabaseLiteral)) // same server, different database via database name literal
                {
                    reference = new NugetPackageReference(
                        requestParams.PackageName,
                        requestParams.PackageVersion,
                        requestParams.SuppressMissingDependencies,
                        requestParams.DatabaseLiteral);
                }
                else if (!string.IsNullOrWhiteSpace(requestParams.DatabaseVariable)) // different database, possibly different server via sqlcmdvar
                {
                    reference = new NugetPackageReference(
                        requestParams.PackageName,
                        requestParams.PackageVersion,
                        requestParams.SuppressMissingDependencies,
                        project.SqlCmdVariables.Get(requestParams.DatabaseVariable),
                        requestParams.ServerVariable != null ? project.SqlCmdVariables.Get(requestParams.ServerVariable) : null);
                }
                else // same database
                {
                    reference = new NugetPackageReference(requestParams.PackageName, requestParams.PackageVersion, requestParams.SuppressMissingDependencies);
                }

                project.DatabaseReferences.Add(reference);
            }, requestContext);
            OnDatabaseReferencesChanged(requestParams.ProjectUri);
        }


        internal async Task HandleDeleteDatabaseReferenceRequest(DeleteDatabaseReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject(onlyLoadProperties: true).DatabaseReferences.Delete(requestParams.Name), requestContext);
            OnDatabaseReferencesChanged(requestParams.ProjectUri);
        }

        #endregion

        #region SQLCMD variable functions

        internal async Task HandleGetSqlCmdVariablesRequest(SqlProjectParams requestParams, RequestContext<GetSqlCmdVariablesResult> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host =>
            {
                return new GetSqlCmdVariablesResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    SqlCmdVariables = host.GetProject(onlyLoadProperties: true).SqlCmdVariables.ToArray()
                };
            }, requestContext);
        }

        internal async Task HandleAddSqlCmdVariableRequest(AddSqlCmdVariableParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject(onlyLoadProperties: true).SqlCmdVariables.Add(new SqlCmdVariable(requestParams.Name, requestParams.DefaultValue)), requestContext);
            OnDatabaseReferencesChanged(requestParams.ProjectUri);
        }

        internal async Task HandleDeleteSqlCmdVariableRequest(DeleteSqlCmdVariableParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host => host.GetProject(onlyLoadProperties: true).SqlCmdVariables.Delete(requestParams.Name!), requestContext);
            OnDatabaseReferencesChanged(requestParams.ProjectUri);
        }

        internal async Task HandleUpdateSqlCmdVariableRequest(AddSqlCmdVariableParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithProject(requestParams.ProjectUri, host =>
            {
                SqlProject project = host.GetProject(onlyLoadProperties: true);
                project.SqlCmdVariables.Update(requestParams.Name, requestParams.DefaultValue); // won't throw if doesn't exist
            }, requestContext);
            OnDatabaseReferencesChanged(requestParams.ProjectUri);
        }

        #endregion

        #endregion

        #region Helper methods

        /// <summary>
        /// Runs <paramref name="operation"/> on the project's host, creating the host if needed. If the host
        /// closes before the operation runs, retries on the new host that replaces it.
        /// </summary>
        private async Task<T> RunOnProjectAsync<T>(string projectUri, Func<ProjectHost, Task<T>> operation)
        {
            while (true)
            {
                ProjectHost host = hosts.GetOrAdd(projectUri, uri => new ProjectHost(
                    uri,
                    snapshot => IntelliSenseModelBuilder(snapshot),
                    (project, projectFilePath) => ResolveProjectReferences(project, projectFilePath)));
                try
                {
                    return await host.RunAsync(() => operation(host)).ConfigureAwait(false);
                }
                catch (ProjectHostClosedException)
                {
                    // Closed after we found it. Close removes the host; remove it here too so a closed host
                    // can never stay in the dictionary, and the next GetOrAdd creates a new one.
                    hosts.TryRemove(new KeyValuePair<string, ProjectHost>(projectUri, host));
                }
            }
        }

        private Task<T> WithProjectAsync<T>(string projectUri, Func<ProjectHost, T> operation)
            => RunOnProjectAsync(projectUri, host => Task.FromResult(operation(host)));

        private Task WithProjectAsync(string projectUri, Action<ProjectHost> operation)
            => WithProjectAsync(projectUri, host =>
            {
                operation(host);
                return true;
            });

        private Task RunWithProject(string projectUri, Action<ProjectHost> operation, RequestContext<ResultStatus> requestContext)
            => RunWithErrorHandling(() => WithProjectAsync(projectUri, operation), requestContext);

        private Task RunWithProject<T>(string projectUri, Func<ProjectHost, T> operation, RequestContext<T> requestContext) where T : ResultStatus, new()
            => RunWithErrorHandling(() => WithProjectAsync(projectUri, operation), requestContext);

        private static GetProjectPropertiesResult BuildProjectPropertiesResult(SqlProject project)
        {
            return new GetProjectPropertiesResult()
            {
                Success = true,
                ErrorMessage = null,
                ProjectGuid = project.Properties.ProjectGuid,
                Configuration = project.Properties.Configuration,
                Platform = project.Properties.Platform,
                OutputPath = project.Properties.OutputPath,
                DefaultCollation = project.Properties.DefaultCollation,
                DatabaseSource = project.Properties.DatabaseSource,
                ProjectStyle = project.SqlProjStyle,
                DatabaseSchemaProvider = project.Properties.DatabaseSchemaProvider,
                RunSqlCodeAnalysis = bool.TryParse(project.Properties.GetProperty(RunSqlCodeAnalysisPropertyName), out var runAnalysis) && runAnalysis,
                SqlCodeAnalysisRules = project.Properties.GetProperty(SqlCodeAnalysisRulesPropertyName)
            };
        }

        private static GetDatabaseReferencesResult BuildDatabaseReferencesResult(SqlProject project)
        {
            return new GetDatabaseReferencesResult()
            {
                Success = true,
                ErrorMessage = null,
                SystemDatabaseReferences = project.DatabaseReferences.OfType<SystemDatabaseReference>().ToArray(),
                DacpacReferences = project.DatabaseReferences.OfType<DacpacReference>().ToArray(),
                SqlProjectReferences = project.DatabaseReferences.OfType<SqlProjectReference>().ToArray(),
                NugetPackageReferences = project.DatabaseReferences.OfType<NugetPackageReference>().ToArray()
            };
        }

        /// <summary>
        /// MSBuild item globs can contain *, ?, or character classes like [abc]
        /// </summary>
        private static readonly SearchValues<char> GlobCharacters = SearchValues.Create("*?[");

        private static bool IsGlobPattern(string path) => path.AsSpan().ContainsAny(GlobCharacters);

        /// <summary>
        /// Top-level files only, including hidden ones, matching .sqlproj in any case (e.g. Db.SQLPROJ)
        /// even on case-sensitive file systems.
        /// </summary>
        private static readonly EnumerationOptions ProjectFileEnumerationOptions = new()
        {
            RecurseSubdirectories = false,
            MatchCasing = MatchCasing.CaseInsensitive,
            MatchType = MatchType.Win32,
            AttributesToSkip = 0,
            IgnoreInaccessible = false,
        };

        /// <summary>
        /// Returns the .sqlproj that owns <paramref name="filePath"/>: the nearest one in the file's folder
        /// or any parent folder. Only the ancestor folders are read, never the whole workspace.
        /// Returns null for anything other than a .sql file, and for files outside any project.
        /// </summary>
        internal static string? FindProjectForFile(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)
                || !Path.IsPathFullyQualified(filePath)
                || !string.Equals(Path.GetExtension(filePath), ".sql", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string? directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
            while (directory != null)
            {
                try
                {
                    // Order so the result is stable when a folder holds more than one project
                    string? projectPath = Directory.EnumerateFiles(directory, "*.sqlproj", ProjectFileEnumerationOptions)
                        .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault();

                    if (projectPath != null)
                    {
                        return projectPath;
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    // Missing or unreadable folder; keep walking up
                }

                directory = Path.GetDirectoryName(directory);
            }

            return null;
        }

        /// <summary>
        /// Directly writes or updates a property element in the first &lt;PropertyGroup&gt; of the
        /// .sqlproj XML file. Used for properties that DacFx exposes as init-only fields with no
        /// public setter (e.g. <c>ProjectGuid</c>).
        /// </summary>
        private static void SetReadOnlyPropertyInXml(string projectUri, string propertyName, string propertyValue)
        {
            XDocument doc = XDocument.Load(projectUri);
            XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            XElement? propertyGroup = doc.Root?.Elements(ns + "PropertyGroup").FirstOrDefault();
            if (propertyGroup == null)
            {
                propertyGroup = new XElement(ns + "PropertyGroup");
                doc.Root?.AddFirst(propertyGroup);
            }

            XElement? existing = propertyGroup.Element(ns + propertyName);
            if (existing != null)
            {
                existing.Value = propertyValue;
            }
            else
            {
                propertyGroup.Add(new XElement(ns + propertyName, propertyValue));
            }

            doc.Save(projectUri);
        }

        #endregion
    }
}
