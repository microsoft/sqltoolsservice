//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Test.QueryExecution;
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

        // This test currently requires a live database connection to initialize 
        // SMO connected metadata provider.  Since we don't want a live DB dependency
        // in the CI unit tests this scenario is currently disabled.
        //[Fact]
        public void AutoCompleteFindCompletions()
        {
            TextDocumentPosition textDocument;
            ConnectionInfo connInfo;
            ScriptFile scriptFile;
            Common.GetAutoCompleteTestObjects(out textDocument, out scriptFile, out connInfo);

            textDocument.Position.Character = 7;
            scriptFile.Contents = "select ";

            var autoCompleteService = LanguageService.Instance;
            var completions = autoCompleteService.GetCompletionItems(
                textDocument, 
                scriptFile,
                connInfo);

            Assert.True(completions.Length > 0);
        }

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

        #endregion
    }
}
