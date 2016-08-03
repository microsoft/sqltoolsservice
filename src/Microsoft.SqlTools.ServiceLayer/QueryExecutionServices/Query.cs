using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecutionServices.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecutionServices
{
    public class Query //: IDisposable
    {
        public string QueryText { get; set; }

        public DbConnection SqlConnection { get; set; }

        private readonly CancellationTokenSource cancellationSource;

        public List<ResultSet> ResultSets { get; set; }

        public Query(string queryText, DbConnection connection)
        {
            QueryText = queryText;
            SqlConnection = connection;
            ResultSets = new List<ResultSet>();
            cancellationSource = new CancellationTokenSource();
        }

        public async Task Execute()
        {
            // Open the connection, if it's not already open
            if ((SqlConnection.State & ConnectionState.Open) == 0)
            {
                await SqlConnection.OpenAsync(cancellationSource.Token);
            }

            // Create a command that we'll use for executing the query
            using (DbCommand command = SqlConnection.CreateCommand())
            {
                command.CommandText = QueryText;
                command.CommandType = CommandType.Text;

                // Execute the command to get back a reader
                using (DbDataReader reader = await command.ExecuteReaderAsync(cancellationSource.Token))
                {
                    do
                    {
                        // Create a new result set that we'll use to store all the data
                        ResultSet resultSet = new ResultSet();
                        if (reader.CanGetColumnSchema())
                        {
                            resultSet.Columns = reader.GetColumnSchema().ToArray();
                        }

                        // Read until we hit the end of the result set
                        while (await reader.ReadAsync(cancellationSource.Token))
                        {
                            resultSet.AddRow(reader);
                        }

                        // Add the result set to the results of the query
                        ResultSets.Add(resultSet);
                    } while (await reader.NextResultAsync(cancellationSource.Token));
                }
            }
        }
    }
}
