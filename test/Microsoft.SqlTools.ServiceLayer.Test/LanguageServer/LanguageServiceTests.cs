//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.LanguageServices
{
    /// <summary>
    /// Tests for the ServiceHost Language Service tests
    /// </summary>
    public class LanguageServiceTests
    {
        #region "Diagnostics tests"

        /// <summary>
        /// Verify that the latest SqlParser (2016 as of this writing) is used by default
        /// </summary>
        [Fact]
        public void LatestSqlParserIsUsedByDefault()
        {
            // This should only parse correctly on SQL server 2016 or newer
            const string sql2016Text = 
                @"CREATE SECURITY POLICY [FederatedSecurityPolicy]" + "\r\n" +
                @"ADD FILTER PREDICATE [rls].[fn_securitypredicate]([CustomerId])" + "\r\n" +   
                @"ON [dbo].[Customer];";
            
            LanguageService service = TestObjects.GetTestLanguageService();

            // parse
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents(sql2016Text);
            ScriptFileMarker[] fileMarkers = service.GetSemanticMarkers(scriptFile);

            // verify that no errors are detected
            Assert.Equal(0, fileMarkers.Length);
        }

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Fact]
        public void ParseSelectStatementWithoutErrors()
        {
            // sql statement with no errors
            const string sqlWithErrors = "SELECT * FROM sys.objects";

            // get the test service 
            LanguageService service = TestObjects.GetTestLanguageService();

            // parse the sql statement
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents(sqlWithErrors);
            ScriptFileMarker[] fileMarkers = service.GetSemanticMarkers(scriptFile);

            // verify there are no errors
            Assert.Equal(0, fileMarkers.Length);
        }

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Fact]
        public void ParseSelectStatementWithError()
        {
            // sql statement with errors
            const string sqlWithErrors = "SELECT *** FROM sys.objects";

            // get test service
            LanguageService service = TestObjects.GetTestLanguageService();

            // parse sql statement
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents(sqlWithErrors);
            ScriptFileMarker[] fileMarkers = service.GetSemanticMarkers(scriptFile);

            // verify there is one error
            Assert.Equal(1, fileMarkers.Length);

            // verify the position of the error
            Assert.Equal(9, fileMarkers[0].ScriptRegion.StartColumnNumber);
            Assert.Equal(1, fileMarkers[0].ScriptRegion.StartLineNumber);
            Assert.Equal(10, fileMarkers[0].ScriptRegion.EndColumnNumber);
            Assert.Equal(1, fileMarkers[0].ScriptRegion.EndLineNumber);
        }

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Fact]
        public void ParseMultilineSqlWithErrors()
        {
            // multiline sql with errors
            const string sqlWithErrors = 
                "SELECT *** FROM sys.objects;\n" +
                "GO\n" +
                "SELECT *** FROM sys.objects;\n";

            // get test service
            LanguageService service = TestObjects.GetTestLanguageService();

            // parse sql
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents(sqlWithErrors);
            ScriptFileMarker[] fileMarkers = service.GetSemanticMarkers(scriptFile);

            // verify there are two errors
            Assert.Equal(2, fileMarkers.Length);

            // check position of first error
            Assert.Equal(9, fileMarkers[0].ScriptRegion.StartColumnNumber);
            Assert.Equal(1, fileMarkers[0].ScriptRegion.StartLineNumber);
            Assert.Equal(10, fileMarkers[0].ScriptRegion.EndColumnNumber);
            Assert.Equal(1, fileMarkers[0].ScriptRegion.EndLineNumber);

            // check position of second error
            Assert.Equal(9, fileMarkers[1].ScriptRegion.StartColumnNumber);
            Assert.Equal(3, fileMarkers[1].ScriptRegion.StartLineNumber);
            Assert.Equal(10, fileMarkers[1].ScriptRegion.EndColumnNumber);
            Assert.Equal(3, fileMarkers[1].ScriptRegion.EndLineNumber);
        }

        #endregion

        #region "Autocomplete Tests"

        /// <summary>
        /// Creates a mock db command that returns a predefined result set
        /// </summary>
        public static DbCommand CreateTestCommand(Dictionary<string, string>[][] data)
        {
            var commandMock = new Mock<DbCommand> { CallBase = true };
            var commandMockSetup = commandMock.Protected()
                .Setup<DbDataReader>("ExecuteDbDataReader", It.IsAny<CommandBehavior>());

            commandMockSetup.Returns(new TestDbDataReader(data));

            return commandMock.Object;
        }

        /// <summary>
        /// Creates a mock db connection that returns predefined data when queried for a result set
        /// </summary>
        public DbConnection CreateMockDbConnection(Dictionary<string, string>[][] data)
        {
            var connectionMock = new Mock<DbConnection> { CallBase = true };
            connectionMock.Protected()
                .Setup<DbCommand>("CreateDbCommand")
                .Returns(CreateTestCommand(data));

            return connectionMock.Object;
        }

        /// <summary>
        /// Verify that the autocomplete service returns tables for the current connection as suggestions
        /// </summary>
        [Fact]
        public void TablesAreReturnedAsAutocompleteSuggestions()
        {
            // Result set for the query of database tables
            Dictionary<string, string>[] data =
            {
                new Dictionary<string, string> { {"name", "master" } },
                new Dictionary<string, string> { {"name", "model" } }
            };

            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>()))
                .Returns(CreateMockDbConnection(new[] {data}));

            var connectionService = new ConnectionService(mockFactory.Object);
            var autocompleteService = new AutoCompleteService();
            autocompleteService.ConnectionServiceInstance = connectionService;
            autocompleteService.InitializeService(Microsoft.SqlTools.ServiceLayer.Hosting.ServiceHost.Instance);
            
            // Open a connection
            // The cache should get updated as part of this
            ConnectParams connectionRequest = TestObjects.GetTestConnectionParams();
            var connectionResult = connectionService.Connect(connectionRequest);
            Assert.NotEmpty(connectionResult.ConnectionId);

            // Check that there is one cache created in the auto complete service
            Assert.Equal(1, autocompleteService.GetCacheCount());

            // Check that we get table suggestions for an autocomplete request
            TextDocumentPosition position = new TextDocumentPosition();
            position.Uri = connectionRequest.OwnerUri;
            position.Position = new Position();
            position.Position.Line = 1;
            position.Position.Character = 1;
            var items = autocompleteService.GetCompletionItems(position);
            Assert.Equal(2, items.Length);
            Assert.Equal("master", items[0].Label);
            Assert.Equal("model", items[1].Label);
        }

        /// <summary>
        /// Verify that only one intellisense cache is created for two documents using
        /// the autocomplete service when they share a common connection.
        /// </summary>
        [Fact]
        public void OnlyOneCacheIsCreatedForTwoDocumentsWithSameConnection()
        {
            var connectionService = new ConnectionService(TestObjects.GetTestSqlConnectionFactory());
            var autocompleteService = new AutoCompleteService();
            autocompleteService.ConnectionServiceInstance = connectionService;
            autocompleteService.InitializeService(Microsoft.SqlTools.ServiceLayer.Hosting.ServiceHost.Instance);

            // Open two connections
            ConnectParams connectionRequest1 = TestObjects.GetTestConnectionParams();
            connectionRequest1.OwnerUri = "file:///my/first/file.sql";
            ConnectParams connectionRequest2 = TestObjects.GetTestConnectionParams();
            connectionRequest2.OwnerUri = "file:///my/second/file.sql";
            var connectionResult1 = connectionService.Connect(connectionRequest1);
            Assert.NotEmpty(connectionResult1.ConnectionId);
            var connectionResult2 = connectionService.Connect(connectionRequest2);
            Assert.NotEmpty(connectionResult2.ConnectionId);

            // Verify that only one intellisense cache is created to service both URI's
            Assert.Equal(1, autocompleteService.GetCacheCount());
        }

        /// <summary>
        /// Verify that two different intellisense caches and corresponding autocomplete
        /// suggestions are provided for two documents with different connections.
        /// </summary>
        [Fact]
        public void TwoCachesAreCreatedForTwoDocumentsWithDifferentConnections()
        {
            const string testDb1 = "my_db";
            const string testDb2 = "my_other_db";

            // Result set for the query of database tables
            Dictionary<string, string>[] data1 =
            {
                new Dictionary<string, string> { {"name", "master" } },
                new Dictionary<string, string> { {"name", "model" } }
            };

            Dictionary<string, string>[] data2 =
            {
                new Dictionary<string, string> { {"name", "master" } },
                new Dictionary<string, string> { {"name", "my_table" } },
                new Dictionary<string, string> { {"name", "my_other_table" } }
            };

            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.Is<string>(x => x.Contains(testDb1))))
                .Returns(CreateMockDbConnection(new[] {data1}));
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.Is<string>(x => x.Contains(testDb2))))
                .Returns(CreateMockDbConnection(new[] {data2}));

            var connectionService = new ConnectionService(mockFactory.Object);
            var autocompleteService = new AutoCompleteService();
            autocompleteService.ConnectionServiceInstance = connectionService;
            autocompleteService.InitializeService(Microsoft.SqlTools.ServiceLayer.Hosting.ServiceHost.Instance);
            
            // Open connections
            // The cache should get updated as part of this
            ConnectParams connectionRequest = TestObjects.GetTestConnectionParams();
            connectionRequest.OwnerUri = "file:///my/first/sql/file.sql";
            connectionRequest.Connection.DatabaseName = testDb1;
            var connectionResult = connectionService.Connect(connectionRequest);
            Assert.NotEmpty(connectionResult.ConnectionId);

            // Check that there is one cache created in the auto complete service
            Assert.Equal(1, autocompleteService.GetCacheCount());

            // Open second connection
            ConnectParams connectionRequest2 = TestObjects.GetTestConnectionParams();
            connectionRequest2.OwnerUri = "file:///my/second/sql/file.sql";
            connectionRequest2.Connection.DatabaseName = testDb2;
            var connectionResult2 = connectionService.Connect(connectionRequest2);
            Assert.NotEmpty(connectionResult2.ConnectionId);

            // Check that there are now two caches in the auto complete service
            Assert.Equal(2, autocompleteService.GetCacheCount());

            // Check that we get 2 different table suggestions for autocomplete requests
            TextDocumentPosition position = new TextDocumentPosition();
            position.Uri = connectionRequest.OwnerUri;
            position.Position = new Position();
            position.Position.Line = 1;
            position.Position.Character = 1;

            var items = autocompleteService.GetCompletionItems(position);
            Assert.Equal(2, items.Length);
            Assert.Equal("master", items[0].Label);
            Assert.Equal("model", items[1].Label);

            TextDocumentPosition position2 = new TextDocumentPosition();
            position2.Uri = connectionRequest2.OwnerUri;
            position2.Position = new Position();
            position2.Position.Line = 1;
            position2.Position.Character = 1;
            
            var items2 = autocompleteService.GetCompletionItems(position2);
            Assert.Equal(3, items2.Length);
            Assert.Equal("master", items2[0].Label);
            Assert.Equal("my_table", items2[1].Label);
            Assert.Equal("my_other_table", items2[2].Label);
        }

        #endregion
    }
}

