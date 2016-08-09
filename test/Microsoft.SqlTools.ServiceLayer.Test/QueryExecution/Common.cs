using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Moq;
using Moq.Protected;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class Common
    {
        public static readonly Dictionary<string, string>[] StandardTestData =
        {
            new Dictionary<string, string> { {"col1", "val11"}, { "col2", "val12"}, { "col3", "val13"}, { "col4", "col14"} },
            new Dictionary<string, string> { {"col1", "val21"}, { "col2", "val22"}, { "col3", "val23"}, { "col4", "col24"} },
            new Dictionary<string, string> { {"col1", "val31"}, { "col2", "val32"}, { "col3", "val33"}, { "col4", "col34"} },
            new Dictionary<string, string> { {"col1", "val41"}, { "col2", "val42"}, { "col3", "val43"}, { "col4", "col44"} },
            new Dictionary<string, string> { {"col1", "val51"}, { "col2", "val52"}, { "col3", "val53"}, { "col4", "col54"} },
        };

        public static Dictionary<string, string>[] GetTestData(int columns, int rows)
        {
            Dictionary<string, string>[] output = new Dictionary<string, string>[rows];
            for (int row = 0; row < rows; row++)
            {
                Dictionary<string, string> rowDictionary = new Dictionary<string, string>();
                for (int column = 0; column < columns; column++)
                {
                    rowDictionary.Add(String.Format("column{0}", column), String.Format("val{0}{1}", column, row));
                }
                output[row] = rowDictionary;
            }

            return output;
        }

        public static Query GetBasicExecutedQuery()
        {
            Query query = new Query("SIMPLE QUERY", CreateTestConnectionInfo(new[] { StandardTestData }, false));
            query.Execute().Wait();
            return query;
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

        public static DbCommand CreateTestCommand(Dictionary<string, string>[][] data, bool throwOnRead)
        {
            var commandMock = new Mock<DbCommand> { CallBase = true };
            var commandMockSetup = commandMock.Protected()
                .Setup<DbDataReader>("ExecuteDbDataReader", It.IsAny<CommandBehavior>());

            // Setup the expected behavior
            if (throwOnRead)
            {
                commandMockSetup.Throws(new Mock<DbException>().Object);
            }
            else
            {
                commandMockSetup.Returns(new TestDbDataReader(data));
            }
                

            return commandMock.Object;
        }

        public static DbConnection CreateTestConnection(Dictionary<string, string>[][] data, bool throwOnRead)
        {
            var connectionMock = new Mock<DbConnection> { CallBase = true };
            connectionMock.Protected()
                .Setup<DbCommand>("CreateDbCommand")
                .Returns(CreateTestCommand(data, throwOnRead));

            return connectionMock.Object;
        }

        public static ISqlConnectionFactory CreateMockFactory(Dictionary<string, string>[][] data, bool throwOnRead)
        {
            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>()))
                .Returns(CreateTestConnection(data, throwOnRead));

            return mockFactory.Object;
        }

        public static ConnectionInfo CreateTestConnectionInfo(Dictionary<string, string>[][] data, bool throwOnRead)
        {
            // Create connection info
            ConnectionDetails connDetails = new ConnectionDetails
            {
                UserName = "sa",
                Password = "Yukon900",
                DatabaseName = "AdventureWorks2016CTP3_2",
                ServerName = "sqltools11"
            };

            return new ConnectionInfo(CreateMockFactory(data, throwOnRead), "test://test", connDetails);
        }

        #endregion

    }
}
