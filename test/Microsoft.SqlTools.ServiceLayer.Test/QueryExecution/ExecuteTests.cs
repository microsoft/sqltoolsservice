using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class ExecuteTests
    {
        private static Dictionary<string, string>[] testData = 
        {
            new Dictionary<string, string> { {"col1", "val11"}, { "col2", "val12"}, { "col3", "val13"}, { "col4", "col14"} },
            new Dictionary<string, string> { {"col1", "val21"}, { "col2", "val22"}, { "col3", "val23"}, { "col4", "col24"} },
            new Dictionary<string, string> { {"col1", "val31"}, { "col2", "val32"}, { "col3", "val33"}, { "col4", "col34"} },
            new Dictionary<string, string> { {"col1", "val41"}, { "col2", "val42"}, { "col3", "val43"}, { "col4", "col44"} },
            new Dictionary<string, string> { {"col1", "val51"}, { "col2", "val52"}, { "col3", "val53"}, { "col4", "col54"} },
        };

        [Fact]
        public void QueryCreationTest()
        {
            // If I create a new query...
            Query query = new Query("NO OP", CreateTestConnectionInfo(null));

            // Then: 
            // ... It should not have executed
            Assert.False(query.HasExecuted, "The query should not have executed.");

            // ... The results should be empty
            Assert.Empty(query.ResultSets);
            Assert.Empty(query.ResultSummary);
        }

        [Fact]
        public void QueryExecuteNoResultSets()
        {
            // If I execute a query that should get no result sets
            Query query = new Query("Query with no result sets", CreateTestConnectionInfo(null));
            query.Execute().Wait();

            // Then:
            // ... It should have executed
            Assert.True(query.HasExecuted, "The query should have been marked executed.");
            
            // ... The results should be empty
            Assert.Empty(query.ResultSets);
            Assert.Empty(query.ResultSummary);

            // ... The results should not be null
            Assert.NotNull(query.ResultSets);
            Assert.NotNull(query.ResultSummary);
        }

        [Fact]
        public void QueryExecuteQueryOneResultSet()
        {
            ConnectionInfo ci = CreateTestConnectionInfo(new[] {testData});

            // If I execute a query that should get one result set
            int resultSets = 1;
            int rows = 5;
            int columns = 4;
            Query query = new Query("Query with one result sets", ci);
            query.Execute().Wait();

            // Then:
            // ... It should have executed
            Assert.True(query.HasExecuted, "The query should have been marked executed.");

            // ... There should be exactly one result set
            Assert.Equal(resultSets, query.ResultSets.Count);

            // ... Inside the result set should be with 5 rows
            Assert.Equal(rows, query.ResultSets[0].Rows.Count);

            // ... Inside the result set should have 5 columns and 5 column definitions
            Assert.Equal(columns, query.ResultSets[0].Rows[0].Length);
            Assert.Equal(columns, query.ResultSets[0].Columns.Length);

            // ... There should be exactly one result set summary
            Assert.Equal(resultSets, query.ResultSummary.Length);

            // ... Inside the result summary, there should be 5 column definitions
            Assert.Equal(columns, query.ResultSummary[0].ColumnInfo.Length);

            // ... Inside the result summary, there should be 5 rows
            Assert.Equal(rows, query.ResultSummary[0].RowCount);
        }

        [Fact]
        public void QueryExecuteQueryTwoResultSets()
        {
            var dataset = new[] {testData, testData};
            int resultSets = dataset.Length;
            int rows = testData.Length;
            int columns = testData[0].Count;
            ConnectionInfo ci = CreateTestConnectionInfo(dataset);

            // If I execute a query that should get two result sets
            Query query = new Query("Query with two result sets", ci);
            query.Execute().Wait();

            // Then:
            // ... It should have executed
            Assert.True(query.HasExecuted, "The query should have been marked executed.");

            // ... There should be exactly two result sets
            Assert.Equal(resultSets, query.ResultSets.Count);

            foreach (ResultSet rs in query.ResultSets)
            {
                // ... Each result set should have 5 rows
                Assert.Equal(rows, rs.Rows.Count);

                // ... Inside each result set should be 5 columns and 5 column definitions
                Assert.Equal(columns, rs.Rows[0].Length);
                Assert.Equal(columns, rs.Columns.Length);
            }

            // ... There should be exactly two result set summaries
            Assert.Equal(resultSets, query.ResultSummary.Length);

            foreach (ResultSetSummary rs in query.ResultSummary)
            {
                // ... Inside each result summary, there should be 5 column definitions
                Assert.Equal(columns, rs.ColumnInfo.Length);

                // ... Inside each result summary, there should be 5 rows
                Assert.Equal(rows, rs.RowCount);
            }
        }

        #region Mocking

        //private static DbDataReader CreateTestReader(int columnCount, int rowCount)
        //{
        //    var readerMock = new Mock<DbDataReader> { CallBase = true };

        //    // Setup for column reads
        //    // TODO: We can't test columns because of oddities with how datatable/GetColumn

        //    // Setup for row reads
        //    var readSequence = readerMock.SetupSequence(dbReader => dbReader.Read());
        //    for (int i = 0; i < rowCount; i++)
        //    {
        //        readSequence.Returns(true);
        //    }
        //    readSequence.Returns(false);

        //    // Make sure that if we call for data from the reader it works
        //    readerMock.Setup(dbReader => dbReader[InColumnRange(columnCount)])
        //        .Returns<object>(i => i.ToString());
        //    readerMock.Setup(dbReader => dbReader[NotInColumnRange(columnCount)])
        //        .Throws(new ArgumentOutOfRangeException());
        //    readerMock.Setup(dbReader => dbReader.HasRows)
        //        .Returns(rowCount > 0);

        //    return readerMock.Object;
        //}

        //private static int InColumnRange(int columnCount)
        //{
        //    return Match.Create<int>(i => i < columnCount && i > 0);
        //}

        //private static int NotInColumnRange(int columnCount)
        //{
        //    return Match.Create<int>(i => i >= columnCount || i < 0);
        //}

        private static DbCommand CreateTestCommand(Dictionary<string, string>[][] data)
        {
            var commandMock = new Mock<DbCommand> {CallBase = true};
            commandMock.Protected()
                .Setup<DbDataReader>("ExecuteDbDataReader", It.IsAny<CommandBehavior>())
                .Returns(new TestDbDataReader(data));

            return commandMock.Object;
        }

        private static DbConnection CreateTestConnection(Dictionary<string, string>[][] data)
        {
            var connectionMock = new Mock<DbConnection> {CallBase = true};
            connectionMock.Protected()
                .Setup<DbCommand>("CreateDbCommand")
                .Returns(CreateTestCommand(data));

            return connectionMock.Object;
        }

        private static ISqlConnectionFactory CreateMockFactory(Dictionary<string, string>[][] data)
        {
            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>()))
                .Returns(CreateTestConnection(data));

            return mockFactory.Object;
        }

        private static ConnectionInfo CreateTestConnectionInfo(Dictionary<string, string>[][] data)
        {
            // Create connection info
            ConnectionDetails connDetails = new ConnectionDetails
            {
                UserName = "sa",
                Password = "Yukon900",
                DatabaseName = "AdventureWorks2016CTP3_2",
                ServerName = "sqltools11"
            };

            return new ConnectionInfo(CreateMockFactory(data), "test://test", connDetails);
        }

        #endregion
    }
}
