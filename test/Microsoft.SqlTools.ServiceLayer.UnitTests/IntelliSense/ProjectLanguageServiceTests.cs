//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.UnitTests.SqlProjects;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.SqlCore.IntelliSense;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.IntelliSense
{
    /// <summary>
    /// Tests for project-based language service features (Go to Definition, Hover, Completion)
    /// </summary>
    [TestFixture]
    public class ProjectLanguageServiceTests
    {
        private LanguageService _langService;
        private WorkspaceService<SqlToolsSettings> _workspaceService;
        private string _projectPath;
        private SqlProject _project;
        private TSqlModel _model;
        private string _projectUri;
        private string _contextKey;

        [SetUp]
        public void SetUp()
        {
            // Create a test project with schema, table, and stored procedure
            _projectPath = ProjectUtils.CreateTestProject("LanguageServiceTestProject");
            _project = SqlProject.OpenProject(_projectPath);

            // Add schema
            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("Schemas", "dbo.sql")),
                "CREATE SCHEMA dbo;");

            // Add a table with columns
            string tableScript = @"
CREATE TABLE dbo.Customers (
    CustomerId INT PRIMARY KEY,
    CustomerName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255)
);
";
            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("Tables", "Customers.sql")),
                tableScript);

            // Add stored procedure that references the table
            string spScript = @"
CREATE PROCEDURE dbo.GetCustomer
    @CustomerId INT
AS
BEGIN
    SELECT CustomerId, CustomerName, Email 
    FROM dbo.Customers 
    WHERE CustomerId = @CustomerId;
