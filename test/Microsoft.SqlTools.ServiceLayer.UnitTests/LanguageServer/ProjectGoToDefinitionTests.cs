//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.SqlCore.IntelliSense;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    /// <summary>
    /// Debug harness for F12 (Go to Definition) on SQL project files.
    /// Run DebugF12 with debugger attached and step into GetDefinition to trace the failure.
    /// Set CallerFile and CursorLine/Column to the .sql file + symbol position you press F12 on.
    /// </summary>
    [TestFixture]
    public class ProjectGoToDefinitionTests
    {
        private const string ProjectPath =
            @"C:\Users\ssreerama\Downloads\DatabaseProjectkeep_HugeDbForIntelliSense_Legacy\DatabaseProjectkeep_HugeDbForIntelliSense_Legacy.sqlproj";

        // Set this to the .sql file you press F12 in, and the cursor position (0-based)
        private const string CallerFile   = @"C:\Users\ssreerama\Downloads\DatabaseProjectkeep_HugeDbForIntelliSense_Legacy\schema0\StoredProcedures\usp_GetDedicatedTable10.sql";
        private const int    CursorLine   = 10; // FROM [schema0].[DedicatedTable10] t
        private const int    CursorColumn = 20; // cursor on 'DedicatedTable10'

        [Test]
        public void DebugF12()
        {
            // Route all Logger.Verbose and Trace.TraceInformation calls to the test console
            Logger.Initialize(tracingLevel: System.Diagnostics.SourceLevels.Verbose, piiEnabled: false, logFilePath: null, autoFlush: true);
            Trace.Listeners.Add(new ConsoleTraceListener());

            // Mirrors BuildProjectIntelliSenseAsync in SqlProjectsService.cs
            SqlProject project  = SqlProject.OpenProject(ProjectPath);
            string databaseName = Path.GetFileNameWithoutExtension(ProjectPath);
            string projectDir   = Path.GetDirectoryName(ProjectPath) ?? string.Empty;
            string contextKey   = $"project_{ProjectPath}";

            var model        = TSqlModelBuilder.LoadModel(project);
            var provider     = new LazySchemaModelMetadataProvider(model, databaseName);
            var parseOptions = new ParseOptions(
                batchSeparator: LanguageService.DefaultBatchSeperator,
                isQuotedIdentifierSet: true,
                compatibilityLevel: DatabaseCompatibilityLevel.Current,
                transactSqlVersion: TransactSqlVersion.Current);

            var langService = new LanguageService();

            // Wire up a workspace so CurrentWorkspace is not null
            var workspaceSvc = new WorkspaceService<SqlToolsSettings>();
            workspaceSvc.Workspace = new Microsoft.SqlTools.ServiceLayer.Workspace.Workspace();
            langService.WorkspaceServiceInstance = workspaceSvc;

            // Mirrors UpdateLanguageServiceOnProjectOpen call
            langService.UpdateLanguageServiceOnProjectOpen(
                ProjectPath, provider, parseOptions, databaseName)
                .GetAwaiter().GetResult();

            // Mirrors InitializeProjectFileContexts call
            var fileUris = project.SqlObjectScripts
                .Select(s => new Uri(
                    Path.IsPathRooted(s.Path) ? s.Path : Path.Combine(projectDir, s.Path))
                    .AbsoluteUri)
                .ToList();

            Console.WriteLine($"[DEBUG] {fileUris.Count} sql files in project");
            langService.InitializeProjectFileContexts(fileUris, contextKey, databaseName);

            // Mirrors HandleDefinitionRequest in LanguageService.cs
            string callerUri  = new Uri(CallerFile).AbsoluteUri;
            string sqlContent = File.ReadAllText(CallerFile);
            var    scriptFile = langService.CurrentWorkspace.GetFileBuffer(callerUri, sqlContent);

            var parseInfo = langService.GetScriptParseInfo(callerUri);
            Console.WriteLine($"[DEBUG] IsConnected={parseInfo?.IsConnected}  ConnectionKey={parseInfo?.ConnectionKey}");

            var pos = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = callerUri },
                Position     = new Position { Line = CursorLine, Character = CursorColumn }
            };

            // ← PUT YOUR BREAKPOINT HERE and step into GetDefinition
            DefinitionResult result = langService.GetDefinition(pos, scriptFile, connInfo: null);

            Console.WriteLine($"[DEBUG] IsErrorResult={result?.IsErrorResult}  Message={result?.Message}");
            if (result?.Locations != null)
                foreach (var loc in result.Locations)
                    Console.WriteLine($"[DEBUG] → {loc.Uri}  L{loc.Range.Start.Line}:{loc.Range.Start.Character}");
        }
    }
}
