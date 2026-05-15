//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.SqlCore.IntelliSense;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects
{
    /// <summary>
    /// Main class for SqlProjects service
    /// </summary>
    public sealed class SqlProjectsService : BaseService
    {
        private static readonly Lazy<SqlProjectsService> instance = new Lazy<SqlProjectsService>(() => new SqlProjectsService());
        private const string RunSqlCodeAnalysisPropertyName = "RunSqlCodeAnalysis";
        private const string SqlCodeAnalysisRulesPropertyName = "SqlCodeAnalysisRules";
        private const string ProjectGuidPropertyName = "ProjectGuid";

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
        /// Maps project URI to its IntelliSense state for offline IntelliSense.
        /// On close: Model must be disposed; binding context and ScriptParseInfo entries
        /// must be removed using ContextKey and FileUris.
        /// </summary>
        private ConcurrentDictionary<string, (TSqlModel Model, TSqlModelMetadataProvider Provider, string ContextKey, string DatabaseName, IReadOnlyList<string> FileUris, ParseOptions ParseOptions)> projectIntelliSense = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Monotonically-increasing generation counter per project URI.
        /// Incremented on every Open and Close. A background build task captures the generation
        /// at start and checks it at each commit point; if the generation has changed the task
        /// knows it is no longer the owner and must discard its results.
        /// </summary>
        private ConcurrentDictionary<string, int> projectGenerations = new(StringComparer.OrdinalIgnoreCase);

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
            serviceHost.SetRequestHandler(SetDatabaseSourceRequest.Type, HandleSetDatabaseSourceRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(SetDatabaseSchemaProviderRequest.Type, HandleSetDatabaseSchemaProviderRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(SetProjectPropertiesRequest.Type, HandleSetProjectPropertiesRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(UpdateCodeAnalysisRulesRequest.Type, HandleUpdateCodeAnalysisRulesRequest, isParallelProcessingSupported: false);

            // SQL object script functions
            serviceHost.SetRequestHandler(GetSqlObjectScriptsRequest.Type, HandleGetSqlObjectScriptsRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(AddSqlObjectScriptRequest.Type, HandleAddSqlObjectScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeleteSqlObjectScriptRequest.Type, HandleDeleteSqlObjectScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(ExcludeSqlObjectScriptRequest.Type, HandleExcludeSqlObjectScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(MoveSqlObjectScriptRequest.Type, HandleMoveSqlObjectScriptRequest, isParallelProcessingSupported: false);

            // Pre/Post-deployment script functions
            serviceHost.SetRequestHandler(GetPreDeploymentScriptsRequest.Type, HandleGetPreDeploymentScriptsRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(AddPreDeploymentScriptRequest.Type, HandleAddPreDeploymentScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeletePreDeploymentScriptRequest.Type, HandleDeletePreDeploymentScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(ExcludePreDeploymentScriptRequest.Type, HandleExcludePreDeploymentScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(MovePreDeploymentScriptRequest.Type, HandleMovePreDeploymentScriptRequest, isParallelProcessingSupported: false);

            serviceHost.SetRequestHandler(GetPostDeploymentScriptsRequest.Type, HandleGetPostDeploymentScriptsRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(AddPostDeploymentScriptRequest.Type, HandleAddPostDeploymentScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeletePostDeploymentScriptRequest.Type, HandleDeletePostDeploymentScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(ExcludePostDeploymentScriptRequest.Type, HandleExcludePostDeploymentScriptRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(MovePostDeploymentScriptRequest.Type, HandleMovePostDeploymentScriptRequest, isParallelProcessingSupported: false);

            // None script functions
            serviceHost.SetRequestHandler(GetNoneItemsRequest.Type, HandleGetNoneItemsRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(AddNoneItemRequest.Type, HandleAddNoneItemRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(DeleteNoneItemRequest.Type, HandleDeleteNoneItemRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(ExcludeNoneItemRequest.Type, HandleExcludeNoneItemRequest, isParallelProcessingSupported: false);
            serviceHost.SetRequestHandler(MoveNoneItemRequest.Type, HandleMoveNoneItemRequest, isParallelProcessingSupported: false);

            // Folder functions
            serviceHost.SetRequestHandler(GetFoldersRequest.Type, HandleGetFoldersRequest, isParallelProcessingSupported: true);
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
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri), requestContext);
            // Bump the generation so any previously in-flight build for this URI is invalidated,
            // then capture the new generation into the background task as its ownership token.
            int generation = projectGenerations.AddOrUpdate(
                requestParams.ProjectUri, 1, (_, prev) => prev + 1);
            // Kick off async IntelliSense model build so .sql files in this project get completions
            // without a live server connection. Fire-and-forget: errors are logged inside.
            _ = Task.Run(() => BuildProjectIntelliSenseAsync(requestParams.ProjectUri, generation));
        }

        internal async Task HandleCloseSqlProjectRequest(SqlProjectParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                Projects.TryRemove(requestParams.ProjectUri, out _);

                // Bump the generation to invalidate any in-flight IntelliSense build.
                // The background task checks this at each commit point and will discard
                // its model if it sees the generation has changed.
                projectGenerations.AddOrUpdate(
                    requestParams.ProjectUri, 1, (_, prev) => prev + 1);

                // Full IntelliSense teardown:
                // 1. Remove binding context from the queue (releases MetadataProvider + _sourceLocations)
                // 2. Remove ScriptParseInfo for all .sql files and the .sqlproj itself
                // 3. Dispose TSqlModel to free DacFx unmanaged resources
                if (projectIntelliSense.TryRemove(requestParams.ProjectUri, out var intelliSense))
                {
                    LanguageService.Instance.TearDownProjectContext(
                        requestParams.ProjectUri,
                        intelliSense.ContextKey,
                        intelliSense.FileUris);
                    intelliSense.Model?.Dispose();
                }
            }, requestContext);
        }

        /// <summary>
        /// Builds the TSqlModel for a project and creates a MetadataProvider for offline IntelliSense.
        /// Stores both in projectIntelliSense cache for disposal on project close.
        /// Runs on a background thread; errors do not affect the project open response.
        /// </summary>
        /// <param name="projectUri">URI of the project being opened.</param>
        /// <param name="generation">Ownership token captured at the moment the task was started.
        /// If the current generation for this URI differs at any commit point, the task is stale
        /// (project was closed or re-opened) and must discard its results.</param>
        private async Task BuildProjectIntelliSenseAsync(string projectUri, int generation)
        {
            TSqlModel? model = null;
            try
            {
                SqlProject project = GetProject(projectUri);

                string databaseName = Path.GetFileNameWithoutExtension(projectUri);
                string contextKey = $"project_{projectUri}";
                string projectDir = Path.GetDirectoryName(new Uri(projectUri).LocalPath)
                    ?? throw new InvalidOperationException($"Cannot determine project directory from URI: {projectUri}");

                // Include all SQL files: Build items, PreDeploy, and PostDeploy
                var allScripts = new List<string>();
                foreach (var script in project.SqlObjectScripts)
                {
                    allScripts.Add(script.Path);
                }
                foreach (var script in project.PreDeployScripts)
                {
                    allScripts.Add(script.Path);
                }
                foreach (var script in project.PostDeployScripts)
                {
                    allScripts.Add(script.Path);
                }

                var fileUriList = allScripts
                    .Select(path => new Uri(Path.IsPathRooted(path)
                        ? path
                        : Path.Combine(projectDir, path)).AbsoluteUri)
                    .ToList();

                model = await Task.Run(() => TSqlModelBuilder.LoadModel(project));

                // Gate 1: after the expensive load — verify we are still the owner.
                if (!IsCurrentGeneration(projectUri, generation))
                {
                    model.Dispose();
                    return;
                }

                var projectMetadataProvider = new TSqlModelMetadataProvider(model, databaseName);

                var parseOptions = new ParseOptions(
                    batchSeparator: LanguageService.DefaultBatchSeperator,
                    isQuotedIdentifierSet: true,
                    compatibilityLevel: DatabaseCompatibilityLevel.Current,
                    transactSqlVersion: TransactSqlVersion.Current);

                // Store everything needed for full teardown on project close.
                projectIntelliSense[projectUri] = (model, projectMetadataProvider, contextKey, databaseName, fileUriList, parseOptions);

                // Gate 2: before registering the binding context — verify we are still the owner.
                // (Close may have run between Gate 1 and here.)
                if (!IsCurrentGeneration(projectUri, generation))
                {
                    projectIntelliSense.TryRemove(projectUri, out _);
                    model.Dispose();
                    return;
                }

                await LanguageService.Instance.UpdateLanguageServiceOnProjectOpen(
                    projectUri, projectMetadataProvider, parseOptions, databaseName, fileUriList);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to build IntelliSense model for project {projectUri}: {ex}");
                model?.Dispose();
            }
        }

        /// <summary>
        /// Returns true when <paramref name="generation"/> still matches the current generation
        /// for the given project URI, meaning no Open or Close has superseded this build task.
        /// </summary>
        private bool IsCurrentGeneration(string projectUri, int generation)
            => projectGenerations.TryGetValue(projectUri, out int current) && current == generation;

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

                await SqlProject.CreateProjectAsync(requestParams.ProjectUri, createParams);
                this.GetProject(requestParams.ProjectUri); // load into the cache
            }, requestContext);
        }

        internal async Task HandleGetCrossPlatformCompatibilityRequest(SqlProjectParams requestParams, RequestContext<GetCrossPlatformCompatibilityResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                return new GetCrossPlatformCompatibilityResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    IsCrossPlatformCompatible = GetProject(requestParams.ProjectUri, onlyLoadProperties: true).CrossPlatformCompatible
                };
            }, requestContext);
        }

        internal async Task HandleUpdateProjectForCrossPlatformRequest(SqlProjectParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri, onlyLoadProperties: true).UpdateForCrossPlatform(), requestContext);
        }

        internal async Task HandleGetProjectPropertiesRequest(SqlProjectParams requestParams, RequestContext<GetProjectPropertiesResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                SqlProject project = GetProject(requestParams.ProjectUri, onlyLoadProperties: true);

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
            }, requestContext);
        }

        internal async Task HandleSetDatabaseSourceRequest(SetDatabaseSourceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri, onlyLoadProperties: true).Properties.DatabaseSource = requestParams.DatabaseSource, requestContext);
        }

        internal async Task HandleSetDatabaseSchemaProviderRequest(SetDatabaseSchemaProviderParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri, onlyLoadProperties: true).Properties.TargetSqlPlatform = Utilities.DatabaseSchemaProviderToSqlPlatform(requestParams.DatabaseSchemaProvider), requestContext);
        }

        internal async Task HandleSetProjectPropertiesRequest(SetProjectPropertiesParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                SqlProject project = GetProject(requestParams.ProjectUri, onlyLoadProperties: true);

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
                        Projects.TryRemove(requestParams.ProjectUri, out _);
                    }
                }
            }, requestContext);
        }

        internal async Task HandleUpdateCodeAnalysisRulesRequest(UpdateCodeAnalysisRulesParams requestParams, RequestContext<UpdateCodeAnalysisRulesResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                SqlProject project = GetProject(requestParams.ProjectUri, onlyLoadProperties: true);

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

        internal async Task HandleGetSqlObjectScriptsRequest(SqlProjectParams requestParams, RequestContext<GetScriptsResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                return new GetScriptsResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Scripts = GetProject(requestParams.ProjectUri).SqlObjectScripts.Select(x => x.Path).ToArray()
                };
            }, requestContext);
        }

        internal async Task HandleAddSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(async () =>
            {
                SqlProject project = GetProject(requestParams.ProjectUri);
                project.SqlObjectScripts.Add(new SqlObjectScript(requestParams.Path));
                // Incrementally update the IntelliSense model for the new file.
                await UpdateProjectIntelliSenseAsync(requestParams.ProjectUri, requestParams.Path, deleted: false);
            }, requestContext);
        }

        internal async Task HandleDeleteSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(async () =>
            {
                SqlProject project = GetProject(requestParams.ProjectUri);
                project.SqlObjectScripts.Delete(requestParams.Path);
                // Incrementally remove the deleted file's objects from the IntelliSense model.
                await UpdateProjectIntelliSenseAsync(requestParams.ProjectUri, requestParams.Path, deleted: true);
            }, requestContext);
        }

        internal async Task HandleExcludeSqlObjectScriptRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(async () =>
            {
                GetProject(requestParams.ProjectUri).SqlObjectScripts.Exclude(requestParams.Path);
                // Remove the excluded file's objects from the IntelliSense model.
                await UpdateProjectIntelliSenseAsync(requestParams.ProjectUri, requestParams.Path, deleted: true);
            }, requestContext);
        }

        internal async Task HandleMoveSqlObjectScriptRequest(MoveItemParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(async () =>
            {
                GetProject(requestParams.ProjectUri).SqlObjectScripts.Move(requestParams.Path, requestParams.DestinationPath);
                // The IntelliSense model is path-keyed, so a rename is a delete + add:
                // (1) Purge the old path's objects from the model and source location index.
                await UpdateProjectIntelliSenseAsync(requestParams.ProjectUri, requestParams.Path, deleted: true);
                // (2) Read the file at its new path and re-register its objects under the new key.
                await UpdateProjectIntelliSenseAsync(requestParams.ProjectUri, requestParams.DestinationPath, deleted: false);
            }, requestContext);
        }

        internal async Task UpdateProjectIntelliSenseAsync(string projectUri, string filePathOrUri, bool deleted)
        {
            if (!projectIntelliSense.TryGetValue(projectUri, out var state)) return;
            try
            {
                string sourceName = GetAbsoluteFilePath(projectUri, filePathOrUri);
                if (!deleted)
                {
                    if (!File.Exists(sourceName)) return;
                    string sqlText = await File.ReadAllTextAsync(sourceName).ConfigureAwait(false);
                    if (!projectIntelliSense.ContainsKey(projectUri)) return; // closed during await
                    state.Model.AddOrUpdateObjects(sqlText, sourceName, new TSqlObjectOptions());
                }
                else
                {
                    if (!projectIntelliSense.ContainsKey(projectUri)) return;
                    state.Model.DeleteObjects(sourceName);
                }
                state.Provider.UpdateForFileChange(sourceName, deleted);

                // The binder built at project-open time holds a snapshot of the metadata.
                // After mutating the provider, recreate the binder so alias resolution (e.g.
                // "p." after "FROM sss.packages p") and object enumeration ("sss.") pick up
                // the updated schema.
                var newBinder = Microsoft.SqlServer.Management.SqlParser.Binder.BinderProvider.CreateBinder(state.Provider);
                LanguageService.Instance.BindingQueue.AddProjectContext(state.ContextKey, newBinder, state.ParseOptions, state.Provider);

                // Stamp the file URI with the project context so IntelliSense works when the
                // user opens the file. For deletes the file is gone so nothing to stamp.
                if (!deleted)
                {
                    string fileUri = new Uri(sourceName).AbsoluteUri;
                    LanguageService.Instance.InitializeProjectFileContexts(
                        new[] { fileUri }, state.ContextKey, state.DatabaseName);
                }
            }
            catch (Exception ex) { Logger.Error($"UpdateProjectIntelliSenseAsync error for {filePathOrUri}: {ex}"); }
        }

        private static string GetAbsoluteFilePath(string projectUri, string filePathOrUri)
        {
            // Handle file:// URIs from LSP (e.g. "file:///c:/Users/..." or "file:///home/...")
            if (Uri.TryCreate(filePathOrUri, UriKind.Absolute, out Uri? parsedUri) && parsedUri.IsFile)
            {
                // Uri.LocalPath gives the OS-native path. On Windows this is normally "C:\Users\..."
                // but some .NET versions return "/c:/Users/..." with a spurious leading slash.
                // Detect that case (letter followed by colon after the slash) and skip the slash.
                string localPath = parsedUri.LocalPath;
                int start = (localPath.Length >= 3 && localPath[0] == '/' &&
                             char.IsLetter(localPath[1]) && localPath[2] == ':') ? 1 : 0;
                return Path.GetFullPath(localPath.Substring(start));
            }

            // Already an absolute OS path — normalise separators/casing via Path.GetFullPath.
            if (Path.IsPathRooted(filePathOrUri))
                return Path.GetFullPath(filePathOrUri);

            // Relative path — resolve against the project directory.
            string projectDir = Path.GetDirectoryName(new Uri(projectUri).LocalPath) ?? string.Empty;
            return Path.GetFullPath(Path.Combine(projectDir, filePathOrUri));
        }

        #endregion

        #region Pre/Post-deployment script functions

        internal async Task HandleGetPreDeploymentScriptsRequest(SqlProjectParams requestParams, RequestContext<GetScriptsResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                return new GetScriptsResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Scripts = GetProject(requestParams.ProjectUri).PreDeployScripts.Select(x => x.Path).ToArray()
                };
            }, requestContext);
        }

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

        internal async Task HandleGetPostDeploymentScriptsRequest(SqlProjectParams requestParams, RequestContext<GetScriptsResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                return new GetScriptsResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Scripts = GetProject(requestParams.ProjectUri).PostDeployScripts.Select(x => x.Path).ToArray()
                };
            }, requestContext);
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

        #region None script functions

        internal async Task HandleGetNoneItemsRequest(SqlProjectParams requestParams, RequestContext<GetScriptsResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                return new GetScriptsResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Scripts = GetProject(requestParams.ProjectUri).NoneItems.Select(x => x.Path).ToArray()
                };
            }, requestContext);
        }

        internal async Task HandleAddNoneItemRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).NoneItems.Add(new NoneItem(requestParams.Path)), requestContext);
        }

        internal async Task HandleDeleteNoneItemRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).NoneItems.Delete(requestParams.Path), requestContext);
        }

        internal async Task HandleExcludeNoneItemRequest(SqlProjectScriptParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).NoneItems.Exclude(requestParams.Path), requestContext);
        }

        internal async Task HandleMoveNoneItemRequest(MoveItemParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).NoneItems.Move(requestParams.Path, requestParams.DestinationPath), requestContext);
        }

        #endregion

        #region Folder functions

        internal async Task HandleGetFoldersRequest(SqlProjectParams requestParams, RequestContext<GetFoldersResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                return new GetFoldersResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Folders = GetProject(requestParams.ProjectUri).Folders.Select(x => x.Path).ToArray()
                };
            }, requestContext);
        }

        internal async Task HandleAddFolderRequest(FolderParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).Folders.Add(new Folder(requestParams.Path)), requestContext);
        }

        internal async Task HandleDeleteFolderRequest(FolderParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).Folders.Delete(requestParams.Path), requestContext);
        }

        internal async Task HandleExcludeFolderRequest(FolderParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).Folders.Exclude(requestParams.Path), requestContext);
        }

        internal async Task HandleMoveFolderRequest(MoveFolderParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri).Folders.Move(requestParams.Path, requestParams.DestinationPath), requestContext);
        }

        #endregion

        #endregion

        #region Database reference functions

        internal async Task HandleGetDatabaseReferencesRequest(SqlProjectParams requestParams, RequestContext<GetDatabaseReferencesResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                SqlProject project = GetProject(requestParams.ProjectUri, onlyLoadProperties: true);

                return new GetDatabaseReferencesResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    SystemDatabaseReferences = project.DatabaseReferences.OfType<SystemDatabaseReference>().ToArray(),
                    DacpacReferences = project.DatabaseReferences.OfType<DacpacReference>().ToArray(),
                    SqlProjectReferences = project.DatabaseReferences.OfType<SqlProjectReference>().ToArray(),
                    NugetPackageReferences = project.DatabaseReferences.OfType<NugetPackageReference>().ToArray()
                };
            }, requestContext);
        }

        internal async Task HandleAddSystemDatabaseReferenceRequest(AddSystemDatabaseReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri, onlyLoadProperties: true).DatabaseReferences.Add(
                new SystemDatabaseReference(
                    requestParams.SystemDatabase,
                    requestParams.SuppressMissingDependencies,
                    requestParams.DatabaseLiteral,
                    requestParams.ReferenceType)),
                requestContext);
        }

        internal async Task HandleAddDacpacReferenceRequest(AddDacpacReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                requestParams.Validate();

                SqlProject project = GetProject(requestParams.ProjectUri, onlyLoadProperties: true);
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
        }

        internal async Task HandleAddSqlProjectReferenceRequest(AddSqlProjectReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                requestParams.Validate();

                SqlProject project = GetProject(requestParams.ProjectUri, onlyLoadProperties: true);
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
        }

        internal async Task HandleAddNugetPackageReferenceRequest(AddNugetPackageReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                requestParams.Validate();

                SqlProject project = GetProject(requestParams.ProjectUri, onlyLoadProperties: true);
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
        }


        internal async Task HandleDeleteDatabaseReferenceRequest(DeleteDatabaseReferenceParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri, onlyLoadProperties: true).DatabaseReferences.Delete(requestParams.Name), requestContext);
        }

        #endregion

        #region SQLCMD variable functions

        internal async Task HandleGetSqlCmdVariablesRequest(SqlProjectParams requestParams, RequestContext<GetSqlCmdVariablesResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                return new GetSqlCmdVariablesResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    SqlCmdVariables = GetProject(requestParams.ProjectUri, onlyLoadProperties: true).SqlCmdVariables.ToArray()
                };
            }, requestContext);
        }

        internal async Task HandleAddSqlCmdVariableRequest(AddSqlCmdVariableParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri, onlyLoadProperties: true).SqlCmdVariables.Add(new SqlCmdVariable(requestParams.Name, requestParams.DefaultValue)), requestContext);
        }

        internal async Task HandleDeleteSqlCmdVariableRequest(DeleteSqlCmdVariableParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() => GetProject(requestParams.ProjectUri, onlyLoadProperties: true).SqlCmdVariables.Delete(requestParams.Name!), requestContext);
        }

        internal async Task HandleUpdateSqlCmdVariableRequest(AddSqlCmdVariableParams requestParams, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                SqlProject project = GetProject(requestParams.ProjectUri, onlyLoadProperties: true);
                project.SqlCmdVariables.Update(requestParams.Name, requestParams.DefaultValue); // won't throw if doesn't exist
            }, requestContext);
        }

        #endregion

        #endregion

        #region Helper methods

        private SqlProject GetProject(string projectUri, bool onlyLoadProperties = false)
        {
            if (!Projects.ContainsKey(projectUri) // if not already loaded, load according to onlyLoadProperties flag
                || (Projects[projectUri].OnlyPropertiesLoaded && !onlyLoadProperties)) // if already loaded, check flag to see if it needs to be reopened as fully-loaded
            {
                Projects[projectUri] = SqlProject.OpenProject(projectUri, onlyLoadProperties);
            }

            return Projects[projectUri];
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
