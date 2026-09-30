//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using Microsoft.SqlTools.LanguageService.Workspace.Contracts;
using Microsoft.SqlTools.SqlCore.IntelliSense;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects
{
    /// <summary>
    /// Owns everything the service holds for one project: the loaded <see cref="SqlProject"/>, its
    /// IntelliSense model, and the changes made while that model builds. Every operation on the project
    /// runs through <see cref="RunAsync{T}"/>, one at a time, so these always change together.
    /// </summary>
    /// <remarks>
    /// Building the IntelliSense model is the only work that runs outside the queue. It takes a private
    /// snapshot of the project through the queue, builds from that, and publishes the result back through
    /// the queue. Changes made in the meantime are collected and replayed into the model when it's published.
    /// </remarks>
    internal sealed class ProjectHost
    {
        private readonly SemaphoreSlim queue = new(1, 1);

        private SqlProject? project;
        private ProjectIntelliSense? intelliSense;

        /// <summary>
        /// Non-null while an IntelliSense build is in flight: the changes to replay into its model.
        /// </summary>
        private List<IntelliSenseChange>? changesDuringBuild;

        /// <summary>
        /// Identifies the in-flight IntelliSense build. Close bumps it so a late build discards its result.
        /// </summary>
        private int buildId;

        private bool closed;

        private readonly Func<SqlProject, TSqlModel> buildModel;

        /// <param name="projectUri">Path of the .sqlproj</param>
        /// <param name="buildModel">Builds the IntelliSense model from a project snapshot</param>
        public ProjectHost(string projectUri, Func<SqlProject, TSqlModel> buildModel)
        {
            ProjectUri = projectUri;
            this.buildModel = buildModel;
        }

        public string ProjectUri { get; }

        /// <summary>
        /// The loaded project, if any. Read outside the queue only for diagnostics and tests.
        /// </summary>
        public SqlProject? LoadedProject => Volatile.Read(ref project);

        /// <summary>
        /// The published IntelliSense model, if any. Read outside the queue by the synchronous
        /// language service lookups.
        /// </summary>
        public ProjectIntelliSense? IntelliSense => Volatile.Read(ref intelliSense);

        /// <summary>
        /// The most recently started IntelliSense build, if any. Lets tests wait for it.
        /// </summary>
        internal Task? IntelliSenseBuild { get; private set; }

        /// <summary>
        /// Runs <paramref name="operation"/> on the thread pool once every earlier operation on this project
        /// has finished. Waiting doesn't block a thread. Operations must not call <see cref="RunAsync{T}"/>
        /// on the same host, since the queue isn't reentrant.
        /// </summary>
        /// <exception cref="ProjectHostClosedException">The project was closed before the operation ran.</exception>
        public async Task<T> RunAsync<T>(Func<Task<T>> operation)
        {
            await queue.WaitAsync().ConfigureAwait(false);
            try
            {
                if (closed)
                {
                    throw new ProjectHostClosedException(ProjectUri);
                }

                return await Task.Run(operation).ConfigureAwait(false);
            }
            finally
            {
                queue.Release();
            }
        }

        // The members below are only called from inside RunAsync.

        /// <summary>
        /// Returns the loaded project, loading it first if needed. A properties-only project is reloaded
        /// in full when <paramref name="onlyLoadProperties"/> is false.
        /// </summary>
        public SqlProject GetProject(bool onlyLoadProperties = false)
        {
            SqlProject? loaded = project;
            if (loaded == null || (loaded.OnlyPropertiesLoaded && !onlyLoadProperties))
            {
                loaded = SqlProject.OpenProject(ProjectUri, onlyLoadProperties);
                Volatile.Write(ref project, loaded);
            }

            return loaded;
        }

        /// <summary>
        /// Drops the loaded project so the next operation reloads it from disk. The IntelliSense model is kept.
        /// </summary>
        public void ForgetProject() => Volatile.Write(ref project, null);

        /// <summary>
        /// Starts building the IntelliSense model in the background, unless one is already published or
        /// being built. Incremental updates keep an existing model current; close tears it down, so closing
        /// and reopening rebuilds it from disk.
        /// </summary>
        public void EnsureIntelliSense()
        {
            if (intelliSense != null || changesDuringBuild != null)
            {
                return;
            }

            changesDuringBuild = new List<IntelliSenseChange>();
            int id = ++buildId;
            IntelliSenseBuild = Task.Run(() => BuildIntelliSenseAsync(id));
        }

        /// <summary>
        /// Applies a file change to the IntelliSense model. While the model is building, the change is
        /// collected and replayed once the model is published. Without a model, there's nothing to update.
        /// </summary>
        public async Task UpdateIntelliSenseAsync(string filePathOrUri, bool deleted, string? sqlTextOverride = null)
        {
            var change = new IntelliSenseChange(filePathOrUri, deleted, sqlTextOverride);
            if (intelliSense != null)
            {
                await ApplyChangeAsync(intelliSense, change).ConfigureAwait(false);
            }
            else
            {
                changesDuringBuild?.Add(change);
            }
        }

        /// <summary>
        /// Tears down the IntelliSense model, drops the loaded project, and rejects every later operation.
        /// </summary>
        public void Close()
        {
            closed = true;
            buildId++;
            changesDuringBuild = null;
            ForgetProject();

            ProjectIntelliSense? state = intelliSense;
            Volatile.Write(ref intelliSense, null);
            state?.TearDown(ProjectUri);
        }

        private async Task BuildIntelliSenseAsync(int id)
        {
            TSqlModel? model = null;
            bool published = false;
            try
            {
                // Take a private snapshot through the queue, so it matches the project as of this point.
                // Nothing else references the snapshot, so later edits can't change what the build reads;
                // they're collected in changesDuringBuild instead.
                SqlProject? snapshot = await RunAsync(() => Task.FromResult(
                    id == buildId ? SqlProject.OpenProject(ProjectUri) : null)).ConfigureAwait(false);
                if (snapshot == null)
                {
                    return;
                }

                model = buildModel(snapshot);
                ProjectIntelliSense state = ProjectIntelliSense.Create(ProjectUri, snapshot, model);

                published = await RunAsync(async () =>
                {
                    if (id != buildId || intelliSense != null)
                    {
                        return false;
                    }

                    List<IntelliSenseChange> changes = changesDuringBuild ?? new List<IntelliSenseChange>();
                    changesDuringBuild = null;

                    bool registered = await TSqlLanguageService.Instance.UpdateLanguageServiceOnProjectOpen(
                        ProjectUri, state.Provider, state.ParseOptions, state.DatabaseName, state.FileUris).ConfigureAwait(false);
                    if (!registered)
                    {
                        // Undo any partial registration. With no model and no build in flight, the next open retries.
                        TSqlLanguageService.Instance.TearDownProjectContext(ProjectUri, state.ContextKey, state.FileUris);
                        return false;
                    }

                    Volatile.Write(ref intelliSense, state);

                    // Replay the edits and saves made while the model was building, in order
                    foreach (IntelliSenseChange change in changes)
                    {
                        await ApplyChangeAsync(state, change).ConfigureAwait(false);
                    }

                    await ApplyUnsavedOpenFilesAsync(state).ConfigureAwait(false);
                    return true;
                }).ConfigureAwait(false);
            }
            catch (ProjectHostClosedException)
            {
                // Closed while building; nothing to publish
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to build IntelliSense model for project {ProjectUri}: {ex}");
                await AbandonBuildAsync(id).ConfigureAwait(false);
            }
            finally
            {
                // A published model belongs to the host, which disposes it on close
                if (!published && intelliSense?.Model != model)
                {
                    model?.Dispose();
                }
            }
        }

        /// <summary>
        /// Applies the unsaved text of the project's open files to a newly published model. The model is built
        /// from disk, and the language service only forwards edits for files already stamped as project files,
        /// so text typed before the model was published would otherwise be missing until the next edit.
        /// </summary>
        private async Task ApplyUnsavedOpenFilesAsync(ProjectIntelliSense state)
        {
            if (!TSqlLanguageService.Instance.TryGetOpenedFiles(out ScriptFile[] openedFiles) || openedFiles.Length == 0)
            {
                return;
            }

            HashSet<string> projectFiles;
            lock (state.FileUris)
            {
                projectFiles = new HashSet<string>(state.FileUris.Select(uri => GetAbsoluteFilePath(ProjectUri, uri)), StringComparer.OrdinalIgnoreCase);
            }

            foreach (ScriptFile openedFile in openedFiles)
            {
                if (!Uri.TryCreate(openedFile.ClientUri, UriKind.Absolute, out Uri? uri) || !uri.IsFile)
                {
                    continue; // untitled and other non-file documents can't belong to the project
                }

                string path = GetAbsoluteFilePath(ProjectUri, openedFile.ClientUri);
                string contents = openedFile.Contents;
                if (!projectFiles.Contains(path)
                    || (File.Exists(path) && string.Equals(await File.ReadAllTextAsync(path).ConfigureAwait(false), contents, StringComparison.Ordinal)))
                {
                    continue; // not in the project, or no unsaved changes
                }

                await ApplyChangeAsync(state, new IntelliSenseChange(openedFile.ClientUri, Deleted: false, SqlTextOverride: contents)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// After a failed build, stops collecting changes so the next open starts a new build.
        /// </summary>
        private async Task AbandonBuildAsync(int id)
        {
            try
            {
                await RunAsync(() =>
                {
                    if (id == buildId && intelliSense == null)
                    {
                        changesDuringBuild = null;
                    }
                    return Task.FromResult(true);
                }).ConfigureAwait(false);
            }
            catch (ProjectHostClosedException)
            {
                // Close already cleared the build state
            }
        }

        private async Task ApplyChangeAsync(ProjectIntelliSense state, IntelliSenseChange change)
        {
            try
            {
                string sourceName = GetAbsoluteFilePath(ProjectUri, change.FilePathOrUri);
                bool parseSucceeded = false;

                if (!change.Deleted)
                {
                    string? sqlText = change.SqlTextOverride ?? (File.Exists(sourceName)
                        ? await File.ReadAllTextAsync(sourceName).ConfigureAwait(false)
                        : null);
                    if (sqlText == null)
                    {
                        return;
                    }

                    try
                    {
                        state.Model.AddOrUpdateObjects(sqlText, sourceName, new TSqlObjectOptions { SkipExistingModelValidation = true });
                        parseSucceeded = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"UpdateProjectIntelliSenseAsync parse error for {sourceName}: {ex}");
                    }
                }
                else
                {
                    state.Model.DeleteObjects(sourceName, new TSqlObjectOptions { SkipExistingModelValidation = true });
                }

                // Update provider: deleted=true for actual deletes OR failed parses
                state.Provider.UpdateForFileChange(sourceName, change.Deleted || !parseSucceeded);

                // The binder built at project-open time holds a snapshot of the metadata.
                // After mutating the provider, recreate the binder so alias resolution (e.g.
                // "p." after "FROM sss.packages p") and object enumeration ("sss.") pick up
                // the updated schema.
                var newBinder = Microsoft.SqlServer.Management.SqlParser.Binder.BinderProvider.CreateBinder(state.Provider);
                TSqlLanguageService.Instance.BindingQueue.AddProjectContext(state.ContextKey, newBinder, state.ParseOptions, state.Provider);

                string fileUri = Utility.FileUtilities.LocalPathToFileUri(sourceName);
                if (!change.Deleted)
                {
                    // Stamp the file URI with the project context so IntelliSense works when the
                    // user opens the file. For deletes the file is gone so nothing to stamp.
                    lock (state.FileUris) { state.FileUris.Add(fileUri); }
                    TSqlLanguageService.Instance.InitializeProjectFileContexts(
                        new[] { fileUri }, state.ContextKey, state.DatabaseName);
                }
                else
                {
                    // Immediately remove the stale ScriptParseInfo so the context key for this
                    // file does not outlive the file's presence in the project. Also drop it
                    // from the FileUris set so TearDownProjectContext won't try it again on close.
                    lock (state.FileUris) { state.FileUris.Remove(fileUri); }
                    TSqlLanguageService.Instance.RemoveScriptParseInfo(fileUri);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"UpdateProjectIntelliSenseAsync error for {change.FilePathOrUri}: {ex}");
            }
        }

        internal static string GetAbsoluteFilePath(string projectUri, string filePathOrUri)
        {
            // Handle file:// URIs from LSP (e.g. "file:///c:/Users/..." or "file:///home/...")
            if (Uri.TryCreate(filePathOrUri, UriKind.Absolute, out Uri? parsedUri) && parsedUri.IsFile)
                return Path.GetFullPath(Utility.FileUtilities.UriToLocalPath(parsedUri));

            // Already an absolute OS path — normalise separators/casing via Path.GetFullPath.
            if (Path.IsPathRooted(filePathOrUri))
                return Path.GetFullPath(filePathOrUri);

            // Relative path — resolve against the project directory.
            // Use ToLocalPath so both file:// URIs and plain OS paths work on all platforms.
            string projectLocal = ToLocalPath(projectUri);
            string projectDir = Path.GetDirectoryName(projectLocal) ?? string.Empty;
            return Path.GetFullPath(Path.Combine(projectDir, filePathOrUri));
        }

        /// <summary>
        /// Returns the OS-native local path from either a <c>file://</c> URI string or a plain
        /// OS path (e.g. <c>/Users/...</c> on macOS or <c>c:\</c> on Windows), so IntelliSense
        /// bootstrap works regardless of whether the caller passes a URI or a raw path.
        /// </summary>
        internal static string ToLocalPath(string uriOrPath)
        {
            if (Uri.TryCreate(uriOrPath, UriKind.Absolute, out Uri? uri) && uri.IsFile)
                return Utility.FileUtilities.UriToLocalPath(uri);
            return uriOrPath; // already a plain OS path
        }

        private readonly record struct IntelliSenseChange(string FilePathOrUri, bool Deleted, string? SqlTextOverride);
    }

    /// <summary>
    /// A project's published IntelliSense model and what's needed to tear it down on close.
    /// </summary>
    internal sealed class ProjectIntelliSense
    {
        private ProjectIntelliSense(TSqlModel model, TSqlModelMetadataProvider provider, string contextKey, string databaseName, HashSet<string> fileUris, ParseOptions parseOptions)
        {
            Model = model;
            Provider = provider;
            ContextKey = contextKey;
            DatabaseName = databaseName;
            FileUris = fileUris;
            ParseOptions = parseOptions;
        }

        public TSqlModel Model { get; }

        public TSqlModelMetadataProvider Provider { get; }

        public string ContextKey { get; }

        public string DatabaseName { get; }

        /// <summary>
        /// File URIs stamped with this project's context. Lock on the set to read or change it.
        /// </summary>
        public HashSet<string> FileUris { get; }

        public ParseOptions ParseOptions { get; }

        public static ProjectIntelliSense Create(string projectUri, SqlProject project, TSqlModel model)
        {
            string databaseName = Path.GetFileNameWithoutExtension(projectUri);
            string projectDir = Path.GetDirectoryName(ProjectHost.ToLocalPath(projectUri))
                ?? throw new InvalidOperationException($"Cannot determine project directory from URI: {projectUri}");

            // Include all SQL files: Build items, PreDeploy, and PostDeploy
            IEnumerable<string> allScripts = project.SqlObjectScripts.Select(script => script.Path)
                .Concat(project.PreDeployScripts.Select(script => script.Path))
                .Concat(project.PostDeployScripts.Select(script => script.Path));

            var fileUris = new HashSet<string>(
                allScripts.Select(p =>
                {
                    // .sqlproj files always store relative paths with Windows backslashes.
                    // On macOS/Linux, Path.Combine does not treat '\\' as a separator,
                    // so we must normalise first or the resulting file URI will contain
                    // literal backslashes that never match VS Code's forward-slash URIs.
                    string norm = p.Replace('\\', Path.DirectorySeparatorChar);
                    return Utility.FileUtilities.LocalPathToFileUri(Path.IsPathRooted(norm) ? norm : Path.Combine(projectDir, norm));
                }),
                StringComparer.OrdinalIgnoreCase);

            var parseOptions = new ParseOptions(
                batchSeparator: TSqlLanguageService.DefaultBatchSeperator,
                isQuotedIdentifierSet: true,
                compatibilityLevel: DatabaseCompatibilityLevel.Current,
                transactSqlVersion: TransactSqlVersion.Current);

            return new ProjectIntelliSense(
                model,
                new TSqlModelMetadataProvider(model, databaseName),
                $"{TSqlLanguageService.ProjectContextKeyPrefix}{projectUri}",
                databaseName,
                fileUris,
                parseOptions);
        }

        /// <summary>
        /// Full IntelliSense teardown:
        /// 1. Remove binding context from the queue (releases MetadataProvider + _sourceLocations)
        /// 2. Remove ScriptParseInfo for all .sql files and the .sqlproj itself
        /// 3. Dispose TSqlModel to free DacFx unmanaged resources
        /// </summary>
        public void TearDown(string projectUri)
        {
            TSqlLanguageService.Instance.TearDownProjectContext(projectUri, ContextKey, FileUris);
            Model.Dispose();
        }
    }

    /// <summary>
    /// Thrown when an operation reaches a <see cref="ProjectHost"/> that has already been closed.
    /// </summary>
    internal sealed class ProjectHostClosedException : Exception
    {
        public ProjectHostClosedException(string projectUri)
            : base($"Project {projectUri} was closed.")
        {
        }
    }
}
