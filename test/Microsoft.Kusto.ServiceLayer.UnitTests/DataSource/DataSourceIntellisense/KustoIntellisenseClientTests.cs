using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.DataSource.DataSourceIntellisense
{
    public class KustoIntellisenseClientTests
    {
        [Test]
        public void Constructor_Sets_SchemaState_For_Functions_And_Tables()
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
            kustoClientMock.Setup(x => x.ExecuteQueryAsync<ShowDatabaseSchemaResult>(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
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
            kustoClientMock.Setup(x => x.ExecuteQueryAsync<ShowFunctionsResult>(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
                .Returns(Task.FromResult(functionSchemaResults));

            var client = new KustoIntellisenseClient(kustoClientMock.Object);

            var schemaState = client.SchemaState;
            Assert.AreEqual("cluster(https://fake.url.com)", schemaState.Cluster.Display);
            Assert.AreEqual("https://fake.url.com", schemaState.Cluster.Name);
            
            Assert.AreEqual("database(FakeDatabaseName)", schemaState.Database.Display);
            Assert.AreEqual("FakeDatabaseName", schemaState.Database.Name);
            
            Assert.AreEqual(1, schemaState.Database.Functions.Count);
            var functionSymbol = schemaState.Database.Functions[0];
            Assert.AreEqual("FakeFunctionName(a, b)", functionSymbol.Display);
            Assert.AreEqual("FakeFunctionName", functionSymbol.Name);
        }
        
        [Test]
        public void UpdateDatabase_Updates_SchemaState()
        {
            var kustoClientMock = new Mock<IKustoClient>();
            kustoClientMock.Setup(x => x.ClusterName).Returns("https://fake.url.com");
            kustoClientMock.Setup(x => x.DatabaseName).Returns("FakeDatabaseName");

            var newDatabaseSchema = new ShowDatabaseSchemaResult
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

            var newDatabaseSchemaResults = new List<ShowDatabaseSchemaResult> {newDatabaseSchema} as IEnumerable<ShowDatabaseSchemaResult>;
            kustoClientMock.Setup(x => x.ExecuteQueryAsync<ShowDatabaseSchemaResult>(It.IsAny<string>(), It.IsAny<CancellationToken>(), newDatabaseSchema.DatabaseName))
                .Returns(Task.FromResult(newDatabaseSchemaResults));

            var client = new KustoIntellisenseClient(kustoClientMock.Object);
            client.UpdateDatabase("NewDatabaseName");

            var schemaState = client.SchemaState;
            Assert.AreEqual("cluster(https://fake.url.com)", schemaState.Cluster.Display);
            Assert.AreEqual("https://fake.url.com", schemaState.Cluster.Name);
            
            Assert.AreEqual("database(NewDatabaseName)", schemaState.Database.Display);
            Assert.AreEqual("NewDatabaseName", schemaState.Database.Name);
        }
    }
}