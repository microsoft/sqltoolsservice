using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.Execution
{
    public class ResultSetTests
    {
        [Fact]
        public async Task ResultCreation()
        {
            // If:
            // ... I create a new result set with a valid db data reader
            DbDataReader mockReader = GetReader(Common.CreateTestConnectionInfo(null, false), string.Empty);
            ResultSet resultSet = new ResultSet(mockReader, Common.GetFileStreamFactory(), null);

            // Then:
            // ... There should not be any data read yet
            Assert.Empty(resultSet.Columns);
            Assert.False(resultSet.IsComplete);
            Assert.Equal(0, resultSet.RowCount);

            // ... Attempting to get a subset should fail terribly
            await Assert.ThrowsAsync<InvalidOperationException>(() => resultSet.GetSubset(0, 1));
        }


        private static DbDataReader GetReader(ConnectionInfo info, string query)
        {

            var connection = info.Factory.CreateSqlConnection(ConnectionService.BuildConnectionString(info.ConnectionDetails));
            var command = connection.CreateCommand();
            command.CommandText = query;
            return command.ExecuteReader();

        }
    }
}