END
";
            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("StoredProcedures", "GetCustomer.sql")),
                spScript);

            // Build TSqlModel
            _model = TSqlModelBuilder.LoadModel(_project);

            // Create metadata provider
            string databaseName = Path.GetFileNameWithoutExtension(_projectPath);
            var metadataProvider = new TSqlModelMetadataProvider(_model, databaseName);

            // Set up parse options
            var parseOptions = new ParseOptions(
                batchSeparator: "GO",
                isQuotedIdentifierSet: true,
                compatibilityLevel: DatabaseCompatibilityLevel.Current,
                transactSqlVersion: TransactSqlVersion.Current);

            // Initialize language service
            _langService = new LanguageService();

            // Set up workspace service
            _workspaceService = new WorkspaceService<SqlToolsSettings>();
            _workspaceService.Workspace = new ServiceLayer.Workspace.Workspace();
            _langService.WorkspaceServiceInstance = _workspaceService;

            // Set up project context
            _projectUri = new Uri(_projectPath).AbsoluteUri;
            _contextKey = $"project_{_projectUri}";

            // Call UpdateLanguageServiceOnProjectOpen
            _langService.UpdateLanguageServiceOnProjectOpen(
                _projectUri, metadataProvider, parseOptions, databaseName)
                .GetAwaiter().GetResult();
        }

        [TearDown]
        public void TearDown()
        {
            _model?.Dispose();
            ProjectUtils.DeleteTestProject(_projectPath);
        }

        /// <summary>
        /// Test that UpdateLanguageServiceOnProjectOpen sets up the binding context correctly
        /// </summary>
        [Test]
        public void UpdateLanguageServiceOnProjectOpen_SetsUpBindingContext()
        {
            // Verify: Project URI should have ScriptParseInfo
            var parseInfo = _langService.GetScriptParseInfo(_projectUri);
            Assert.IsNotNull(parseInfo, "ScriptParseInfo should be created for project URI");

            // Verify: Should be marked as a project context (not a live SMO connection)
            Assert.IsTrue(parseInfo.IsProject, "Project should be marked as project context");

            // Verify: Should have project connection key
            Assert.AreEqual(_contextKey, parseInfo.ConnectionKey, "Connection key should match project context key");

            // Verify: Should have project database name
            Assert.AreEqual("LanguageServiceTestProject", parseInfo.ProjectDatabaseName, "Database name should be set");
        }

        /// <summary>
        /// Test that InitializeProjectFileContexts stamps all file URIs with the project context
        /// </summary>
        [Test]
        public void InitializeProjectFileContexts_StampsAllFileContexts()
        {
            // Arrange: Get all SQL file URIs from the project
            string projectDir = Path.GetDirectoryName(_projectPath);
            var fileUris = _project.SqlObjectScripts
                .Select(s => new Uri(
                    Path.IsPathRooted(s.Path) ? s.Path : Path.Combine(projectDir, s.Path))
                    .AbsoluteUri)
                .ToList();

            Assert.AreEqual(3, fileUris.Count, "Should have 3 SQL files in test project");

            // Act: Initialize all file contexts
            _langService.InitializeProjectFileContexts(fileUris, _contextKey, "LanguageServiceTestProject");

            // Assert: Each file should have ScriptParseInfo with project context
            foreach (var fileUri in fileUris)
            {
                var parseInfo = _langService.GetScriptParseInfo(fileUri);
                Assert.IsNotNull(parseInfo, $"ScriptParseInfo should exist for {fileUri}");
                Assert.IsTrue(parseInfo.IsProject, $"File {fileUri} should be marked as project context");
                Assert.AreEqual(_contextKey, parseInfo.ConnectionKey, 
                    $"File {fileUri} should have project connection key");
                Assert.AreEqual("LanguageServiceTestProject", parseInfo.ProjectDatabaseName,
                    $"File {fileUri} should have project database name");
            }
        }

        /// <summary>
        /// Test Go to Definition from a stored procedure to a table it references
        /// </summary>
        [Test]
        public void GoToDefinition_FindsTableDefinitionFromStoredProcedure()
        {
            // Arrange: Set up the stored procedure file context
            string projectDir = Path.GetDirectoryName(_projectPath);
            string spFilePath = Path.Combine(projectDir, "StoredProcedures", "GetCustomer.sql");
            string spFileUri = new Uri(spFilePath).AbsoluteUri;

            // Get the stored procedure content
            string spContent = @"
CREATE PROCEDURE dbo.GetCustomer
    @CustomerId INT
AS
BEGIN
    SELECT CustomerId, CustomerName, Email 
    FROM dbo.Customers 
    WHERE CustomerId = @CustomerId;
END
";

            // Add file to workspace
            var scriptFile = _workspaceService.Workspace.GetFileBuffer(spFileUri, spContent);

            // Initialize file context
            _langService.InitializeProjectFileContexts(
                new[] { spFileUri }, _contextKey, "LanguageServiceTestProject");

            // Position cursor on "Customers" in the FROM clause.
            // Line 6 (0-based): "    FROM dbo.Customers " — the script starts with a leading \n.
            // "    FROM dbo." is 13 chars, so char 15 is inside "Customers".
            var textPosition = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = spFileUri },
                Position = new Position { Line = 6, Character = 15 }
            };

            // Act: Request definition
            DefinitionResult result = _langService.GetDefinition(textPosition, scriptFile, connInfo: null);

            // Assert: Should find the table definition
            Assert.IsNotNull(result, "Definition result should not be null");
            Assert.IsFalse(result.IsErrorResult, $"Should not have error. Message: {result.Message}");
            Assert.IsNotNull(result.Locations, "Locations should not be null");
            Assert.Greater(result.Locations.Length, 0, "Should find at least one location");

            // Verify the location points to the Customers table file
            var tableLocation = result.Locations[0];
            Assert.IsTrue(tableLocation.Uri.Contains("Customers.sql"), 
                $"Definition should point to Customers.sql. Got: {tableLocation.Uri}");
        }

        /// <summary>
        /// Test that ParseInfo exists for project files after initialization
        /// </summary>
        [Test]
        public void ParseInfo_ExistsForAllProjectFiles()
        {
            // Arrange: Get all SQL file URIs
            string projectDir = Path.GetDirectoryName(_projectPath);
            var fileUris = _project.SqlObjectScripts
                .Select(s => new Uri(
                    Path.IsPathRooted(s.Path) ? s.Path : Path.Combine(projectDir, s.Path))
                    .AbsoluteUri)
                .ToList();

            // Act: Initialize contexts
            _langService.InitializeProjectFileContexts(fileUris, _contextKey, "LanguageServiceTestProject");

            // Assert: Each file should have parse info
            foreach (var fileUri in fileUris)
            {
                var parseInfo = _langService.GetScriptParseInfo(fileUri);
                Assert.IsNotNull(parseInfo, 
                    $"Parse info should exist for {Path.GetFileName(new Uri(fileUri).LocalPath)}");
            }
        }

        /// <summary>
        /// Test that project binding context contains metadata provider
        /// </summary>
        [Test]
        public void ProjectBindingContext_ContainsMetadataProvider()
        {
            // Arrange: Get the parse info for the project
            var parseInfo = _langService.GetScriptParseInfo(_projectUri);
            Assert.IsNotNull(parseInfo, "Parse info should exist");
            Assert.IsNotNull(parseInfo.ConnectionKey, "Connection key should be set");

            // Act: Get the binding context
            _langService.BindingQueue.BindingContextMap.TryGetValue(parseInfo.ConnectionKey, out var bindingContext);

            // Assert: Binding context should exist with binder and metadata provider
            Assert.IsNotNull(bindingContext, "Binding context should exist for project");
            Assert.IsNotNull(bindingContext.Binder, "Binder should be set in binding context");
            Assert.IsNotNull(bindingContext.MetadataDisplayInfoProvider, 
                "Metadata display info provider should be set");
        }

        /// <summary>
        /// Test Go to Definition with a schema whose name contains a dot (e.g. [SwaggerPetstore.Models]).
        /// The SqlParser Resolver returns DatabaseQualifiedName = "ProjectDB.SwaggerPetstore.Models.Get0ItemsItem"
        /// and QueueProjectTask must try the full name before stripping the first segment,
        /// otherwise the source index lookup fails because the key is "SwaggerPetstore.Models.Get0ItemsItem".
        /// </summary>
        [Test]
        public void GoToDefinition_FindsTableDefinitionWithDottedSchemaName()
        {
            // Arrange: Create a fresh project with a dotted schema and a table in that schema
            string projectPath = ProjectUtils.CreateTestProject("DottedSchemaProject");
            var project = SqlProject.OpenProject(projectPath);

            project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("Schemas", "SwaggerPetstoreModels.sql")),
                "CREATE SCHEMA [SwaggerPetstore.Models];");

            string tableScript = @"
