using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.Test.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class ExecuteTests
    {
        [Fact]
        public void QueryCreationTest()
        {
            // If I create a new query...
            Query query = new Query("NO OP", CreateTestConnectionInfo());

            // Then: 
            // ... It should not have executed
            Assert.False(query.HasExecuted, "The query should not have executed.");

            // ... The results should be empty
            Assert.Empty(query.ResultSets);
            Assert.Empty(query.ResultSummary);
        }

        private static ConnectionInfo CreateTestConnectionInfo()
        {
            // Create connection info
            ConnectionDetails connDetails = new ConnectionDetails
            {
                UserName = "sa",
                Password = "Yukon900",
                DatabaseName = "AdventureWorks2016CTP3_2",
                ServerName = "sqltools11"
            };

#if !USE_LIVE_CONNECTION
            // Use the mock db connection factory
            ISqlConnectionFactory factory = new TestSqlConnectionFactory();
#else
            // Use a real db connection factory
            ISqlConnectionFactory factory = new SqlConnectionFactory();
#endif

            return new ConnectionInfo(factory, "test://test", connDetails);
        }
    }
}
