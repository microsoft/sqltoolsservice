//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Internal representation of an active query
    /// </summary>
    public class Query : IDisposable
    {
        #region Properties

        /// <summary>
        /// Cancellation token source, used for cancelling async db actions
        /// </summary>
        private readonly CancellationTokenSource cancellationSource;

        /// <summary>
        /// The connection info associated with the file editor owner URI, used to create a new
        /// connection upon execution of the query
        /// </summary>
        public ConnectionInfo EditorConnection { get; set; }

        public bool HasExecuted { get; set; }

        /// <summary>
        /// The text of the query to execute
        /// </summary>
        public string QueryText { get; set; }

        /// <summary>
        /// The result sets of the query execution
        /// </summary>
        public List<ResultSet> ResultSets { get; set; }

        /// <summary>
        /// Property for generating a set result set summaries from the result sets
        /// </summary>
        public ResultSetSummary[] ResultSummary
        {
            get
            {
                return ResultSets.Select((set, index) => new ResultSetSummary
                {
                    ColumnInfo = set.Columns,
                    Id = index,
                    RowCount = set.Rows.Count
                }).ToArray();
            }
        }

        #endregion

        /// <summary>
        /// Constructor for a query
        /// </summary>
        /// <param name="queryText">The text of the query to execute</param>
        /// <param name="connection">The information of the connection to use to execute the query</param>
        public Query(string queryText, ConnectionInfo connection)
        {
            // Sanity check for input
            if (String.IsNullOrWhiteSpace(queryText))
            {
                throw new ArgumentNullException(nameof(queryText), "Query text cannot be null");
            }
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection), "Connection cannot be null");
            }

            // Initialize the internal state
            QueryText = queryText;
            EditorConnection = connection;
            HasExecuted = false;
            ResultSets = new List<ResultSet>();
            cancellationSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Executes this query asynchronously and collects all result sets
        /// </summary>
        public async Task Execute()
        {
            // Sanity check to make sure we haven't already run this query
            if (HasExecuted)
            {
                throw new InvalidOperationException("Query has already executed.");
            }

            DbConnection conn = null;

            // Create a connection from the connection details
            try
            {
                string connectionString = ConnectionService.BuildConnectionString(EditorConnection.ConnectionDetails);
                using (conn = EditorConnection.Factory.CreateSqlConnection(connectionString))
                {
                    await conn.OpenAsync(cancellationSource.Token);

                    // Create a command that we'll use for executing the query
                    using (DbCommand command = conn.CreateCommand())
                    {
                        command.CommandText = QueryText;
                        command.CommandType = CommandType.Text;

                        // Execute the command to get back a reader
                        using (DbDataReader reader = await command.ExecuteReaderAsync(cancellationSource.Token))
                        {
                            do
                            {
                                // TODO: This doesn't properly handle scenarios where the query is SELECT but does not have rows
                                if (!reader.HasRows)
                                {
                                    continue;
                                }

                                // Read until we hit the end of the result set
                                ResultSet resultSet = new ResultSet();
                                while (await reader.ReadAsync(cancellationSource.Token))
                                {
                                    resultSet.AddRow(reader);
                                }

                                // Read off the column schema information
                                if (reader.CanGetColumnSchema())
                                {
                                    resultSet.Columns = reader.GetColumnSchema().ToArray();
                                }

                                // Add the result set to the results of the query
                                ResultSets.Add(resultSet);
                            } while (await reader.NextResultAsync(cancellationSource.Token));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Dispose of the connection
                conn?.Dispose();
                throw;
            }
            finally
            {
                // Mark that we have executed
                HasExecuted = true;
            }
        }

        /// <summary>
        /// Retrieves a subset of the result sets
        /// </summary>
        /// <param name="resultSetIndex">The index for selecting the result set</param>
        /// <param name="startRow">The starting row of the results</param>
        /// <param name="rowCount">How many rows to retrieve</param>
        /// <returns>A subset of results</returns>
        public ResultSetSubset GetSubset(int resultSetIndex, int startRow, int rowCount)
        {
            // Sanity check that the results are available
            if (!HasExecuted)
            {
                throw new InvalidOperationException("The query has not completed, yet.");
            }

            // Sanity check to make sure we have valid numbers
            if (resultSetIndex < 0 || resultSetIndex >= ResultSets.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(resultSetIndex), "Result set index cannot be less than 0" +
                                                                             "or greater than the number of result sets");
            }
            ResultSet targetResultSet = ResultSets[resultSetIndex];
            if (startRow < 0 || startRow >= targetResultSet.Rows.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(startRow), "Start row cannot be less than 0 " +
                                                                        "or greater than the number of rows in the resultset");
            }
            if (rowCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowCount), "Row count must be a positive integer");
            }

            // Retrieve the subset of the results as per the request
            object[][] rows = targetResultSet.Rows.Skip(startRow).Take(rowCount).ToArray();
            return new ResultSetSubset
            {
                Rows = rows,
                RowCount = rows.Length
            };
        }

        #region IDisposable Implementation

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                cancellationSource.Dispose();
            }

            disposed = true;
        }

        ~Query()
        {
            Dispose(false);
        }

        #endregion
    }
}