CREATE TABLE [SwaggerPetstore.Models].[Get0ItemsItem] (
    Id INT PRIMARY KEY,
    Name NVARCHAR(100)
);
";
            project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("Tables", "Get0ItemsItem.sql")),
                tableScript);

            string spScript = @"
CREATE PROCEDURE [SwaggerPetstore.Models].[GetItem]
    @Id INT
AS
BEGIN
    SELECT Id, Name
    FROM [SwaggerPetstore.Models].[Get0ItemsItem]
    WHERE Id = @Id;
END
";
            project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("StoredProcedures", "GetItem.sql")),
                spScript);

            TSqlModel model = null;
            try
            {
                string databaseName = Path.GetFileNameWithoutExtension(projectPath);
                model = TSqlModelBuilder.LoadModel(project);
                var metadataProvider = new TSqlModelMetadataProvider(model, databaseName);

                var parseOptions = new ParseOptions(
                    batchSeparator: "GO",
                    isQuotedIdentifierSet: true,
                    compatibilityLevel: DatabaseCompatibilityLevel.Current,
                    transactSqlVersion: TransactSqlVersion.Current);

                var langService = new LanguageService();
                var workspaceService = new WorkspaceService<SqlToolsSettings>();
                workspaceService.Workspace = new ServiceLayer.Workspace.Workspace();
                langService.WorkspaceServiceInstance = workspaceService;

                string projectUri = new Uri(projectPath).AbsoluteUri;
                string contextKey = $"project_{projectUri}";

                langService.UpdateLanguageServiceOnProjectOpen(
                    projectUri, metadataProvider, parseOptions, databaseName)
                    .GetAwaiter().GetResult();

                // Set up the SP file in the workspace
                string projectDir = Path.GetDirectoryName(projectPath);
                string spFilePath = Path.Combine(projectDir, "StoredProcedures", "GetItem.sql");
                string spFileUri = new Uri(spFilePath).AbsoluteUri;

                var scriptFile = workspaceService.Workspace.GetFileBuffer(spFileUri, spScript);
                langService.InitializeProjectFileContexts(new[] { spFileUri }, contextKey, databaseName);

                // Position cursor on "Get0ItemsItem" in the FROM clause.
                // Line 6 (0-based): "    FROM [SwaggerPetstore.Models].[Get0ItemsItem]" — the script starts with a leading \n.
                // "    FROM [SwaggerPetstore.Models].[" is 35 chars, so char 38 is inside "Get0ItemsItem".
                var textPosition = new TextDocumentPosition
                {
                    TextDocument = new TextDocumentIdentifier { Uri = spFileUri },
                    Position = new Position { Line = 6, Character = 38 }
                };

                // Act
                DefinitionResult result = langService.GetDefinition(textPosition, scriptFile, connInfo: null);

                // Assert: Must succeed and point to Get0ItemsItem.sql
                Assert.IsNotNull(result, "Definition result should not be null");
                Assert.IsFalse(result.IsErrorResult, $"Should not have error. Message: {result.Message}");
                Assert.IsNotNull(result.Locations, "Locations should not be null");
                Assert.Greater(result.Locations.Length, 0, "Should find at least one location");
                Assert.IsTrue(result.Locations[0].Uri.Contains("Get0ItemsItem.sql"),
                    $"Definition should point to Get0ItemsItem.sql. Got: {result.Locations[0].Uri}");
            }
            finally
            {
                model?.Dispose();
                ProjectUtils.DeleteTestProject(projectPath);
            }
        }

        /// <summary>
        /// Completions after "dbo.Customers." should include column names from the model.
        /// Exercises TSqlModelTable.Columns → TSqlObject.GetReferenced(Table.Columns).
        /// </summary>
        [Test]
        public void Completions_ColumnNamesAppearsAfterTableDotAlias()
        {
            // Arrange: open a query file that aliases the table then types "c."
            string queryUri = "file:///test_query.sql";
            // The trailing space after "c." gives the parser a clean token boundary
            string queryContent = "SELECT c. FROM dbo.Customers AS c";
            var scriptFile = _workspaceService.Workspace.GetFileBuffer(queryUri, queryContent);
            _langService.InitializeProjectFileContexts(new[] { queryUri }, _contextKey, "LanguageServiceTestProject");

            // Position: line 0 (0-based), character 9 — right after the dot in "c."
            var position = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = queryUri },
                Position = new Position { Line = 0, Character = 9 }
            };

            // Act
            var completions = _langService.GetCompletionItems(position, scriptFile, connInfo: null)
                                          .GetAwaiter().GetResult();

            // Assert: at least CustomerId, CustomerName, Email should appear
            Assert.IsNotNull(completions, "Completions should not be null");
            var names = completions.Select(c => c.Label).ToList();
            Assert.IsTrue(names.Any(n => string.Equals(n, "CustomerId",   StringComparison.OrdinalIgnoreCase)),
                $"Expected 'CustomerId' in completions. Got: {string.Join(", ", names.Take(20))}");
            Assert.IsTrue(names.Any(n => string.Equals(n, "CustomerName", StringComparison.OrdinalIgnoreCase)),
                $"Expected 'CustomerName' in completions. Got: {string.Join(", ", names.Take(20))}");
            Assert.IsTrue(names.Any(n => string.Equals(n, "Email",        StringComparison.OrdinalIgnoreCase)),
                $"Expected 'Email' in completions. Got: {string.Join(", ", names.Take(20))}");
        }

        /// <summary>
        /// Hover on "Customers" should return a tooltip containing "table" and "Customers".
        /// Exercises Resolver.GetQuickInfo → bound ParseResult.
        /// </summary>
        [Test]
        public void Hover_ReturnsTableTooltip()
        {
            // Arrange
            string queryUri = "file:///test_hover.sql";
            string queryContent = "SELECT * FROM dbo.Customers";
            var scriptFile = _workspaceService.Workspace.GetFileBuffer(queryUri, queryContent);
            _langService.InitializeProjectFileContexts(new[] { queryUri }, _contextKey, "LanguageServiceTestProject");

            // ParseAndBind must run before GetHoverItem — hover reads the already-bound ParseResult;
            // it does not call ParseAndBind itself.
            _langService.ParseAndBind(scriptFile, connInfo: null).GetAwaiter().GetResult();

            // Position: line 0, character 22 — inside "Customers"
            var position = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = queryUri },
                Position = new Position { Line = 0, Character = 22 }
            };

            // Act
            var hover = _langService.GetHoverItem(position, scriptFile);

            // Assert: hover tooltip should mention "table" and "Customers"
            Assert.IsNotNull(hover, "Hover result should not be null");
            // Contents is MarkedString[] — each entry has a .Value string
            var markedStrings = hover.Contents as MarkedString[];
            Assert.IsNotNull(markedStrings, "Hover contents should be a MarkedString array");
            string hoverText = string.Join(" ", markedStrings.Select(m => m.Value));
            StringAssert.Contains("Customers", hoverText, "Hover should mention the table name");
            StringAssert.Contains("table", hoverText, "Hover should identify the object type as table");
        }

        /// <summary>
        /// Regression: hover tooltip on a project column must show the real data type and nullability,
        /// not the broken "(, null)" that appeared when TSqlModelColumn.DataType returned null.
        /// Exercises the fix to store TSqlObject and call GetReferenced(Column.DataType) /
        /// GetProperty&lt;bool&gt;(Column.Nullable) instead of the old hardcoded stubs.
        /// </summary>
        [Test]
        public void Hover_ColumnTooltip_ShowsDataTypeAndNullability()
        {
            // "CustomerId INT PRIMARY KEY" — NOT NULL (primary key implies non-nullable)
            // "Email NVARCHAR(255)"        — nullable (no NOT NULL constraint)
            string queryUri = "file:///test_column_hover.sql";
            string queryContent = "SELECT CustomerId, Email FROM dbo.Customers";
            var scriptFile = _workspaceService.Workspace.GetFileBuffer(queryUri, queryContent);
            _langService.InitializeProjectFileContexts(new[] { queryUri }, _contextKey, "LanguageServiceTestProject");
            _langService.ParseAndBind(scriptFile, connInfo: null).GetAwaiter().GetResult();

            // --- CustomerId (INT, NOT NULL) ---
            var posCustomerId = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = queryUri },
                Position = new Position { Line = 0, Character = 8 }  // inside "CustomerId"
            };
            var hoverCustomerId = _langService.GetHoverItem(posCustomerId, scriptFile);
            Assert.IsNotNull(hoverCustomerId, "Hover result should not be null for CustomerId");
            var stringsCustomerId = hoverCustomerId.Contents as MarkedString[];
            Assert.IsNotNull(stringsCustomerId, "Hover contents should be MarkedString[] for CustomerId");
            string textCustomerId = string.Join(" ", stringsCustomerId.Select(m => m.Value));
            StringAssert.Contains("int", textCustomerId,
                $"Hover for CustomerId should contain data type 'int'. Got: {textCustomerId}");
            StringAssert.DoesNotContain("(, null)", textCustomerId,
                $"Hover should not contain broken '(, null)'. Got: {textCustomerId}");

            // --- Email (NVARCHAR(255), nullable) ---
            var posEmail = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = queryUri },
                Position = new Position { Line = 0, Character = 20 }  // inside "Email"
            };
            var hoverEmail = _langService.GetHoverItem(posEmail, scriptFile);
            Assert.IsNotNull(hoverEmail, "Hover result should not be null for Email");
            var stringsEmail = hoverEmail.Contents as MarkedString[];
            Assert.IsNotNull(stringsEmail, "Hover contents should be MarkedString[] for Email");
            string textEmail = string.Join(" ", stringsEmail.Select(m => m.Value));
            StringAssert.Contains("nvarchar", textEmail,
                $"Hover for Email should contain data type 'nvarchar'. Got: {textEmail}");
            StringAssert.DoesNotContain("(, null)", textEmail,
                $"Hover should not contain broken '(, null)'. Got: {textEmail}");
        }

        /// <summary>
        /// Regression: F12 on a schema-qualified two-part name ("dbo.Customers") must succeed and
        /// return the correct source file and a valid (>=0) line number.
        /// The LazyCollection name indexer previously threw KeyNotFoundException on cache miss;
        /// it now returns null so the binder can continue resolving.
        /// </summary>
        [Test]
        public void GoToDefinition_SchemaQualifiedName_ResolvesToCorrectFileAndLine()
        {
            // Arrange: cursor on "Customers" in "SELECT * FROM dbo.Customers" (line 0, char 22)
            string queryUri = "file:///test_schemaqualified.sql";
            var scriptFile = _workspaceService.Workspace.GetFileBuffer(queryUri, "SELECT * FROM dbo.Customers");
            _langService.InitializeProjectFileContexts(new[] { queryUri }, _contextKey, "LanguageServiceTestProject");

            var position = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = queryUri },
                Position = new Position { Line = 0, Character = 22 }
            };

            // Act
            DefinitionResult result = _langService.GetDefinition(position, scriptFile, connInfo: null);

            // Assert: resolves to Customers.sql at a valid line
            Assert.IsNotNull(result, "Definition result should not be null");
            Assert.IsFalse(result.IsErrorResult, $"Should not have error. Message: {result.Message}");
            Assert.IsTrue(result.Locations[0].Uri.Contains("Customers.sql"),
                $"Should point to Customers.sql. Got: {result.Locations[0].Uri}");
            // DacFx source locations are 1-based; QueueProjectTask converts to 0-based.
            Assert.GreaterOrEqual(result.Locations[0].Range.Start.Line, 0,
                "Source line should be a valid 0-based line number");
        }

        /// <summary>
        /// Regression: UpdateLanguageServiceOnConnection must NOT overwrite the "project_" context key
        /// with a live-connection key when the user also opens a server connection on the same file.
        /// </summary>
        [Test]
        public void ServerConnection_DoesNotOverwriteProjectContextKey()
        {
            // Arrange: confirm the project URI has the project context key
            var parseInfo = _langService.GetScriptParseInfo(_projectUri);
            Assert.IsNotNull(parseInfo);
            string originalKey = parseInfo.ConnectionKey;
            Assert.IsTrue(originalKey.StartsWith("project_", StringComparison.Ordinal),
                "Key should start with 'project_' before any connection attempt");

            // Act: simulate what UpdateLanguageServiceOnConnection does — it should bail out early
            // because IsProjectContext returns true (the OwnerUri already has a project_ key).
            // We pass a real ConnectionInfo pointing at the project URI so the guard can read OwnerUri.
            var fakeDetails = new ConnectionDetails { ServerName = "fakeserver", DatabaseName = "fakedb" };
            var fakeConn = new ConnectionInfo(factory: null, ownerUri: _projectUri, details: fakeDetails);
            _langService.UpdateLanguageServiceOnConnection(fakeConn).GetAwaiter().GetResult();

            // Assert: key must still be the project key
            var parseInfoAfter = _langService.GetScriptParseInfo(_projectUri);
            Assert.AreEqual(originalKey, parseInfoAfter.ConnectionKey,
                "Project context key must not be overwritten by a server connection");
        }

        /// <summary>
        /// Test that multiple files can share the same project context
        /// </summary>
        [Test]
        public void MultipleFiles_ShareSameProjectContext()
        {
            // Arrange: Get file URIs
            string projectDir = Path.GetDirectoryName(_projectPath);
            var fileUris = _project.SqlObjectScripts
                .Select(s => new Uri(
                    Path.IsPathRooted(s.Path) ? s.Path : Path.Combine(projectDir, s.Path))
                    .AbsoluteUri)
                .ToList();

            // Act: Initialize all files
            _langService.InitializeProjectFileContexts(fileUris, _contextKey, "LanguageServiceTestProject");

            // Assert: All files should share the same connection key
            string sharedConnectionKey = null;
            foreach (var fileUri in fileUris)
            {
                var parseInfo = _langService.GetScriptParseInfo(fileUri);
                if (sharedConnectionKey == null)
                {
                    sharedConnectionKey = parseInfo.ConnectionKey;
                }
                else
                {
                    Assert.AreEqual(sharedConnectionKey, parseInfo.ConnectionKey,
                        $"All files should share the same connection key. File: {Path.GetFileName(new Uri(fileUri).LocalPath)}");
                }
            }
        }
    }
}
