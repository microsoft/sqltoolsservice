//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Parser;
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
        /// The batches underneath this query
        /// </summary>
        private IEnumerable<Batch> Batches { get; set; }

        /// <summary>
        /// The summaries of the batches underneath this query
        /// </summary>
        public BatchSummary[] BatchSummaries
        {
            get { return Batches.Select((batch, index) => new BatchSummary
            {
                Id = index,
                HasError = batch.HasError,
                Messages = batch.ResultMessages.ToArray(),
                ResultSetSummaries = batch.ResultSummaries
            }).ToArray(); }
        }

        /// <summary>
        /// Cancellation token source, used for cancelling async db actions
        /// </summary>
        private readonly CancellationTokenSource cancellationSource;

        /// <summary>
        /// The connection info associated with the file editor owner URI, used to create a new
        /// connection upon execution of the query
        /// </summary>
        public ConnectionInfo EditorConnection { get; set; }

        /// <summary>
        /// Whether or not the query has completed executed, regardless of success or failure
        /// </summary>
        public bool HasExecuted
        {
            get { return Batches.All(b => b.HasExecuted); }
        }

        /// <summary>
        /// The text of the query to execute
        /// </summary>
        public string QueryText { get; set; }

        #endregion

        /// <summary>
        /// Constructor for a query
        /// </summary>
        /// <param name="queryText">The text of the query to execute</param>
        /// <param name="connection">The information of the connection to use to execute the query</param>
        public Query(string queryText, ConnectionInfo connection)
        {
            // Sanity check for input
            if (String.IsNullOrEmpty(queryText))
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
            cancellationSource = new CancellationTokenSource();

            // Process the query into batches
            ParseResult parseResult = Parser.Parse(queryText);
            Batches = parseResult.Script.Batches.Select(b => new Batch(b.Sql));
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

            // Open up a connection for querying the database
            string connectionString = ConnectionService.BuildConnectionString(EditorConnection.ConnectionDetails);
            using (DbConnection conn = EditorConnection.Factory.CreateSqlConnection(connectionString))
            {
                // We need these to execute synchronously, otherwise the user will be very unhappy
                foreach (Batch b in Batches)
                {
                    await b.Execute(conn, cancellationSource.Token);
                }
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

        /// <summary>
        /// Cancels the query by issuing the cancellation token
        /// </summary>
        public void Cancel()
        {
            // Make sure that the query hasn't completed execution
            if (HasExecuted)
            {
                throw new InvalidOperationException("The query has already completed, it cannot be cancelled.");
            }

            // Issue the cancellation token for the query
            cancellationSource.Cancel();
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
