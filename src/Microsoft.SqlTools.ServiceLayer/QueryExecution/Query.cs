// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Internal representation of an active query
    /// </summary>
    public class Query : IDisposable
    {
        /// <summary>
        /// "Error" code produced by SQL Server when the database context (name) for a connection changes.
        /// </summary>
        private const int DatabaseContextChangeErrorNumber = 5701;

        #region Member Variables

        /// <summary>
        /// Cancellation token source, used for cancelling async db actions
        /// </summary>
        private readonly CancellationTokenSource cancellationSource;

        /// <summary>
        /// For IDisposable implementation, whether or not this object has been disposed
        /// </summary>
        private bool disposed;

        /// <summary>
        /// The connection info associated with the file editor owner URI, used to create a new
        /// connection upon execution of the query
        /// </summary>
        private readonly ConnectionInfo editorConnection;

        /// <summary>
        /// Whether or not the execute method has been called for this query
        /// </summary>
        private bool hasExecuteBeenCalled;

        /// <summary>
        /// The factory to use for outputting the results of this query
        /// </summary>
        private readonly IFileStreamFactory outputFileFactory;

        #endregion

        /// <summary>
        /// Constructor for a query
        /// </summary>
        /// <param name="queryText">The text of the query to execute</param>
        /// <param name="connection">The information of the connection to use to execute the query</param>
        /// <param name="settings">Settings for how to execute the query, from the user</param>
        /// <param name="outputFactory">Factory for creating output files</param>
        public Query(string queryText, ConnectionInfo connection, QueryExecutionSettings settings, IFileStreamFactory outputFactory)
        {
            // Sanity check for input
            Validate.IsNotNullOrEmptyString(nameof(queryText), queryText);
            Validate.IsNotNull(nameof(connection), connection);
            Validate.IsNotNull(nameof(settings), settings);
            Validate.IsNotNull(nameof(outputFactory), outputFactory);

            // Initialize the internal state
            QueryText = queryText;
            editorConnection = connection;
            cancellationSource = new CancellationTokenSource();
            outputFileFactory = outputFactory;

            // Process the query into batches
            ParseResult parseResult = Parser.Parse(queryText, new ParseOptions
            {
                BatchSeparator = settings.BatchSeparator
            });
            // NOTE: We only want to process batches that have statements (ie, ignore comments and empty lines)
            Batches = parseResult.Script.Batches.Where(b => b.Statements.Count > 0)
                .Select(b => new Batch(b.Sql, 
                                       b.StartLocation.LineNumber - 1, 
                                       b.StartLocation.ColumnNumber - 1, 
                                       b.EndLocation.LineNumber - 1, 
                                       b.EndLocation.ColumnNumber - 1, 
                                       outputFileFactory)).ToArray();
        }

        #region Properties

        /// <summary>
        /// The batches underneath this query
        /// </summary>
        internal Batch[] Batches { get; set; }

        /// <summary>
        /// The summaries of the batches underneath this query
        /// </summary>
        public BatchSummary[] BatchSummaries
        {
            get
            {
                if (!HasExecuted)
                {
                    throw new InvalidOperationException("Query has not been executed.");
                }

                return Batches.Select((batch, index) => new BatchSummary
                {
                    Id = index,
                    HasError = batch.HasError,
                    Messages = batch.ResultMessages.ToArray(),
                    ResultSetSummaries = batch.ResultSummaries,
                    Selection = batch.Selection
                }).ToArray();
            }
        }

        /// <summary>
        /// Whether or not the query has completed executed, regardless of success or failure
        /// </summary>
        /// <remarks>
        /// Don't touch the setter unless you're doing unit tests!
        /// </remarks>
        public bool HasExecuted
        {
            get { return Batches.Length == 0 ? hasExecuteBeenCalled : Batches.All(b => b.HasExecuted); }
            internal set
            {
                hasExecuteBeenCalled = value;
                foreach (var batch in Batches)
                {
                    batch.HasExecuted = value;
                }
            }
        }

        /// <summary>
        /// The text of the query to execute
        /// </summary>
        public string QueryText { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Cancels the query by issuing the cancellation token
        /// </summary>
        public void Cancel()
        {
            // Make sure that the query hasn't completed execution
            if (HasExecuted)
            {
                throw new InvalidOperationException(SR.QueryServiceCancelAlreadyCompleted);
            }

            // Issue the cancellation token for the query
            cancellationSource.Cancel();
        }

        /// <summary>
        /// Executes this query asynchronously and collects all result sets
        /// </summary>
        public async Task Execute()
        {
            // Mark that we've internally executed
            hasExecuteBeenCalled = true;

            // Don't actually execute if there aren't any batches to execute
            if (Batches.Length == 0)
            {
                return;
            }

            // Open up a connection for querying the database
            string connectionString = ConnectionService.BuildConnectionString(editorConnection.ConnectionDetails);
            // TODO: Don't create a new connection every time, see TFS #834978
            using (DbConnection conn = editorConnection.Factory.CreateSqlConnection(connectionString))
            {
                await conn.OpenAsync();

                ReliableSqlConnection sqlConn = conn as ReliableSqlConnection;
                if (sqlConn != null)
                {
                    // Subscribe to database informational messages
                    sqlConn.GetUnderlyingConnection().InfoMessage += OnInfoMessage;
                }

                try
                {
                    // We need these to execute synchronously, otherwise the user will be very unhappy
                    foreach (Batch b in Batches)
                    {
                        await b.Execute(conn, cancellationSource.Token);
                    }
                }
                finally
                {
                    if (sqlConn != null)
                    {
                        // Subscribe to database informational messages
                        sqlConn.GetUnderlyingConnection().InfoMessage -= OnInfoMessage;
                    }
                }

                // TODO: Close connection after eliminating using statement for above TODO
            }
        }

        /// <summary>
        /// Handler for database messages during query execution
        /// </summary>
        private void OnInfoMessage(object sender, SqlInfoMessageEventArgs args)
        {
            SqlConnection conn = sender as SqlConnection;
            if (conn == null)
            {
                throw new InvalidOperationException(SR.QueryServiceMessageSenderNotSql);
            }

            foreach(SqlError error in args.Errors) 
            {
                // Did the database context change (error code 5701)?
                if (error.Number == DatabaseContextChangeErrorNumber)
                {
                    ConnectionService.Instance.ChangeConnectionDatabaseContext(editorConnection.OwnerUri, conn.Database);
                }
            }
        }

        /// <summary>
        /// Retrieves a subset of the result sets
        /// </summary>
        /// <param name="batchIndex">The index for selecting the batch item</param>
        /// <param name="resultSetIndex">The index for selecting the result set</param>
        /// <param name="startRow">The starting row of the results</param>
        /// <param name="rowCount">How many rows to retrieve</param>
        /// <returns>A subset of results</returns>
        public Task<ResultSetSubset> GetSubset(int batchIndex, int resultSetIndex, int startRow, int rowCount)
        {
            // Sanity check that the results are available
            if (!HasExecuted)
            {
                throw new InvalidOperationException(SR.QueryServiceSubsetNotCompleted);
            }

            // Sanity check to make sure that the batch is within bounds
            if (batchIndex < 0 || batchIndex >= Batches.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(batchIndex), SR.QueryServiceSubsetBatchOutOfRange);
            }

            return Batches[batchIndex].GetSubset(resultSetIndex, startRow, rowCount);
        }

        #endregion

        #region IDisposable Implementation

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
                foreach (Batch b in Batches)
                {
                    b.Dispose();
                }
            }

            disposed = true;
        }

        #endregion
    }
}
