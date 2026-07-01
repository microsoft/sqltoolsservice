//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.LanguageService.Connection.Contracts;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using Microsoft.SqlTools.LanguageService.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.SqlProjects;
using Microsoft.SqlTools.ServiceLayer.UnitTests.SqlProjects;
using Microsoft.SqlTools.LanguageService.Workspace;
using Microsoft.SqlTools.LanguageService.Workspace.Contracts;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.SqlCore.IntelliSense;
using Moq;
using NUnit.Framework;
using WsLocation = Microsoft.SqlTools.LanguageService.Workspace.Contracts.Location;
using LangService = Microsoft.SqlTools.ServiceLayer.LanguageServices.LanguageService;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.IntelliSense
{
    /// <summary>
    /// Tests for project-based language service features (Go to Definition, Hover, Completion)
    /// </summary>
    [TestFixture]
    public class ProjectLanguageServiceTests
    {
        private LangService _langService;
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
            _langService = new LangService();
            _langService.ConnectionServiceInstance = ConnectionService.Instance;

            // Set up workspace service
            _workspaceService = new WorkspaceService<SqlToolsSettings>();
            _workspaceService.Workspace = new Microsoft.SqlTools.LanguageService.Workspace.Workspace();
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

                var langService = new LangService();
                var workspaceService = new WorkspaceService<SqlToolsSettings>();
                workspaceService.Workspace = new Microsoft.SqlTools.LanguageService.Workspace.Workspace();
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

        /// <summary>
        /// F12 on the referenced table name inside a DDL FOREIGN KEY ... REFERENCES clause must
        /// resolve to the table's source file, for both plain and bracket-quoted identifiers.
        ///
        /// FindCompletions returns nothing in REFERENCES clauses, so the schema-walk path
        /// (Step 2 in QueueProjectTask / GetPrecedingSchemaPrefix) is the only resolver.
        ///
        /// Cursor positions on line 3 (0-based), inside "Customers" / "[Customers]":
        ///   plain:     "... REFERENCES dbo.Customers(..."   — "Customers" starts at col 75
        ///   bracketed: "... REFERENCES [dbo].[Customers](..." — "[Customers]" starts at col 77
        /// </summary>
        [TestCase(
            "    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId)",
            78,
            TestName = "GoToDefinition_ForeignKeyReferences_SimpleSchema")]
        [TestCase(
            "    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES [dbo].[Customers]([CustomerId])",
            80,
            TestName = "GoToDefinition_ForeignKeyReferences_BracketedIdentifiers")]
        public void GoToDefinition_ForeignKeyReferences_UsesTokenWalk(string constraintLine, int cursorColumn)
        {
            string queryContent =
                "CREATE TABLE dbo.Orders (\n" +
                "    OrderId INT PRIMARY KEY,\n" +
                "    CustomerId INT NOT NULL,\n" +
                constraintLine + "\n" +
                ");";

            string queryUri = "file:///test_fk_references.sql";
            var scriptFile = _workspaceService.Workspace.GetFileBuffer(queryUri, queryContent);
            _langService.InitializeProjectFileContexts(new[] { queryUri }, _contextKey, "LanguageServiceTestProject");

            var position = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = queryUri },
                Position = new Position { Line = 3, Character = cursorColumn }
            };

            DefinitionResult result = _langService.GetDefinition(position, scriptFile, connInfo: null);

            Assert.IsNotNull(result, "Definition result should not be null");
            Assert.IsFalse(result.IsErrorResult,
                $"Go-to-definition in REFERENCES clause should succeed. Message: {result.Message}");
            Assert.IsNotNull(result.Locations, "Locations should not be null");
            Assert.Greater(result.Locations.Length, 0, "Should find at least one location");
            Assert.IsTrue(result.Locations[0].Uri.Contains("Customers.sql"),
                $"Definition should point to Customers.sql. Got: {result.Locations[0].Uri}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Incremental IntelliSense update tests
    // Verify that TSqlModelMetadataProvider.UpdateForFileChange patches the
    // metadata wrappers without rebuilding the entire schema.
    // ────────────────────────────────────────────────────────────────────────
    [TestFixture]
    public class IncrementalIntelliSenseUpdateTests
    {
        private const string DbName = "TestDb";
        private const string Source1 = @"C:\fake\project\Table1.sql";
        private const string Source2 = @"C:\fake\project\Table2.sql";

        private static TSqlModel BuildModel(params (string sql, string source)[] files)
        {
            var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
            foreach (var (sql, source) in files)
                model.AddOrUpdateObjects(sql, source, new TSqlObjectOptions());
            return model;
        }

        [Test]
        public void AddTable_AppearsInSchema_OtherTableUntouched()
        {
            using var model = BuildModel(
                ("CREATE TABLE [dbo].[Orders] ([Id] INT)", Source1));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            Assert.That(db.Schemas["dbo"]?.Tables["Orders"], Is.Not.Null, "Orders should exist before add");
            Assert.That(db.Schemas["dbo"]?.Tables["Customers"], Is.Null, "Customers should not exist before add");

            model.AddOrUpdateObjects("CREATE TABLE [dbo].[Customers] ([Id] INT)", Source2, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source2, deleted: false);

            Assert.That(db.Schemas["dbo"]?.Tables["Customers"], Is.Not.Null, "Customers should appear after add");
            Assert.That(db.Schemas["dbo"]?.Tables["Orders"], Is.Not.Null, "Orders should still exist after add");
        }

        [Test]
        public void DeleteTable_DisappearsFromSchema_OtherTableUntouched()
        {
            using var model = BuildModel(
                ("CREATE TABLE [dbo].[Orders] ([Id] INT)", Source1),
                ("CREATE TABLE [dbo].[Customers] ([Id] INT)", Source2));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            Assert.That(db.Schemas["dbo"]?.Tables["Orders"], Is.Not.Null, "Orders before delete");
            Assert.That(db.Schemas["dbo"]?.Tables["Customers"], Is.Not.Null, "Customers before delete");

            model.DeleteObjects(Source2);
            provider.UpdateForFileChange(Source2, deleted: true);

            Assert.That(db.Schemas["dbo"]?.Tables["Customers"], Is.Null, "Customers should be gone after delete");
            Assert.That(db.Schemas["dbo"]?.Tables["Orders"], Is.Not.Null, "Orders should be unaffected by delete");
        }

        [Test]
        public void ModifyTable_ColumnsRefreshed_OtherTableUnchanged()
        {
            using var model = BuildModel(
                ("CREATE TABLE [dbo].[Orders] ([Id] INT)", Source1),
                ("CREATE TABLE [dbo].[Customers] ([Id] INT)", Source2));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            int ordersColsBefore = db.Schemas["dbo"]!.Tables["Orders"]!.Columns.Count;
            int customersColsBefore = db.Schemas["dbo"]!.Tables["Customers"]!.Columns.Count;
            Assert.That(ordersColsBefore, Is.EqualTo(1), "Orders should have 1 column before modify");
            Assert.That(customersColsBefore, Is.EqualTo(1), "Customers should have 1 column before modify");

            model.AddOrUpdateObjects("CREATE TABLE [dbo].[Orders] ([Id] INT, [Name] NVARCHAR(100))", Source1, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source1, deleted: false);

            Assert.That(db.Schemas["dbo"]!.Tables["Orders"]!.Columns.Count, Is.EqualTo(2), "Orders should have 2 columns after modify");
            Assert.That(db.Schemas["dbo"]!.Tables["Customers"]!.Columns.Count, Is.EqualTo(1), "Customers column count should be unchanged");
        }

        [Test]
        public void UpdateForFileChange_PatchesSourceLocationIndex()
        {
            using var model = BuildModel(
                ("CREATE TABLE [dbo].[Orders] ([Id] INT)", Source1));

            var provider = new TSqlModelMetadataProvider(model, DbName);

            Assert.That(provider.TryGetSourceInformation("dbo.Orders", out var info1), Is.True, "Should find dbo.Orders before update");
            Assert.That(info1!.SourceName, Is.EqualTo(Source1).IgnoreCase);

            model.DeleteObjects(Source1);
            model.AddOrUpdateObjects("CREATE TABLE [dbo].[Orders] ([Id] INT)", Source2, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source1, deleted: true);
            provider.UpdateForFileChange(Source2, deleted: false);

            Assert.That(provider.TryGetSourceInformation("dbo.Orders", out var info2), Is.True, "Should still find dbo.Orders after update");
            Assert.That(info2!.SourceName, Is.EqualTo(Source2).IgnoreCase, "Source should now point to Source2");
        }

        [Test]
        public async Task UpdateProjectIntelliSenseAsync_NoException_WhenProjectNotOpen()
        {
            var service = new SqlProjectsService();
            string tempSql = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.sql");

            try
            {
                await File.WriteAllTextAsync(tempSql, "CREATE TABLE [dbo].[T] ([Id] INT)");
                await service.UpdateProjectIntelliSenseAsync("file:///does/not/exist.sqlproj", tempSql, deleted: false);
            }
            finally
            {
                if (File.Exists(tempSql)) File.Delete(tempSql);
            }
        }

        // ── Views ─────────────────────────────────────────────────────────────────

        [Test]
        public void AddView_AppearsInSchema_OtherViewUntouched()
        {
            using var model = BuildModel(
                ("CREATE VIEW [dbo].[OrderView] AS SELECT 1 AS Id", Source1));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            Assert.That(db.Schemas["dbo"]?.Views["OrderView"], Is.Not.Null, "OrderView should exist before add");
            Assert.That(db.Schemas["dbo"]?.Views["CustomerView"], Is.Null, "CustomerView should not exist before add");

            model.AddOrUpdateObjects("CREATE VIEW [dbo].[CustomerView] AS SELECT 2 AS Id", Source2, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source2, deleted: false);

            Assert.That(db.Schemas["dbo"]?.Views["CustomerView"], Is.Not.Null, "CustomerView should appear after add");
            Assert.That(db.Schemas["dbo"]?.Views["OrderView"], Is.Not.Null, "OrderView should still exist after add");
        }

        [Test]
        public void DeleteView_DisappearsFromSchema_OtherViewUntouched()
        {
            using var model = BuildModel(
                ("CREATE VIEW [dbo].[OrderView] AS SELECT 1 AS Id", Source1),
                ("CREATE VIEW [dbo].[CustomerView] AS SELECT 2 AS Id", Source2));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            Assert.That(db.Schemas["dbo"]?.Views["OrderView"], Is.Not.Null, "OrderView before delete");
            Assert.That(db.Schemas["dbo"]?.Views["CustomerView"], Is.Not.Null, "CustomerView before delete");

            model.DeleteObjects(Source2);
            provider.UpdateForFileChange(Source2, deleted: true);

            Assert.That(db.Schemas["dbo"]?.Views["CustomerView"], Is.Null, "CustomerView should be gone after delete");
            Assert.That(db.Schemas["dbo"]?.Views["OrderView"], Is.Not.Null, "OrderView should be unaffected by delete");
        }

        [Test]
        public void ModifyView_StillAccessibleAfterUpdate_OtherViewUnchanged()
        {
            using var model = BuildModel(
                ("CREATE VIEW [dbo].[OrderView] AS SELECT 1 AS Id", Source1),
                ("CREATE VIEW [dbo].[CustomerView] AS SELECT 1 AS Id", Source2));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            // Touch both to ensure the collection is initialized
            _ = db.Schemas["dbo"]?.Views["OrderView"];
            _ = db.Schemas["dbo"]?.Views["CustomerView"];

            model.AddOrUpdateObjects("CREATE VIEW [dbo].[OrderView] AS SELECT 1 AS Id, 2 AS Extra", Source1, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source1, deleted: false);

            Assert.That(db.Schemas["dbo"]?.Views["OrderView"], Is.Not.Null, "OrderView should still exist after modify");
            Assert.That(db.Schemas["dbo"]?.Views["CustomerView"], Is.Not.Null, "CustomerView should be unaffected by modify");
        }

        // ── Stored Procedures ─────────────────────────────────────────────────────

        [Test]
        public void AddStoredProcedure_AppearsInSchema_OtherProcedureUntouched()
        {
            using var model = BuildModel(
                ("CREATE PROCEDURE [dbo].[GetOrders] AS BEGIN SELECT 1 END", Source1));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            Assert.That(db.Schemas["dbo"]?.StoredProcedures["GetOrders"], Is.Not.Null, "GetOrders should exist before add");
            Assert.That(db.Schemas["dbo"]?.StoredProcedures["GetCustomers"], Is.Null, "GetCustomers should not exist before add");

            model.AddOrUpdateObjects("CREATE PROCEDURE [dbo].[GetCustomers] AS BEGIN SELECT 2 END", Source2, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source2, deleted: false);

            Assert.That(db.Schemas["dbo"]?.StoredProcedures["GetCustomers"], Is.Not.Null, "GetCustomers should appear after add");
            Assert.That(db.Schemas["dbo"]?.StoredProcedures["GetOrders"], Is.Not.Null, "GetOrders should still exist after add");
        }

        [Test]
        public void DeleteStoredProcedure_DisappearsFromSchema_OtherProcedureUntouched()
        {
            using var model = BuildModel(
                ("CREATE PROCEDURE [dbo].[GetOrders] AS BEGIN SELECT 1 END", Source1),
                ("CREATE PROCEDURE [dbo].[GetCustomers] AS BEGIN SELECT 2 END", Source2));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            Assert.That(db.Schemas["dbo"]?.StoredProcedures["GetOrders"], Is.Not.Null, "GetOrders before delete");
            Assert.That(db.Schemas["dbo"]?.StoredProcedures["GetCustomers"], Is.Not.Null, "GetCustomers before delete");

            model.DeleteObjects(Source2);
            provider.UpdateForFileChange(Source2, deleted: true);

            Assert.That(db.Schemas["dbo"]?.StoredProcedures["GetCustomers"], Is.Null, "GetCustomers should be gone after delete");
            Assert.That(db.Schemas["dbo"]?.StoredProcedures["GetOrders"], Is.Not.Null, "GetOrders should be unaffected by delete");
        }

        [Test]
        public void ModifyStoredProcedure_StillAccessibleAfterUpdate_OtherProcedureUnchanged()
        {
            using var model = BuildModel(
                ("CREATE PROCEDURE [dbo].[GetOrders] AS BEGIN SELECT 1 END", Source1),
                ("CREATE PROCEDURE [dbo].[GetCustomers] AS BEGIN SELECT 2 END", Source2));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            _ = db.Schemas["dbo"]?.StoredProcedures["GetOrders"];
            _ = db.Schemas["dbo"]?.StoredProcedures["GetCustomers"];

            model.AddOrUpdateObjects("CREATE PROCEDURE [dbo].[GetOrders] @Id INT AS BEGIN SELECT @Id END", Source1, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source1, deleted: false);

            Assert.That(db.Schemas["dbo"]?.StoredProcedures["GetOrders"], Is.Not.Null, "GetOrders should still exist after modify");
            Assert.That(db.Schemas["dbo"]?.StoredProcedures["GetCustomers"], Is.Not.Null, "GetCustomers should be unaffected by modify");
        }

        // ── Scalar Functions ──────────────────────────────────────────────────────

        [Test]
        public void AddScalarFunction_AppearsInSchema_OtherFunctionUntouched()
        {
            using var model = BuildModel(
                ("CREATE FUNCTION [dbo].[GetCount]() RETURNS INT AS BEGIN RETURN 1 END", Source1));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            Assert.That(db.Schemas["dbo"]?.ScalarValuedFunctions["GetCount"], Is.Not.Null, "GetCount should exist before add");
            Assert.That(db.Schemas["dbo"]?.ScalarValuedFunctions["GetTotal"], Is.Null, "GetTotal should not exist before add");

            model.AddOrUpdateObjects("CREATE FUNCTION [dbo].[GetTotal]() RETURNS INT AS BEGIN RETURN 2 END", Source2, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source2, deleted: false);

            Assert.That(db.Schemas["dbo"]?.ScalarValuedFunctions["GetTotal"], Is.Not.Null, "GetTotal should appear after add");
            Assert.That(db.Schemas["dbo"]?.ScalarValuedFunctions["GetCount"], Is.Not.Null, "GetCount should still exist after add");
        }

        [Test]
        public void DeleteScalarFunction_DisappearsFromSchema_OtherFunctionUntouched()
        {
            using var model = BuildModel(
                ("CREATE FUNCTION [dbo].[GetCount]() RETURNS INT AS BEGIN RETURN 1 END", Source1),
                ("CREATE FUNCTION [dbo].[GetTotal]() RETURNS INT AS BEGIN RETURN 2 END", Source2));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            Assert.That(db.Schemas["dbo"]?.ScalarValuedFunctions["GetCount"], Is.Not.Null, "GetCount before delete");
            Assert.That(db.Schemas["dbo"]?.ScalarValuedFunctions["GetTotal"], Is.Not.Null, "GetTotal before delete");

            model.DeleteObjects(Source2);
            provider.UpdateForFileChange(Source2, deleted: true);

            Assert.That(db.Schemas["dbo"]?.ScalarValuedFunctions["GetTotal"], Is.Null, "GetTotal should be gone after delete");
            Assert.That(db.Schemas["dbo"]?.ScalarValuedFunctions["GetCount"], Is.Not.Null, "GetCount should be unaffected by delete");
        }

        [Test]
        public void ModifyScalarFunction_StillAccessibleAfterUpdate_OtherFunctionUnchanged()
        {
            using var model = BuildModel(
                ("CREATE FUNCTION [dbo].[GetCount]() RETURNS INT AS BEGIN RETURN 1 END", Source1),
                ("CREATE FUNCTION [dbo].[GetTotal]() RETURNS INT AS BEGIN RETURN 2 END", Source2));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            _ = db.Schemas["dbo"]?.ScalarValuedFunctions["GetCount"];
            _ = db.Schemas["dbo"]?.ScalarValuedFunctions["GetTotal"];

            model.AddOrUpdateObjects("CREATE FUNCTION [dbo].[GetCount]() RETURNS BIGINT AS BEGIN RETURN 99 END", Source1, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source1, deleted: false);

            Assert.That(db.Schemas["dbo"]?.ScalarValuedFunctions["GetCount"], Is.Not.Null, "GetCount should still exist after modify");
            Assert.That(db.Schemas["dbo"]?.ScalarValuedFunctions["GetTotal"], Is.Not.Null, "GetTotal should be unaffected by modify");
        }

        // ── Table-Valued Functions ────────────────────────────────────────────────

        [Test]
        public void AddTableValuedFunction_AppearsInSchema_OtherFunctionUntouched()
        {
            using var model = BuildModel(
                ("CREATE FUNCTION [dbo].[GetOrderRows]() RETURNS TABLE AS RETURN (SELECT 1 AS Id)", Source1));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            Assert.That(db.Schemas["dbo"]?.TableValuedFunctions["GetOrderRows"], Is.Not.Null, "GetOrderRows should exist before add");
            Assert.That(db.Schemas["dbo"]?.TableValuedFunctions["GetCustomerRows"], Is.Null, "GetCustomerRows should not exist before add");

            model.AddOrUpdateObjects("CREATE FUNCTION [dbo].[GetCustomerRows]() RETURNS TABLE AS RETURN (SELECT 2 AS Id)", Source2, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source2, deleted: false);

            Assert.That(db.Schemas["dbo"]?.TableValuedFunctions["GetCustomerRows"], Is.Not.Null, "GetCustomerRows should appear after add");
            Assert.That(db.Schemas["dbo"]?.TableValuedFunctions["GetOrderRows"], Is.Not.Null, "GetOrderRows should still exist after add");
        }

        [Test]
        public void DeleteTableValuedFunction_DisappearsFromSchema_OtherFunctionUntouched()
        {
            using var model = BuildModel(
                ("CREATE FUNCTION [dbo].[GetOrderRows]() RETURNS TABLE AS RETURN (SELECT 1 AS Id)", Source1),
                ("CREATE FUNCTION [dbo].[GetCustomerRows]() RETURNS TABLE AS RETURN (SELECT 2 AS Id)", Source2));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            Assert.That(db.Schemas["dbo"]?.TableValuedFunctions["GetOrderRows"], Is.Not.Null, "GetOrderRows before delete");
            Assert.That(db.Schemas["dbo"]?.TableValuedFunctions["GetCustomerRows"], Is.Not.Null, "GetCustomerRows before delete");

            model.DeleteObjects(Source2);
            provider.UpdateForFileChange(Source2, deleted: true);

            Assert.That(db.Schemas["dbo"]?.TableValuedFunctions["GetCustomerRows"], Is.Null, "GetCustomerRows should be gone after delete");
            Assert.That(db.Schemas["dbo"]?.TableValuedFunctions["GetOrderRows"], Is.Not.Null, "GetOrderRows should be unaffected by delete");
        }

        [Test]
        public void ModifyTableValuedFunction_StillAccessibleAfterUpdate_OtherFunctionUnchanged()
        {
            using var model = BuildModel(
                ("CREATE FUNCTION [dbo].[GetOrderRows]() RETURNS TABLE AS RETURN (SELECT 1 AS Id)", Source1),
                ("CREATE FUNCTION [dbo].[GetCustomerRows]() RETURNS TABLE AS RETURN (SELECT 2 AS Id)", Source2));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            _ = db.Schemas["dbo"]?.TableValuedFunctions["GetOrderRows"];
            _ = db.Schemas["dbo"]?.TableValuedFunctions["GetCustomerRows"];

            model.AddOrUpdateObjects("CREATE FUNCTION [dbo].[GetOrderRows]() RETURNS TABLE AS RETURN (SELECT 1 AS Id, 2 AS Extra)", Source1, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source1, deleted: false);

            Assert.That(db.Schemas["dbo"]?.TableValuedFunctions["GetOrderRows"], Is.Not.Null, "GetOrderRows should still exist after modify");
            Assert.That(db.Schemas["dbo"]?.TableValuedFunctions["GetCustomerRows"], Is.Not.Null, "GetCustomerRows should be unaffected by modify");
        }

        // ── User-Defined Data Types ───────────────────────────────────────────────

        [Test]
        public void AddUserDefinedDataType_AppearsInSchema_OtherTypeUntouched()
        {
            using var model = BuildModel(
                ("CREATE TYPE [dbo].[PhoneNumber] FROM NVARCHAR(20) NOT NULL", Source1));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            Assert.That(db.Schemas["dbo"]?.UserDefinedDataTypes["PhoneNumber"], Is.Not.Null, "PhoneNumber should exist before add");
            Assert.That(db.Schemas["dbo"]?.UserDefinedDataTypes["PostalCode"], Is.Null, "PostalCode should not exist before add");

            model.AddOrUpdateObjects("CREATE TYPE [dbo].[PostalCode] FROM NVARCHAR(10) NOT NULL", Source2, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source2, deleted: false);

            Assert.That(db.Schemas["dbo"]?.UserDefinedDataTypes["PostalCode"], Is.Not.Null, "PostalCode should appear after add");
            Assert.That(db.Schemas["dbo"]?.UserDefinedDataTypes["PhoneNumber"], Is.Not.Null, "PhoneNumber should still exist after add");
        }

        [Test]
        public void DeleteUserDefinedDataType_DisappearsFromSchema_OtherTypeUntouched()
        {
            using var model = BuildModel(
                ("CREATE TYPE [dbo].[PhoneNumber] FROM NVARCHAR(20) NOT NULL", Source1),
                ("CREATE TYPE [dbo].[PostalCode] FROM NVARCHAR(10) NOT NULL", Source2));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            Assert.That(db.Schemas["dbo"]?.UserDefinedDataTypes["PhoneNumber"], Is.Not.Null, "PhoneNumber before delete");
            Assert.That(db.Schemas["dbo"]?.UserDefinedDataTypes["PostalCode"], Is.Not.Null, "PostalCode before delete");

            model.DeleteObjects(Source2);
            provider.UpdateForFileChange(Source2, deleted: true);

            Assert.That(db.Schemas["dbo"]?.UserDefinedDataTypes["PostalCode"], Is.Null, "PostalCode should be gone after delete");
            Assert.That(db.Schemas["dbo"]?.UserDefinedDataTypes["PhoneNumber"], Is.Not.Null, "PhoneNumber should be unaffected by delete");
        }

        [Test]
        public void ModifyUserDefinedDataType_StillAccessibleAfterUpdate_OtherTypeUnchanged()
        {
            using var model = BuildModel(
                ("CREATE TYPE [dbo].[PhoneNumber] FROM NVARCHAR(20) NOT NULL", Source1),
                ("CREATE TYPE [dbo].[PostalCode] FROM NVARCHAR(10) NOT NULL", Source2));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            _ = db.Schemas["dbo"]?.UserDefinedDataTypes["PhoneNumber"];
            _ = db.Schemas["dbo"]?.UserDefinedDataTypes["PostalCode"];

            model.AddOrUpdateObjects("CREATE TYPE [dbo].[PhoneNumber] FROM NVARCHAR(30) NOT NULL", Source1, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source1, deleted: false);

            Assert.That(db.Schemas["dbo"]?.UserDefinedDataTypes["PhoneNumber"], Is.Not.Null, "PhoneNumber should still exist after modify");
            Assert.That(db.Schemas["dbo"]?.UserDefinedDataTypes["PostalCode"], Is.Not.Null, "PostalCode should be unaffected by modify");
        }

        // ── User-Defined Table Types ──────────────────────────────────────────────

        [Test]
        public void AddUserDefinedTableType_AppearsInSchema_OtherTypeUntouched()
        {
            using var model = BuildModel(
                ("CREATE TYPE [dbo].[IdTable] AS TABLE (Id INT PRIMARY KEY)", Source1));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            Assert.That(db.Schemas["dbo"]?.UserDefinedTableTypes["IdTable"], Is.Not.Null, "IdTable should exist before add");
            Assert.That(db.Schemas["dbo"]?.UserDefinedTableTypes["NameTable"], Is.Null, "NameTable should not exist before add");

            model.AddOrUpdateObjects("CREATE TYPE [dbo].[NameTable] AS TABLE (Name NVARCHAR(100))", Source2, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source2, deleted: false);

            Assert.That(db.Schemas["dbo"]?.UserDefinedTableTypes["NameTable"], Is.Not.Null, "NameTable should appear after add");
            Assert.That(db.Schemas["dbo"]?.UserDefinedTableTypes["IdTable"], Is.Not.Null, "IdTable should still exist after add");
        }

        [Test]
        public void DeleteUserDefinedTableType_DisappearsFromSchema_OtherTypeUntouched()
        {
            using var model = BuildModel(
                ("CREATE TYPE [dbo].[IdTable] AS TABLE (Id INT PRIMARY KEY)", Source1),
                ("CREATE TYPE [dbo].[NameTable] AS TABLE (Name NVARCHAR(100))", Source2));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            Assert.That(db.Schemas["dbo"]?.UserDefinedTableTypes["IdTable"], Is.Not.Null, "IdTable before delete");
            Assert.That(db.Schemas["dbo"]?.UserDefinedTableTypes["NameTable"], Is.Not.Null, "NameTable before delete");

            model.DeleteObjects(Source2);
            provider.UpdateForFileChange(Source2, deleted: true);

            Assert.That(db.Schemas["dbo"]?.UserDefinedTableTypes["NameTable"], Is.Null, "NameTable should be gone after delete");
            Assert.That(db.Schemas["dbo"]?.UserDefinedTableTypes["IdTable"], Is.Not.Null, "IdTable should be unaffected by delete");
        }

        [Test]
        public void ModifyUserDefinedTableType_StillAccessibleAfterUpdate_OtherTypeUnchanged()
        {
            using var model = BuildModel(
                ("CREATE TYPE [dbo].[IdTable] AS TABLE (Id INT PRIMARY KEY)", Source1),
                ("CREATE TYPE [dbo].[NameTable] AS TABLE (Name NVARCHAR(100))", Source2));

            var provider = new TSqlModelMetadataProvider(model, DbName);
            var db = provider.Server.Databases[DbName];

            _ = db.Schemas["dbo"]?.UserDefinedTableTypes["IdTable"];
            _ = db.Schemas["dbo"]?.UserDefinedTableTypes["NameTable"];

            model.AddOrUpdateObjects("CREATE TYPE [dbo].[IdTable] AS TABLE (Id INT PRIMARY KEY, Code NVARCHAR(10))", Source1, new TSqlObjectOptions());
            provider.UpdateForFileChange(Source1, deleted: false);

            Assert.That(db.Schemas["dbo"]?.UserDefinedTableTypes["IdTable"], Is.Not.Null, "IdTable should still exist after modify");
            Assert.That(db.Schemas["dbo"]?.UserDefinedTableTypes["NameTable"], Is.Not.Null, "NameTable should be unaffected by modify");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Find All References (textDocument/references) tests
    // Verifies HandleReferencesRequest, FindTokenLocationsInFile, and the
    // TSqlModelMetadataProvider helper methods FindObject / GetReferencingFilePaths.
    // ────────────────────────────────────────────────────────────────────────
    [TestFixture]
    public class FindReferencesTests
    {
        private string _projectPath;
        private SqlProject _project;
        private TSqlModel _model;
        private LangService _langService;
        private WorkspaceService<SqlToolsSettings> _workspaceService;
        private string _projectUri;
        private string _contextKey;
        private string _databaseName;
        private string _projectDir;

        // SQL contents — no leading blank line so 0-based LSP line numbers match array indices.
        //
        // GetCustomerScript line layout (0-based):
        //   0: "CREATE PROCEDURE dbo.GetCustomer"
        //   1: "    @CustomerId INT"
        //   2: "AS"
        //   3: "BEGIN"
        //   4: "    SELECT CustomerId, CustomerName"
        //   5: "    FROM dbo.Customers"   ← "Customers" occupies chars 13-21
        //   6: "    WHERE CustomerId = @CustomerId;"
        //   7: "END"
        private const string TableScript =
            "CREATE TABLE dbo.Customers (\n" +
            "    CustomerId INT PRIMARY KEY,\n" +
            "    CustomerName NVARCHAR(100) NOT NULL\n" +
            ");";

        private const string GetCustomerScript =
            "CREATE PROCEDURE dbo.GetCustomer\n" +
            "    @CustomerId INT\n" +
            "AS\n" +
            "BEGIN\n" +
            "    SELECT CustomerId, CustomerName\n" +
            "    FROM dbo.Customers\n" +
            "    WHERE CustomerId = @CustomerId;\n" +
            "END";

        // ListCustomersScript uses an unqualified reference so the tests exercise the
        // DacFx model lookup fallback path (no preceding ".") that resolves by last name part
        // (e.g. bare "Customers" → "dbo.Customers" via FindQualifiedNameByLastPart).
        //
        // ListCustomersScript line layout (0-based):
        //   0: "CREATE PROCEDURE dbo.ListCustomers"
        //   1: "AS"
        //   2: "BEGIN"
        //   3: "    SELECT CustomerId, CustomerName"
        //   4: "    FROM Customers;"  ← bare name; "Customers" at chars 9-17
        //   5: "END"
        private const string ListCustomersScript =
            "CREATE PROCEDURE dbo.ListCustomers\n" +
            "AS\n" +
            "BEGIN\n" +
            "    SELECT CustomerId, CustomerName\n" +
            "    FROM Customers;\n" +
            "END";

        // Orders table is intentionally never referenced by any stored procedure.
        // Used by the isolation test.
        // Line 0: "CREATE TABLE dbo.Orders ("  — "Orders" starts at char 17.
        private const string OrdersScript =
            "CREATE TABLE dbo.Orders (\n" +
            "    OrderId INT PRIMARY KEY\n" +
            ");"; 

        [SetUp]
        public void SetUp()
        {
            _projectPath = ProjectUtils.CreateTestProject("FindReferencesTestProject");
            _project = SqlProject.OpenProject(_projectPath);
            _projectDir = Path.GetDirectoryName(_projectPath);

            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("Tables", "Customers.sql")), TableScript);
            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("StoredProcedures", "GetCustomer.sql")), GetCustomerScript);
            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("StoredProcedures", "ListCustomers.sql")), ListCustomersScript);
            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("Tables", "Orders.sql")), OrdersScript);

            _model = TSqlModelBuilder.LoadModel(_project);
            _databaseName = Path.GetFileNameWithoutExtension(_projectPath);

            var metadataProvider = new TSqlModelMetadataProvider(_model, _databaseName);
            var parseOptions = new ParseOptions(
                batchSeparator: "GO",
                isQuotedIdentifierSet: true,
                compatibilityLevel: DatabaseCompatibilityLevel.Current,
                transactSqlVersion: TransactSqlVersion.Current);

            _langService = new LangService();
            _workspaceService = new WorkspaceService<SqlToolsSettings>();
            _workspaceService.Workspace = new Microsoft.SqlTools.LanguageService.Workspace.Workspace();
            _langService.WorkspaceServiceInstance = _workspaceService;

            _projectUri = new Uri(_projectPath).AbsoluteUri;
            _contextKey = $"project_{_projectUri}";

            _langService.UpdateLanguageServiceOnProjectOpen(
                _projectUri, metadataProvider, parseOptions, _databaseName)
                .GetAwaiter().GetResult();
        }

        [TearDown]
        public void TearDown()
        {
            _model?.Dispose();
            ProjectUtils.DeleteTestProject(_projectPath);
        }

        private string GetFileUri(string relativeSlashPath)
        {
            // Split on '/' so Path.Combine inserts the correct separator on all platforms.
            string abs = Path.Combine(_projectDir, Path.Combine(relativeSlashPath.Split('/')));
            return new Uri(abs).AbsoluteUri;
        }

        /// <summary>
        /// Loads all four SQL files (Customers.sql, Orders.sql, GetCustomer.sql, ListCustomers.sql)
        /// into the workspace and stamps them with the project context via
        /// <see cref="LanguageService.InitializeProjectFileContexts"/>. ParseResult is left null
        /// so individual tests can verify on-demand parsing behaviour.
        /// </summary>
        private void LoadAllFilesIntoWorkspace()
        {
            var entries = new[]
            {
                (Path: "Tables/Customers.sql",              Content: TableScript),
                (Path: "Tables/Orders.sql",                  Content: OrdersScript),
                (Path: "StoredProcedures/GetCustomer.sql",  Content: GetCustomerScript),
                (Path: "StoredProcedures/ListCustomers.sql", Content: ListCustomersScript),
            };

            var fileUris = System.Array.ConvertAll(entries, e =>
            {
                string uri = GetFileUri(e.Path);
                _workspaceService.Workspace.GetFileBuffer(uri, e.Content);
                return uri;
            });

            _langService.InitializeProjectFileContexts(fileUris, _contextKey, _databaseName);
        }

        // ── Guard: IntelliSense disabled ─────────────────────────────────────

        [Test]
        public async Task HandleReferencesRequest_ReturnsEmpty_WhenIntellisenseDisabled()
        {
            _langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = false;
            LoadAllFilesIntoWorkspace();

            WsLocation[] result = null;
            var ctx = new Mock<RequestContext<WsLocation[]>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<WsLocation[]>()))
               .Returns<WsLocation[]>(r => { result = r; return Task.FromResult(0); });

            await _langService.HandleReferencesRequest(
                new ReferencesParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = GetFileUri("StoredProcedures/GetCustomer.sql") },
                    Position = new Position { Line = 5, Character = 15 }
                },
                ctx.Object);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty, "Should return empty when IntelliSense is disabled");
        }

        // ── Guard: file not in workspace ─────────────────────────────────────

        [Test]
        public async Task HandleReferencesRequest_ReturnsEmpty_WhenFileNotFound()
        {
            WsLocation[] result = null;
            var ctx = new Mock<RequestContext<WsLocation[]>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<WsLocation[]>()))
               .Returns<WsLocation[]>(r => { result = r; return Task.FromResult(0); });

            await _langService.HandleReferencesRequest(
                new ReferencesParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = "file:///no/such/file.sql" },
                    Position = new Position { Line = 0, Character = 0 }
                },
                ctx.Object);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty, "Unknown file should return empty");
        }

        // ── Guard: file exists in workspace but not marked as a project file ─

        [Test]
        public async Task HandleReferencesRequest_ReturnsEmpty_ForNonProjectFile()
        {
            const string queryUri = "file:///test_non_project_refs.sql";
            _workspaceService.Workspace.GetFileBuffer(queryUri, "SELECT * FROM dbo.Customers");
            // Intentionally NOT calling InitializeProjectFileContexts → IsProject stays false.

            WsLocation[] result = null;
            var ctx = new Mock<RequestContext<WsLocation[]>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<WsLocation[]>()))
               .Returns<WsLocation[]>(r => { result = r; return Task.FromResult(0); });

            await _langService.HandleReferencesRequest(
                new ReferencesParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = queryUri },
                    Position = new Position { Line = 0, Character = 20 }
                },
                ctx.Object);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty, "Non-project file should return empty");
        }

        // ── Main integration: references returned across all project files ────

        [Test]
        public async Task HandleReferencesRequest_FindsReferencesAcrossAllProjectFiles()
        {
            LoadAllFilesIntoWorkspace();

            // Cursor on "Customers" in line 5 of GetCustomer.sql:
            //   "    FROM dbo.Customers"  — char 15 is inside the word (starts at char 13).
            WsLocation[] result = null;
            var ctx = new Mock<RequestContext<WsLocation[]>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<WsLocation[]>()))
               .Returns<WsLocation[]>(r => { result = r; return Task.FromResult(0); });

            await _langService.HandleReferencesRequest(
                new ReferencesParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = GetFileUri("StoredProcedures/GetCustomer.sql") },
                    Position = new Position { Line = 5, Character = 15 }
                },
                ctx.Object);

            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Is.Not.Empty, "Should find at least one reference");

            var files = result.Select(l => l.Uri).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            Assert.That(files.Any(f => f.Contains("GetCustomer.sql")), Is.True,
                $"GetCustomer.sql should appear in results. Found: {string.Join(", ", files)}");
            Assert.That(files.Any(f => f.Contains("ListCustomers.sql")), Is.True,
                $"ListCustomers.sql should appear in results. Found: {string.Join(", ", files)}");
            Assert.That(files.Any(f => f.Contains("Customers.sql") && !f.Contains("GetCustomer") && !f.Contains("ListCustomers")), Is.True,
                $"Customers.sql (table definition) should appear in results. Found: {string.Join(", ", files)}");
        }

        [Test]
        public async Task HandleReferencesRequest_FindsReferences_UnqualifiedName()
        {
            LoadAllFilesIntoWorkspace();

            // Cursor on bare "Customers" in ListCustomers.sql line 4: "    FROM Customers;"
            // GetPrecedingSchemaPrefix returns null → FindQualifiedNameByLastPart("Customers")
            // resolves to "dbo.Customers" directly from the DacFx model.
            WsLocation[] result = null;
            var ctx = new Mock<RequestContext<WsLocation[]>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<WsLocation[]>()))
               .Returns<WsLocation[]>(r => { result = r; return Task.FromResult(0); });

            await _langService.HandleReferencesRequest(
                new ReferencesParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = GetFileUri("StoredProcedures/ListCustomers.sql") },
                    Position = new Position { Line = 4, Character = 12 }  // inside "Customers" (chars 9-17)
                },
                ctx.Object);

            Assert.That(result, Is.Not.Null, "Unqualified-name result should not be null");
            Assert.That(result, Is.Not.Empty, "Unqualified name should find references via DacFx model lookup fallback");
            var files = result.Select(l => l.Uri).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Assert.That(files.Any(f => f.Contains("Customers.sql") && !f.Contains("GetCustomer") && !f.Contains("ListCustomers")), Is.True,
                $"Customers.sql (table definition) should appear for unqualified name. Found: {string.Join(", ", files)}");
        }

        // ── Unopened files: null ParseResult is populated on demand ───────────

        [Test]
        public async Task HandleReferencesRequest_ParsesUnopenedFiles_OnDemand()
        {
            // Load files into workspace without calling ParseAndBind.
            // InitializeProjectFileContexts leaves ParseResult = null for all files.
            LoadAllFilesIntoWorkspace();

            string listCustomersUri = GetFileUri("StoredProcedures/ListCustomers.sql");
            var listParseInfo = _langService.GetScriptParseInfo(listCustomersUri);
            Assert.That(listParseInfo, Is.Not.Null, "ParseInfo should exist for ListCustomers.sql after InitializeProjectFileContexts");
            Assert.That(listParseInfo.ParseResult, Is.Null, "ParseResult should be null before HandleReferencesRequest");

            WsLocation[] result = null;
            var ctx = new Mock<RequestContext<WsLocation[]>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<WsLocation[]>()))
               .Returns<WsLocation[]>(r => { result = r; return Task.FromResult(0); });

            await _langService.HandleReferencesRequest(
                new ReferencesParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = GetFileUri("StoredProcedures/GetCustomer.sql") },
                    Position = new Position { Line = 5, Character = 15 }
                },
                ctx.Object);

            Assert.That(result, Is.Not.Null);

            // ListCustomers.sql must appear even though it had null ParseResult initially.
            var files = result.Select(l => l.Uri).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Assert.That(files.Any(f => f.Contains("ListCustomers.sql")), Is.True,
                $"ListCustomers.sql should appear even if ParseResult was initially null. Found: {string.Join(", ", files)}");

            // FindTokenLocationsInFile parses the file from disk into a local ParseResult without
            // mutating shared state, so the cached ScriptParseInfo.ParseResult stays null.
            Assert.That(listParseInfo.ParseResult, Is.Null,
                "On-demand disk parse should not mutate the shared ScriptParseInfo.ParseResult");
        }

        // ── Isolation: unrelated tables must not appear in each other's results ─

        [Test]
        public async Task HandleReferencesRequest_DoesNotReturnUnrelatedFiles()
        {
            LoadAllFilesIntoWorkspace();

            var ctx1 = new Mock<RequestContext<WsLocation[]>>();
            WsLocation[] customersResult = null;
            ctx1.Setup(rc => rc.SendResult(It.IsAny<WsLocation[]>()))
               .Returns<WsLocation[]>(r => { customersResult = r; return Task.FromResult(0); });

            // Find References on "Customers" in GetCustomer.sql — Orders.sql must NOT appear.
            await _langService.HandleReferencesRequest(
                new ReferencesParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = GetFileUri("StoredProcedures/GetCustomer.sql") },
                    Position = new Position { Line = 5, Character = 15 }
                },
                ctx1.Object);

            Assert.That(customersResult, Is.Not.Null);
            var customersFiles = customersResult.Select(l => l.Uri).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Assert.That(customersFiles.Any(f => f.Contains("Orders.sql")), Is.False,
                $"Orders.sql should NOT appear in Customers references. Found: {string.Join(", ", customersFiles)}");

            // Find References on "Orders" in Orders.sql (line 0, char 20) — Customers-related files must NOT appear.
            var ctx2 = new Mock<RequestContext<WsLocation[]>>();
            WsLocation[] ordersResult = null;
            ctx2.Setup(rc => rc.SendResult(It.IsAny<WsLocation[]>()))
               .Returns<WsLocation[]>(r => { ordersResult = r; return Task.FromResult(0); });

            await _langService.HandleReferencesRequest(
                new ReferencesParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = GetFileUri("Tables/Orders.sql") },
                    Position = new Position { Line = 0, Character = 20 }
                },
                ctx2.Object);

            Assert.That(ordersResult, Is.Not.Null);
            var ordersFiles = ordersResult.Select(l => l.Uri).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Assert.That(ordersFiles.Any(f => f.Contains("GetCustomer.sql") || f.Contains("ListCustomers.sql")), Is.False,
                $"Customers SPs should NOT appear in Orders references. Found: {string.Join(", ", ordersFiles)}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Rename (textDocument/rename) tests
    // Verifies HandleRenameRequest builds a WorkspaceEdit from the same
    // FindProjectSymbolLocations helper that powers Find All References.
    // ────────────────────────────────────────────────────────────────────────
    [TestFixture]
    public class RenameTests
    {
        private string _projectPath;
        private SqlProject _project;
        private TSqlModel _model;
        private LangService _langService;
        private WorkspaceService<SqlToolsSettings> _workspaceService;
        private string _projectUri;
        private string _contextKey;
        private string _databaseName;
        private string _projectDir;

        // Same SQL scripts as FindReferencesTests — identical line/column layout.
        private const string TableScript =
            "CREATE TABLE dbo.Customers (\n" +
            "    CustomerId INT PRIMARY KEY,\n" +
            "    CustomerName NVARCHAR(100) NOT NULL\n" +
            ");";

        private const string GetCustomerScript =
            "CREATE PROCEDURE dbo.GetCustomer\n" +
            "    @CustomerId INT\n" +
            "AS\n" +
            "BEGIN\n" +
            "    SELECT CustomerId, CustomerName\n" +
            "    FROM dbo.Customers\n" +       // line 5, "Customers" starts at char 13
            "    WHERE CustomerId = @CustomerId;\n" +
            "END";

        private const string ListCustomersScript =
            "CREATE PROCEDURE dbo.ListCustomers\n" +
            "AS\n" +
            "BEGIN\n" +
            "    SELECT CustomerId, CustomerName\n" +
            "    FROM Customers;\n" +          // line 4, bare "Customers" starts at char 9
            "END";

        private const string OrdersScript =
            "CREATE TABLE dbo.Orders (\n" +
            "    OrderId INT PRIMARY KEY\n" +
            ");";

        // Uses bracket-quoted [Customers] so the bracket-quoting preservation logic fires.
        // Line 3: "    SELECT * FROM dbo.[Customers];"  — '[' at char 21, ']' at char 31.
        private const string BracketedReferenceScript =
            "CREATE PROCEDURE dbo.GetBracketed\n" +  // line 0
            "AS\n" +                                  // line 1
            "BEGIN\n" +                               // line 2
            "    SELECT * FROM dbo.[Customers];\n" +  // line 3 — [Customers] starts at char 21
            "END";

        private const string MemoryOptimizedTableScript =
            "CREATE TABLE dbo.MemOptTable (\n" +
            "    Id INT NOT NULL PRIMARY KEY NONCLUSTERED HASH WITH (BUCKET_COUNT = 1000000),\n" +
            "    Value NVARCHAR(100)\n" +
            ") WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);";

        private const string SalesSchemaScript = "CREATE SCHEMA sales;";

        private const string StagingSchemaScript = "CREATE SCHEMA staging;";

        private const string SalesCustomersTableScript =
            "CREATE TABLE sales.Customers (\n" +
            "    CustomerId INT PRIMARY KEY,\n" +
            "    Region NVARCHAR(50)\n" +
            ");";  

        [SetUp]
        public void SetUp()
        {
            _projectPath = ProjectUtils.CreateTestProject("RenameTestProject");
            _project = SqlProject.OpenProject(_projectPath);
            _projectDir = Path.GetDirectoryName(_projectPath);

            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("Tables", "Customers.sql")), TableScript);
            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("StoredProcedures", "GetCustomer.sql")), GetCustomerScript);
            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("StoredProcedures", "ListCustomers.sql")), ListCustomersScript);
            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("Tables", "Orders.sql")), OrdersScript);
            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("StoredProcedures", "GetBracketed.sql")), BracketedReferenceScript);
            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("Tables", "MemOptTable.sql")), MemoryOptimizedTableScript);
            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("Schemas", "sales.sql")), SalesSchemaScript);
            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("Tables", "SalesCustomers.sql")), SalesCustomersTableScript);
            // 'staging' schema exists in the model but has no Customers table — used as the
            // collision-free target for the happy-path move test so that sales.Customers in the
            // model doesn't trigger the name-collision guard.
            _project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("Schemas", "staging.sql")), StagingSchemaScript);

            _model = TSqlModelBuilder.LoadModel(_project);
            _databaseName = Path.GetFileNameWithoutExtension(_projectPath);

            var metadataProvider = new TSqlModelMetadataProvider(_model, _databaseName);
            var parseOptions = new ParseOptions(
                batchSeparator: "GO",
                isQuotedIdentifierSet: true,
                compatibilityLevel: DatabaseCompatibilityLevel.Current,
                transactSqlVersion: TransactSqlVersion.Current);

            _langService = new LangService();
            _workspaceService = new WorkspaceService<SqlToolsSettings>();
            _workspaceService.Workspace = new Microsoft.SqlTools.LanguageService.Workspace.Workspace();
            _langService.WorkspaceServiceInstance = _workspaceService;

            _projectUri = new Uri(_projectPath).AbsoluteUri;
            _contextKey = $"project_{_projectUri}";

            _langService.UpdateLanguageServiceOnProjectOpen(
                _projectUri, metadataProvider, parseOptions, _databaseName)
                .GetAwaiter().GetResult();
        }

        [TearDown]
        public void TearDown()
        {
            _model?.Dispose();
            ProjectUtils.DeleteTestProject(_projectPath);
        }

        private string GetFileUri(string relativeSlashPath)
        {
            string abs = Path.Combine(_projectDir, Path.Combine(relativeSlashPath.Split('/')));
            return new Uri(abs).AbsoluteUri;
        }

        private void LoadAllFilesIntoWorkspace(bool includeMemOptTable = false, bool includeSalesSchema = false)
        {
            var entriesList = new System.Collections.Generic.List<(string Path, string Content)>
            {
                (Path: "Tables/Customers.sql",               Content: TableScript),
                (Path: "Tables/Orders.sql",                   Content: OrdersScript),
                (Path: "StoredProcedures/GetCustomer.sql",   Content: GetCustomerScript),
                (Path: "StoredProcedures/ListCustomers.sql", Content: ListCustomersScript),
            };
            
            if (includeMemOptTable)
            {
                entriesList.Add((Path: "Tables/MemOptTable.sql", Content: MemoryOptimizedTableScript));
            }
            
            if (includeSalesSchema)
            {
                entriesList.Add((Path: "Schemas/sales.sql", Content: SalesSchemaScript));
                entriesList.Add((Path: "Tables/SalesCustomers.sql", Content: SalesCustomersTableScript));
            }
            
            var entries = entriesList.ToArray();

            var fileUris = System.Array.ConvertAll(entries, e =>
            {
                string uri = GetFileUri(e.Path);
                _workspaceService.Workspace.GetFileBuffer(uri, e.Content);
                return uri;
            });

            _langService.InitializeProjectFileContexts(fileUris, _contextKey, _databaseName);
        }

        // ── Guard: non-project file returns null WorkspaceEdit ───────────────
        // The three null-return paths (IntelliSense disabled, file not found, non-project file)
        // all flow through FindProjectSymbolLocations, which is already exhaustively covered by
        // the FindReferencesTests guards above. One representative case is sufficient here.

        [Test]
        public async Task HandleRenameRequest_ReturnsNull_ForNonProjectFile()
        {
            const string queryUri = "file:///test_non_project_rename.sql";
            _workspaceService.Workspace.GetFileBuffer(queryUri, "SELECT * FROM dbo.Customers");
            // Intentionally NOT calling InitializeProjectFileContexts → IsProject stays false.

            SqlSymbolRenameResponse result = null;
            bool resultSent = false;
            var ctx = new Mock<RequestContext<SqlSymbolRenameResponse>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<SqlSymbolRenameResponse>()))
               .Returns<SqlSymbolRenameResponse>(r => { result = r; resultSent = true; return Task.FromResult(0); });

            await _langService.HandleSqlRenameRequest(
                new SqlSymbolRenameParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = queryUri },
                    Position = new Position { Line = 0, Character = 20 },
                    NewName = "dbo.NewName"
                },
                ctx.Object);

            Assert.That(resultSent, Is.True, "SendResult should have been called");
            Assert.That(result?.Changes, Is.Null, "Non-project file should return null Changes");
        }

        // ── Happy path: WorkspaceEdit covers all referencing files, every edit uses the new name ─

        [Test]
        public async Task HandleRenameRequest_ProducesWorkspaceEditAcrossAllProjectFiles()
        {
            LoadAllFilesIntoWorkspace();
            const string newName = "Clients";

            SqlSymbolRenameResponse result = null;
            var ctx = new Mock<RequestContext<SqlSymbolRenameResponse>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<SqlSymbolRenameResponse>()))
               .Returns<SqlSymbolRenameResponse>(r => { result = r; return Task.FromResult(0); });

            // Cursor on "Customers" in line 5 of GetCustomer.sql:
            //   "    FROM dbo.Customers"  — char 15 is inside "Customers" (starts at char 13).
            await _langService.HandleSqlRenameRequest(
                new SqlSymbolRenameParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = GetFileUri("StoredProcedures/GetCustomer.sql") },
                    Position = new Position { Line = 5, Character = 15 },
                    NewName = newName
                },
                ctx.Object);

            Assert.That(result, Is.Not.Null, "SqlSymbolRenameResponse should not be null");
            Assert.That(result.Changes, Is.Not.Null.And.Not.Empty, "Changes should contain at least one file");

            var files = result.Changes.Keys.ToList();
            Assert.That(files.Any(f => f.Contains("GetCustomer.sql")), Is.True,
                $"GetCustomer.sql should be in the edit set. Keys: {string.Join(", ", files)}");
            Assert.That(files.Any(f => f.Contains("ListCustomers.sql")), Is.True,
                $"ListCustomers.sql should be in the edit set. Keys: {string.Join(", ", files)}");
            Assert.That(files.Any(f => f.Contains("Customers.sql") && !f.Contains("GetCustomer") && !f.Contains("ListCustomers")), Is.True,
                $"Customers.sql (table definition) should be in the edit set. Keys: {string.Join(", ", files)}");

            // Every edit renames to the new name. The bracket-quoted occurrence in GetBracketed.sql
            // (a project file scanned from disk) is preserved as [Clients]; all others are plain.
            var allEdits = result.Changes.Values.SelectMany(e => e).ToList();
            Assert.That(allEdits.All(e => e.NewText == newName || e.NewText == $"[{newName}]"), Is.True,
                $"Every TextEdit.NewText should equal '{newName}' or '[{newName}]'. Found: {string.Join(", ", allEdits.Select(e => e.NewText).Distinct())}");
        }

        // ── Isolation: unrelated files must not appear in the edit set ──────────

        [Test]
        public async Task HandleRenameRequest_DoesNotIncludeUnrelatedFiles()
        {
            LoadAllFilesIntoWorkspace();

            SqlSymbolRenameResponse result = null;
            var ctx = new Mock<RequestContext<SqlSymbolRenameResponse>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<SqlSymbolRenameResponse>()))
               .Returns<SqlSymbolRenameResponse>(r => { result = r; return Task.FromResult(0); });

            // Rename "Customers" — Orders.sql references a completely different table.
            await _langService.HandleSqlRenameRequest(
                new SqlSymbolRenameParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = GetFileUri("StoredProcedures/GetCustomer.sql") },
                    Position = new Position { Line = 5, Character = 15 },
                    NewName = "Clients"
                },
                ctx.Object);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Changes, Is.Not.Null);
            Assert.That(result.Changes.Keys.Any(f => f.Contains("Orders.sql")), Is.False,
                $"Orders.sql should NOT appear when renaming Customers. Keys: {string.Join(", ", result.Changes.Keys)}");
        }

        // ── Bracket-quoting preservation ─────────────────────────────────────
        // When the original token is bracket-quoted (e.g. [Customers]), the rename edit for
        // that occurrence must also be bracket-quoted ([Clients]), while unbracketed occurrences
        // (dbo.Customers, bare Customers) keep plain text.

        [Test]
        public async Task HandleRenameRequest_PreservesBracketQuoting()
        {
            LoadAllFilesIntoWorkspace();
            const string newName = "Clients";

            SqlSymbolRenameResponse result = null;
            var ctx = new Mock<RequestContext<SqlSymbolRenameResponse>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<SqlSymbolRenameResponse>()))
               .Returns<SqlSymbolRenameResponse>(r => { result = r; return Task.FromResult(0); });

            // Load GetBracketed.sql into the workspace (not in LoadAllFilesIntoWorkspace to avoid
            // affecting other tests that assert all NewText values are unbracketed).
            string bracketedUri = GetFileUri("StoredProcedures/GetBracketed.sql");
            _workspaceService.Workspace.GetFileBuffer(bracketedUri, BracketedReferenceScript);
            _langService.InitializeProjectFileContexts(new[] { bracketedUri }, _contextKey, _databaseName);

            // Cursor on [Customers] in GetBracketed.sql line 3:
            //   "    SELECT * FROM dbo.[Customers];"
            // '[' is at char 22, 'C' is at char 23 — place cursor on 'C' (inside the bracket token).
            await _langService.HandleSqlRenameRequest(
                new SqlSymbolRenameParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = bracketedUri },
                    Position = new Position { Line = 3, Character = 23 },
                    NewName = newName
                },
                ctx.Object);

            string bracketedFileUri = bracketedUri;

            Assert.That(result, Is.Not.Null, "SqlSymbolRenameResponse should not be null");
            Assert.That(result.Changes, Is.Not.Null.And.Not.Empty, "Changes should not be empty");

            // GetBracketed.sql uses [Customers] — that edit must be bracket-wrapped.
            Assert.That(result.Changes.ContainsKey(bracketedFileUri), Is.True,
                "GetBracketed.sql should be in the edit set");
            var bracketedEdits = result.Changes[bracketedFileUri];
            Assert.That(bracketedEdits.Any(e => e.NewText == $"[{newName}]"), Is.True,
                $"The bracketed occurrence must be renamed to [{newName}], but got: " +
                string.Join(", ", bracketedEdits.Select(e => e.NewText)));

            // All other files use unbracketed identifiers — those edits must NOT be bracket-wrapped.
            foreach (var kvp in result.Changes)
            {
                if (string.Equals(kvp.Key, bracketedFileUri, StringComparison.OrdinalIgnoreCase))
                    continue;
                Assert.That(kvp.Value.All(e => e.NewText == newName), Is.True,
                    $"Unbracketed occurrences in {kvp.Key} should be renamed to plain '{newName}', " +
                    $"but got: {string.Join(", ", kvp.Value.Select(e => e.NewText))}");
            }
        }

        // ── Refactorlog content generation ───────────────────────────────────
        // The rename response carries the full .refactorlog document with the new Rename Refactor
        // operation appended. Verify that supplying ExistingRefactorLogContent produces a valid
        // document whose appended operation has the expected ElementType/ElementName/NewName.

        [Test]
        public async Task HandleRenameRequest_AppendsRenameOperationToExistingRefactorLog()
        {
            LoadAllFilesIntoWorkspace();
            const string newName = "Clients";

            XNamespace ns = "http://schemas.microsoft.com/sqlserver/dac/Serialization/2012/02";
            // A pre-existing .refactorlog with one unrelated operation already recorded.
            var existingDoc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(ns + "Operations",
                    new XAttribute("Version", "1.0"),
                    new XElement(ns + "Operation",
                        new XAttribute("Name", "Rename Refactor"),
                        new XAttribute("Key", Guid.NewGuid().ToString()),
                        new XAttribute("ChangeDateTime", "01/01/2020 00:00:00"),
                        new XElement(ns + "Property",
                            new XAttribute("Name", "ElementName"),
                            new XAttribute("Value", "[dbo].[Orders]")))));
            string existingRefactorLog = existingDoc.Declaration + Environment.NewLine + existingDoc.ToString();

            SqlSymbolRenameResponse result = null;
            var ctx = new Mock<RequestContext<SqlSymbolRenameResponse>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<SqlSymbolRenameResponse>()))
               .Returns<SqlSymbolRenameResponse>(r => { result = r; return Task.FromResult(0); });

            // Cursor on "Customers" in GetCustomer.sql line 5: "    FROM dbo.Customers".
            await _langService.HandleSqlRenameRequest(
                new SqlSymbolRenameParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = GetFileUri("StoredProcedures/GetCustomer.sql") },
                    Position = new Position { Line = 5, Character = 15 },
                    NewName = newName,
                    ExistingRefactorLogContent = existingRefactorLog
                },
                ctx.Object);

            Assert.That(result, Is.Not.Null, "SqlSymbolRenameResponse should not be null");
            Assert.That(result.NewName, Is.EqualTo(newName));
            Assert.That(result.RefactorLogContent, Is.Not.Null.And.Not.Empty,
                "RefactorLogContent should be populated for a schema-level table rename");

            // The returned content must be valid XML rooted at the Operations element.
            XDocument doc = XDocument.Parse(result.RefactorLogContent);
            Assert.That(doc.Root?.Name, Is.EqualTo(ns + "Operations"),
                "Refactorlog root should be the Operations element");

            var operations = doc.Root.Elements(ns + "Operation").ToList();
            Assert.That(operations.Count, Is.EqualTo(2),
                "The pre-existing operation should be preserved and the new rename appended");

            // The newly appended operation describes the Customers → Clients table rename.
            XElement appended = operations.Last();
            string PropertyValue(string name) => appended.Elements(ns + "Property")
                .FirstOrDefault(p => (string)p.Attribute("Name") == name)?.Attribute("Value")?.Value;

            Assert.That((string)appended.Attribute("Name"), Is.EqualTo("Rename Refactor"));
            Assert.That(PropertyValue("ElementType"), Is.EqualTo("SqlTable"));
            Assert.That(PropertyValue("ElementName"), Is.EqualTo("[dbo].[Customers]"));
            Assert.That(PropertyValue("ParentElementType"), Is.EqualTo("SqlSchema"));
            Assert.That(PropertyValue("ParentElementName"), Is.EqualTo("[dbo]"));
            Assert.That(PropertyValue("NewName"), Is.EqualTo(newName));
        }

        // ── TryGetRefactorInfo: refactorlog element-type resolution ──────────────
        // The client uses these fields to write a Rename Refactor operation into the
        // .refactorlog. A schema-level object resolves to its Sql* type with a SqlSchema
        // parent; a column resolves to SqlSimpleColumn with its owning SqlTable parent.

        [Test]
        public void TryGetRefactorInfo_SchemaLevelTable_ReturnsSqlTableWithSchemaParent()
        {
            var provider = new TSqlModelMetadataProvider(_model, _databaseName);

            bool resolved = provider.TryGetRefactorInfo(
                "dbo.Customers", "Customers",
                out string elementName, out string elementType,
                out string parentElementName, out string parentElementType);

            Assert.That(resolved, Is.True, "A schema-level table should resolve refactor info");
            Assert.That(elementName, Is.EqualTo("[dbo].[Customers]"));
            Assert.That(elementType, Is.EqualTo("SqlTable"));
            Assert.That(parentElementName, Is.EqualTo("[dbo]"));
            Assert.That(parentElementType, Is.EqualTo("SqlSchema"));
        }

        [Test]
        public void TryGetRefactorInfo_Column_ReturnsSqlSimpleColumnWithTableParent()
        {
            var provider = new TSqlModelMetadataProvider(_model, _databaseName);

            // "CustomerName" is a column on dbo.Customers, not a schema-level object,
            // so Case 1 misses and the column scan (Case 2) resolves it. The production call site
            // passes the resolved schema.table.column name, so exercise that qualified path here.
            bool resolved = provider.TryGetRefactorInfo(
                "dbo.Customers.CustomerName", "CustomerName",
                out string elementName, out string elementType,
                out string parentElementName, out string parentElementType);

            Assert.That(resolved, Is.True, "A table column should resolve refactor info");
            Assert.That(elementName, Is.EqualTo("[dbo].[Customers].[CustomerName]"));
            Assert.That(elementType, Is.EqualTo("SqlSimpleColumn"));
            Assert.That(parentElementName, Is.EqualTo("[dbo].[Customers]"));
            Assert.That(parentElementType, Is.EqualTo("SqlTable"));
        }

        [Test]
        public void TryGetRefactorInfo_UnknownSymbol_ReturnsFalse()
        {
            var provider = new TSqlModelMetadataProvider(_model, _databaseName);

            bool resolved = provider.TryGetRefactorInfo(
                "dbo.DoesNotExist", "DoesNotExist",
                out string elementName, out string elementType,
                out string parentElementName, out string parentElementType);

            Assert.That(resolved, Is.False, "An unknown symbol should not resolve refactor info");
            Assert.That(elementName, Is.Null);
            Assert.That(elementType, Is.Null);
            Assert.That(parentElementName, Is.Null);
            Assert.That(parentElementType, Is.Null);
        }

        // ── Move to Schema (sql/moveToSchema) ────────────────────────────────
        // HandleMoveToSchemaRequest reuses FindProjectSymbolLocations to find every reference, then
        // rewrites the schema qualifier (or inserts one for unqualified names) and appends a
        // "Move Schema" operation to the .refactorlog. These cover the guard, the happy path with
        // refactorlog generation, and the same-schema no-op.

        [Test]
        public async Task HandleMoveToSchemaRequest_ReturnsNull_ForNonProjectFile()
        {
            const string queryUri = "file:///test_non_project_move.sql";
            _workspaceService.Workspace.GetFileBuffer(queryUri, "SELECT * FROM dbo.Customers");
            // Intentionally NOT calling InitializeProjectFileContexts → IsProject stays false.

            SqlMoveToSchemaResponse result = null;
            bool resultSent = false;
            var ctx = new Mock<RequestContext<SqlMoveToSchemaResponse>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<SqlMoveToSchemaResponse>()))
               .Returns<SqlMoveToSchemaResponse>(r => { result = r; resultSent = true; return Task.FromResult(0); });

            await _langService.HandleMoveToSchemaRequest(
                new SqlMoveToSchemaParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = queryUri },
                    Position = new Position { Line = 0, Character = 20 },
                    TargetSchema = "sales"
                },
                ctx.Object);

            Assert.That(resultSent, Is.True, "SendResult should have been called");
            Assert.That(result, Is.Null, "Non-project file should return a null response");
        }

        [Test]
        public async Task HandleMoveToSchemaRequest_RewritesReferencesAndAppendsMoveSchemaOperation()
        {
            LoadAllFilesIntoWorkspace();
            // Use 'staging' as the target: it exists in the model (so IsNewSchemaExternal=False)
            // but has no Customers table, so the name-collision guard does not trigger.
            // (The model has sales.Customers, which would block a move to 'sales'.)
            const string targetSchema = "staging";

            SqlMoveToSchemaResponse result = null;
            var ctx = new Mock<RequestContext<SqlMoveToSchemaResponse>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<SqlMoveToSchemaResponse>()))
               .Returns<SqlMoveToSchemaResponse>(r => { result = r; return Task.FromResult(0); });

            // Cursor on "Customers" in GetCustomer.sql line 5: "    FROM dbo.Customers".
            await _langService.HandleMoveToSchemaRequest(
                new SqlMoveToSchemaParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = GetFileUri("StoredProcedures/GetCustomer.sql") },
                    Position = new Position { Line = 5, Character = 15 },
                    TargetSchema = targetSchema
                },
                ctx.Object);

            Assert.That(result, Is.Not.Null, "SqlMoveToSchemaResponse should not be null");
            Assert.That(result.TargetSchema, Is.EqualTo(targetSchema));
            Assert.That(result.Changes, Is.Not.Null.And.Not.Empty, "Changes should cover the referencing files");

            // The two-part reference (dbo.Customers) becomes a [staging] qualifier; the bare reference
            // (Customers in ListCustomers.sql) gets a "[staging]." qualifier inserted before it.
            var allEdits = result.Changes.Values.SelectMany(e => e).ToList();
            Assert.That(allEdits.Any(e => e.NewText == "[staging]"), Is.True,
                "A two-part reference should have its schema qualifier rewritten to [staging]");
            Assert.That(allEdits.Any(e => e.NewText == "[staging]."), Is.True,
                "An unqualified reference should have a [staging]. qualifier inserted");

            // The refactorlog carries a Move Schema operation for the moved table.
            XNamespace ns = "http://schemas.microsoft.com/sqlserver/dac/Serialization/2012/02";
            XDocument doc = XDocument.Parse(result.RefactorLogContent);
            XElement op = doc.Root.Elements(ns + "Operation").Last();
            string PropertyValue(string name) => op.Elements(ns + "Property")
                .FirstOrDefault(p => (string)p.Attribute("Name") == name)?.Attribute("Value")?.Value;

            Assert.That((string)op.Attribute("Name"), Is.EqualTo("Move Schema"));
            Assert.That(PropertyValue("ElementType"), Is.EqualTo("SqlTable"));
            Assert.That(PropertyValue("ElementName"), Is.EqualTo("[dbo].[Customers]"));
            Assert.That(PropertyValue("NewSchema"), Is.EqualTo(targetSchema));
            Assert.That(PropertyValue("IsNewSchemaExternal"), Is.EqualTo("False"));
        }

        [Test]
        public async Task HandleMoveToSchemaRequest_ReturnsNull_WhenTargetSchemaMatchesCurrent()
        {
            LoadAllFilesIntoWorkspace();

            SqlMoveToSchemaResponse result = null;
            bool resultSent = false;
            var ctx = new Mock<RequestContext<SqlMoveToSchemaResponse>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<SqlMoveToSchemaResponse>()))
               .Returns<SqlMoveToSchemaResponse>(r => { result = r; resultSent = true; return Task.FromResult(0); });

            // dbo.Customers is already in dbo — moving it to dbo is a no-op.
            await _langService.HandleMoveToSchemaRequest(
                new SqlMoveToSchemaParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = GetFileUri("StoredProcedures/GetCustomer.sql") },
                    Position = new Position { Line = 5, Character = 15 },
                    TargetSchema = "dbo"
                },
                ctx.Object);

            Assert.That(resultSent, Is.True, "SendResult should have been called");
            Assert.That(result, Is.Null, "Moving to the current schema should return a null response");
        }

        [Test]
        public async Task HandleMoveToSchemaRequest_ReturnsNull_ForMemoryOptimizedTable()
        {
            LoadAllFilesIntoWorkspace(includeMemOptTable: true);

            SqlMoveToSchemaResponse result = null;
            bool resultSent = false;
            var ctx = new Mock<RequestContext<SqlMoveToSchemaResponse>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<SqlMoveToSchemaResponse>()))
               .Returns<SqlMoveToSchemaResponse>(r => { result = r; resultSent = true; return Task.FromResult(0); });

            // Try to move memory-optimized table to another schema (cursor on "MemOptTable" in the CREATE TABLE line)
            await _langService.HandleMoveToSchemaRequest(
                new SqlMoveToSchemaParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = GetFileUri("Tables/MemOptTable.sql") },
                    Position = new Position { Line = 0, Character = 20 },
                    TargetSchema = "sales"
                },
                ctx.Object);

            Assert.That(resultSent, Is.True, "SendResult should have been called");
            Assert.That(result, Is.Null, "Memory-optimized tables should be rejected (ALTER SCHEMA TRANSFER does not support them)");
        }

        [Test]
        public async Task HandleMoveToSchemaRequest_ReturnsNull_WhenNameCollisionExists()
        {
            LoadAllFilesIntoWorkspace(includeSalesSchema: true);

            SqlMoveToSchemaResponse result = null;
            bool resultSent = false;
            var ctx = new Mock<RequestContext<SqlMoveToSchemaResponse>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<SqlMoveToSchemaResponse>()))
               .Returns<SqlMoveToSchemaResponse>(r => { result = r; resultSent = true; return Task.FromResult(0); });

            // Try to move dbo.Customers to sales schema (but sales.Customers already exists)
            // Cursor on "Customers" in GetCustomer.sql line 5: "    FROM dbo.Customers"
            await _langService.HandleMoveToSchemaRequest(
                new SqlMoveToSchemaParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = GetFileUri("StoredProcedures/GetCustomer.sql") },
                    Position = new Position { Line = 5, Character = 15 },
                    TargetSchema = "sales"
                },
                ctx.Object);

            Assert.That(resultSent, Is.True, "SendResult should have been called");
            Assert.That(result, Is.Null, "Name collision should cause the operation to be rejected");
        }

        // ── Rename rejection: specific error messages ─────────────────────────

        [Test]
        public async Task HandleRenameRequest_ReturnsNotSupportedMessage_WhenCursorOnKeyword()
        {
            LoadAllFilesIntoWorkspace();
            string fileUri = GetFileUri("StoredProcedures/GetCustomer.sql");

            SqlSymbolRenameResponse result = null;
            var ctx = new Mock<RequestContext<SqlSymbolRenameResponse>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<SqlSymbolRenameResponse>()))
               .Returns<SqlSymbolRenameResponse>(r => { result = r; return Task.FromResult(0); });

            // Line 0: "CREATE PROCEDURE dbo.GetCustomer" — char 0 is 'C' in "CREATE" (a keyword).
            await _langService.HandleSqlRenameRequest(
                new SqlSymbolRenameParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = fileUri },
                    Position = new Position { Line = 0, Character = 0 },
                    NewName = "Whatever"
                },
                ctx.Object);

            Assert.That(result?.ErrorMessage, Is.EqualTo(SR.RenameNotSupported),
                $"Expected not-supported message. Got: {result?.ErrorMessage}");
        }

        [Test]
        public async Task HandleRenameRequest_ReturnsNotSupportedMessage_WhenSymbolUnresolved()
        {
            LoadAllFilesIntoWorkspace();

            const string ghostUri = "file:///ghost.sql";
            _workspaceService.Workspace.GetFileBuffer(ghostUri, "SELECT * FROM dbo.NonExistentTable");
            _langService.InitializeProjectFileContexts(new[] { ghostUri }, _contextKey, _databaseName);

            SqlSymbolRenameResponse result = null;
            var ctx = new Mock<RequestContext<SqlSymbolRenameResponse>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<SqlSymbolRenameResponse>()))
               .Returns<SqlSymbolRenameResponse>(r => { result = r; return Task.FromResult(0); });

            // Char 22 is inside "NonExistentTable".
            await _langService.HandleSqlRenameRequest(
                new SqlSymbolRenameParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = ghostUri },
                    Position = new Position { Line = 0, Character = 22 },
                    NewName = "SomeName"
                },
                ctx.Object);

            Assert.That(result?.ErrorMessage, Is.EqualTo(SR.RenameNotSupported),
                $"Expected not-supported message. Got: {result?.ErrorMessage}");
        }

        // Cursor on the schema prefix of a qualified name (e.g. "dbo" in "dbo.Customers").
        // The FAR scan must NOT run — we should get the schema error immediately.
        [Test]
        public async Task HandleRenameRequest_ReturnsNotSupportedMessage_WhenCursorOnSchemaPrefix()
        {
            LoadAllFilesIntoWorkspace();
            string fileUri = GetFileUri("StoredProcedures/GetCustomer.sql");

            SqlSymbolRenameResponse result = null;
            var ctx = new Mock<RequestContext<SqlSymbolRenameResponse>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<SqlSymbolRenameResponse>()))
               .Returns<SqlSymbolRenameResponse>(r => { result = r; return Task.FromResult(0); });

            // Line 5 of GetCustomerScript: "    FROM dbo.Customers"
            // "dbo" starts at char 9.
            await _langService.HandleSqlRenameRequest(
                new SqlSymbolRenameParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = fileUri },
                    Position = new Position { Line = 5, Character = 10 },
                    NewName = "sales"
                },
                ctx.Object);

            Assert.That(result?.ErrorMessage, Is.EqualTo(SR.RenameNotSupported),
                $"Expected not-supported message. Got: {result?.ErrorMessage}");
        }

        // File connected to a live server must be rejected before any project-model work runs.
        [Test]
        public async Task HandleRenameRequest_ReturnsLiveServerMessage_WhenFileConnectedToServer()
        {
            const string connectedUri = "file:///test_live_server.sql";
            _workspaceService.Workspace.GetFileBuffer(connectedUri, "SELECT * FROM dbo.Customers");

            // Stamp the file as a live-connection context (IsConnected = true, IsProject = false).
            var parseInfo = new Microsoft.SqlTools.LanguageService.LanguageServices.ScriptParseInfo
            {
                BindingContextKind = BindingContextKindEnum.LiveConnection,
                ConnectionKey = "some-live-connection-key"
            };
            _langService.AddOrUpdateScriptParseInfo(connectedUri, parseInfo);

            SqlSymbolRenameResponse result = null;
            var ctx = new Mock<RequestContext<SqlSymbolRenameResponse>>();
            ctx.Setup(rc => rc.SendResult(It.IsAny<SqlSymbolRenameResponse>()))
               .Returns<SqlSymbolRenameResponse>(r => { result = r; return Task.FromResult(0); });

            await _langService.HandleSqlRenameRequest(
                new SqlSymbolRenameParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = connectedUri },
                    Position = new Position { Line = 0, Character = 20 },
                    NewName = "NewName"
                },
                ctx.Object);

            Assert.That(result?.ErrorMessage, Is.EqualTo(SR.RenameNotSupportedLiveServer),
                $"Expected live-server message. Got: {result?.ErrorMessage}");
        }

    }

    /// <summary>
    /// Focused tests for <see cref="RenameScriptDomHelper"/>, the ScriptDom-based syntactic
    /// analysis that backs rename / find-all-references. These cover the behaviour the AST visitor
    /// adds over the old token scan: bracket spans and extended-property precision.
    /// </summary>
    [TestFixture]
    public class RenameScriptDomHelperTests
    {
        private string _tempFile;

        [TearDown]
        public void TearDown()
        {
            if (_tempFile != null && File.Exists(_tempFile))
            {
                File.Delete(_tempFile);
            }
            _tempFile = null;
        }

        /// <summary>Writes <paramref name="sql"/> to a fresh temp .sql file and returns its path.</summary>
        private string WriteTempSql(string sql)
        {
            _tempFile = Path.Combine(Path.GetTempPath(), $"rename_{Guid.NewGuid():N}.sql");
            File.WriteAllText(_tempFile, sql);
            return _tempFile;
        }

        private static string MatchedText(string sql, WsLocation loc)
        {
            string[] lines = sql.Replace("\r\n", "\n").Split('\n');
            string line = lines[loc.Range.Start.Line];
            return line.Substring(loc.Range.Start.Character, loc.Range.End.Character - loc.Range.Start.Character);
        }

        [Test]
        public void FindNameLocationsInFile_BracketedIdentifier_SpanIncludesBrackets()
        {
            // The emitted span must cover the brackets so bracket-quoting can be re-applied on rename.
            const string sql = "SELECT * FROM dbo.[Customers];";
            string path = WriteTempSql(sql);

            var locations = RenameScriptDomHelper.FindNameLocationsInFile(path, "Customers").ToList();

            Assert.That(locations.Count, Is.EqualTo(1));
            Assert.That(MatchedText(sql, locations[0]), Is.EqualTo("[Customers]"));
        }

        [Test]
        public void FindNameLocationsInFile_MatchesExtendedPropertyLevelName_NotUnrelatedLiterals()
        {
            // Only the @level1name literal denotes the object; the matching @value literal and a
            // plain INSERT literal must be ignored — this precision is the key win over a token scan.
            const string sql =
                "EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'Customers', " +
                "@level0type = N'SCHEMA', @level0name = N'dbo', " +
                "@level1type = N'TABLE', @level1name = N'Customers';\n" +
                "INSERT INTO dbo.Log (Note) VALUES (N'Customers');";
            string path = WriteTempSql(sql);

            var locations = RenameScriptDomHelper.FindNameLocationsInFile(path, "Customers").ToList();

            Assert.That(locations.Count, Is.EqualTo(1), "Only the @level1name literal should match");
            // Inner text only — the N'...' quoting is left intact by the rename.
            Assert.That(MatchedText(sql, locations[0]), Is.EqualTo("Customers"));
        }

        [Test]
        public void TryResolveCursorName_DottedReference_ReturnsQualifiedName()
        {
            const string sql = "SELECT * FROM dbo.Customers;";
            // "Customers" starts at char 18 — place the cursor inside it.
            bool ok = RenameScriptDomHelper.TryResolveCursorName(sql, 0, 20, out string bare, out string qualified);

            Assert.That(ok, Is.True);
            Assert.That(bare, Is.EqualTo("Customers"));
            Assert.That(qualified, Is.EqualTo("dbo.Customers"));
        }

        // ── Schema-move edits ────────────────────────────────────────────────
        // FindSchemaMoveEditsInFile rewrites an existing schema qualifier in place, and inserts a
        // new qualifier (zero-width edit) before an otherwise-unqualified reference.

        [Test]
        public void FindSchemaMoveEditsInFile_TwoPartName_RewritesSchemaQualifier()
        {
            const string sql = "SELECT * FROM dbo.Customers;";
            string path = WriteTempSql(sql);

            var edits = RenameScriptDomHelper.FindSchemaMoveEditsInFile(path, "Customers", "dbo", "sales").ToList();

            Assert.That(edits.Count, Is.EqualTo(1));
            Assert.That(edits[0].NewText, Is.EqualTo("[sales]"));
            Assert.That(MatchedText(sql, edits[0].Location), Is.EqualTo("dbo"),
                "The replaced span should cover the existing schema qualifier");
        }

        [Test]
        public void FindSchemaMoveEditsInFile_UnqualifiedName_InsertsSchemaQualifier()
        {
            const string sql = "SELECT * FROM Customers;";
            string path = WriteTempSql(sql);

            var edits = RenameScriptDomHelper.FindSchemaMoveEditsInFile(path, "Customers", "dbo", "sales").ToList();

            Assert.That(edits.Count, Is.EqualTo(1));
            Assert.That(edits[0].NewText, Is.EqualTo("[sales]."));
            Assert.That(edits[0].Location.Range.Start.Character,
                Is.EqualTo(edits[0].Location.Range.End.Character),
                "An inserted qualifier should be a zero-width edit");
        }
    }
}