using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Completion;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.DataSource.DataSourceIntellisense
{
    public class KustoIntellisenseClientTests
    {
        private Mock<IKustoClient> GetMockKustoClient()
        {
            var kustoClientMock = new Mock<IKustoClient>();
            kustoClientMock.Setup(x => x.ClusterName).Returns("https://fake.url.com");
            kustoClientMock.Setup(x => x.DatabaseName).Returns("FakeDatabaseName");

            var databaseSchema = new ShowDatabaseSchemaResult
            {
                DatabaseName = "FakeDatabaseName",
                TableName = "FakeTableName",
                ColumnName = "FakeColumnName",
                ColumnType = "bool",
                IsDefaultTable = false,
                IsDefaultColumn = false,
                PrettyName = "Fake Table Name",
                Version = "",
                Folder = "FakeTableFolder",
                DocName = ""
            };

            var databaseSchemaResults = new List<ShowDatabaseSchemaResult> {databaseSchema} as IEnumerable<ShowDatabaseSchemaResult>;
            kustoClientMock.Setup(x => x.ExecuteQueryAsync<ShowDatabaseSchemaResult>(It.IsAny<string>(), It.IsAny<CancellationToken>(), "FakeDatabaseName"))
                .Returns(Task.FromResult(databaseSchemaResults));

            var functionSchema = new ShowFunctionsResult
            {
                Name = "FakeFunctionName",
                Parameters = "a:real, b:real",
                Body = "a+b",
                Folder = "FakeFunctionFolder",
                DocString = ""
            };

            var functionSchemaResults = new List<ShowFunctionsResult> {functionSchema} as IEnumerable<ShowFunctionsResult>;
            kustoClientMock.Setup(x => x.ExecuteQueryAsync<ShowFunctionsResult>(It.IsAny<string>(), It.IsAny<CancellationToken>(), "FakeDatabaseName"))
                .Returns(Task.FromResult(functionSchemaResults));

            return kustoClientMock;
        }
        
        [Test]
        public void GetSemanticMarkers_Returns_Error_For_InvalidText()
        {
            var kustoClientMock = GetMockKustoClient();
            
            var client = new KustoIntellisenseClient(kustoClientMock.Object);
            var semanticMarkers = client.GetSemanticMarkers(new ScriptParseInfo(), new ScriptFile("", "", ""), "InvalidText");
            
            var semanticMarker = semanticMarkers.Single();
            Assert.AreEqual(ScriptFileMarkerLevel.Error, semanticMarker.Level);
            Assert.AreEqual("The name 'InvalidText' does not refer to any known column, table, variable or function.", semanticMarker.Message);
            Assert.IsNotNull(semanticMarker.ScriptRegion);
        }

        [Test]
        public void GetSemanticMarkers_Returns_Zero_SemanticMarkers_For_ValidQueryText()
        {
            var kustoClientMock = GetMockKustoClient();
            
            var client = new KustoIntellisenseClient(kustoClientMock.Object);
            var queryText = @".show commands";
            var semanticMarkers = client.GetSemanticMarkers(new ScriptParseInfo(), new ScriptFile("", "", ""), queryText);
            
            Assert.AreEqual(0, semanticMarkers.Length);
        }

        [Test]
        public void GetDefinition_Returns_Null()
        {
            var kustoClientMock = GetMockKustoClient();
            var client = new KustoIntellisenseClient(kustoClientMock.Object);
            var definition = client.GetDefinition("queryText", 0, 1, 1);
            
            // finish these assertions once the function is implemented
            Assert.IsNull(definition);
        }

        [Test]
        public void GetHoverHelp_Returns_Hover()
        {
            var kustoClientMock = GetMockKustoClient();
            var client = new KustoIntellisenseClient(kustoClientMock.Object);
            var textDocumentPosition = new TextDocumentPosition
            {
                Position = new Position()
            };
            var scriptFile = new ScriptFile("", "", "");
            var scriptParseInfo = new ScriptParseInfo();
            var documentInfo = new ScriptDocumentInfo(textDocumentPosition, scriptFile, scriptParseInfo);
            
            var hover = client.GetHoverHelp(documentInfo, new Position());

            Assert.IsNotNull(hover);
        }

        [Test]
        public void GetAutoCompleteSuggestions_Returns_CompletionItems()
        {
            var kustoClientMock = GetMockKustoClient();
            var client = new KustoIntellisenseClient(kustoClientMock.Object);

            var position = new Position();
            var textDocumentPosition = new TextDocumentPosition
            {
                Position = position
            };
            var scriptFile = new ScriptFile("", "", "");
            var scriptParseInfo = new ScriptParseInfo();
            var documentInfo = new ScriptDocumentInfo(textDocumentPosition, scriptFile, scriptParseInfo);
            var items = client.GetAutoCompleteSuggestions(documentInfo, position);
            
            Assert.AreEqual(20, items.Length);
        }
        [Test]
        public void UpdateDatabase_Updates_SchemaState()
        {
            var kustoClientMock = GetMockKustoClient();
            
            var databaseSchema = new ShowDatabaseSchemaResult
            {
                DatabaseName = "NewDatabaseName",
                TableName = "NewTableName",
                ColumnName = "NewColumnName",
                ColumnType = "bool",
                IsDefaultTable = false,
                IsDefaultColumn = false,
                PrettyName = "New Table Name",
                Version = "",
                Folder = "NewTableFolder",
                DocName = ""
            };

            var databaseSchemaResults = new List<ShowDatabaseSchemaResult> {databaseSchema} as IEnumerable<ShowDatabaseSchemaResult>;
            kustoClientMock.Setup(x => x.ExecuteQueryAsync<ShowDatabaseSchemaResult>(It.IsAny<string>(), It.IsAny<CancellationToken>(), "NewDatabaseName"))
                .Returns(Task.FromResult(databaseSchemaResults));
            
            var client = new KustoIntellisenseClient(kustoClientMock.Object);
            client.UpdateDatabase("NewDatabaseName");

            var position = new Position();
            var textDocumentPosition = new TextDocumentPosition
            {
                Position = position
            };
            var scriptFile = new ScriptFile("", "", "");
            var scriptParseInfo = new ScriptParseInfo();
            var documentInfo = new ScriptDocumentInfo(textDocumentPosition, scriptFile, scriptParseInfo);
            var items = client.GetAutoCompleteSuggestions(documentInfo, position);
            
            Assert.AreEqual(19, items.Length);
            var tableItem = items.FirstOrDefault(x => x.Detail == "Table");

            // assert new table is being returned to show database has changed
            Assert.IsNotNull(tableItem);
            Assert.AreEqual("NewTableName", tableItem.InsertText);
            Assert.AreEqual("NewTableName", tableItem.Label);
        }
    }
}