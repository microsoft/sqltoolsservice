//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// This class represents a batch within a query
    /// </summary>
    public class Batch : IDisposable
    {
        private const string RowsAffectedFormat = "({0} row(s) affected)";

        #region Properties
        /// <summary>
        /// The text of batch that will be executed
        /// </summary>
        public string BatchText { get; set; }

        /// <summary>
        /// Whether or not this batch has an error
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Whether or not this batch has been executed, regardless of success or failure 
        /// </summary>
        public bool HasExecuted { get; set; }

        /// <summary>
        /// Internal representation of the messages so we can modify internally
        /// </summary>
        private readonly List<string> resultMessages;

        /// <summary>
        /// Messages that have come back from the server
        /// </summary>
        public IEnumerable<string> ResultMessages
        {
            get { return resultMessages; }
        }

        /// <summary>
        /// Internal representation of the result sets so we can modify internally
        /// </summary>
        private readonly List<ResultSet> resultSets;

        /// <summary>
        /// The result sets of the batch execution
        /// </summary>
        public IEnumerable<ResultSet> ResultSets
        {
            get { return resultSets; }
        }

        /// <summary>
        /// Property for generating a set result set summaries from the result sets
        /// </summary>
        public ResultSetSummary[] ResultSummaries
        {
            get
            {
                return ResultSets.Select((set, index) => new ResultSetSummary()
                {
                    ColumnInfo = set.Columns,
                    Id = index,
                    RowCount = set.RowCount
                }).ToArray();
            }
        }

        /// <summary>
        /// The 0-indexed line number that this batch started on
        /// </summary>
        internal int StartLine { get; set; }

        /// <summary>
        /// Factory for creating readers/writrs for the output of the batch
        /// </summary>
        private IFileStreamFactory OutputFileFactory { get; set; }

        #endregion

        internal Batch(string batchText, int startLine, IFileStreamFactory outputFileFactory)
        {
            // Sanity check for input
            if (string.IsNullOrEmpty(batchText))
            {
                throw new ArgumentNullException(nameof(batchText), "Query text cannot be null");
            }
            if (outputFileFactory == null)
            {
                throw new ArgumentNullException(nameof(outputFileFactory), "Output file factory cannot be null");
            }

            // Initialize the internal state
            BatchText = batchText;
            StartLine = startLine - 1;  // -1 to make sure that the line number of the batch is 0-indexed, since SqlParser gives 1-indexed line numbers
            HasExecuted = false;
            resultSets = new List<ResultSet>();
            resultMessages = new List<string>();
            OutputFileFactory = outputFileFactory;
        }

        /// <summary>
        /// Executes this batch and captures any server messages that are returned.
        /// </summary>
        /// <param name="conn">The connection to use to execute the batch</param>
        /// <param name="cancellationToken">Token for cancelling the execution</param>
        public async Task Execute(DbConnection conn, CancellationToken cancellationToken)
        {
            // Sanity check to make sure we haven't already run this batch
            if (HasExecuted)
            {
                throw new InvalidOperationException("Batch has already executed.");
            }

            try
            {
                // Register the message listener to *this instance* of the batch
                // Note: This is being done to associate messages with batches
                SqlConnection sqlConn = conn as SqlConnection;
                if (sqlConn != null)
                {
                    sqlConn.InfoMessage += StoreDbMessage;
                }

                // Create a command that we'll use for executing the query
                using (DbCommand command = conn.CreateCommand())
                {
                    command.CommandText = BatchText;
                    command.CommandType = CommandType.Text;

                    // Execute the command to get back a reader
                    using (DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        do
                        {
                            // Skip this result set if there aren't any rows
                            if (!reader.HasRows && reader.FieldCount == 0)
                            {
                                // Create a message with the number of affected rows -- IF the query affects rows
                                resultMessages.Add(reader.RecordsAffected >= 0
                                    ? string.Format(RowsAffectedFormat, reader.RecordsAffected)
                                    : "Command(s) completed successfully.");
                                continue;
                            }

                            // Read until we hit the end of the result set
                            ResultSet resultSet = new ResultSet(reader, OutputFileFactory);
                            await resultSet.ReadResultToEnd(cancellationToken);

                            // Add the result set to the results of the query
                            resultSets.Add(resultSet);

                            // Add a message for the number of rows the query returned
                            resultMessages.Add(string.Format(RowsAffectedFormat, resultSet.RowCount));
                        } while (await reader.NextResultAsync(cancellationToken));
                    }
                }
            }
            catch (DbException dbe)
            {
                HasError = true;
                UnwrapDbException(dbe);
            }
            catch (Exception)
            {
                HasError = true;
                throw;
            }
            finally
            {
                // Remove the message event handler from the connection
                SqlConnection sqlConn = conn as SqlConnection;
                if (sqlConn != null)
                {
                    sqlConn.InfoMessage -= StoreDbMessage;
                }

                // Mark that we have executed
                HasExecuted = true;
            }
        }

        /// <summary>
        /// Generates a subset of the rows from a result set of the batch
        /// </summary>
        /// <param name="resultSetIndex">The index for selecting the result set</param>
        /// <param name="startRow">The starting row of the results</param>
        /// <param name="rowCount">How many rows to retrieve</param>
        /// <returns>A subset of results</returns>
        public Task<ResultSetSubset> GetSubset(int resultSetIndex, int startRow, int rowCount)
        {
            // Sanity check to make sure we have valid numbers
            if (resultSetIndex < 0 || resultSetIndex >= resultSets.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(resultSetIndex), "Result set index cannot be less than 0" +
                                                                             "or greater than the number of result sets");
            }

            // Retrieve the result set
            return resultSets[resultSetIndex].GetSubset(startRow, rowCount);
        }

        #region Private Helpers

        /// <summary>
        /// Delegate handler for storing messages that are returned from the server
        /// NOTE: Only messages that are below a certain severity will be returned via this
        /// mechanism. Anything above that level will trigger an exception.
        /// </summary>
        /// <param name="sender">Object that fired the event</param>
        /// <param name="args">Arguments from the event</param>
        private void StoreDbMessage(object sender, SqlInfoMessageEventArgs args)
        {
            resultMessages.Add(args.Message);
        }

        /// <summary>
        /// Attempts to convert a <see cref="DbException"/> to a <see cref="SqlException"/> that
        /// contains much more info about Sql Server errors. The exception is then unwrapped and
        /// messages are formatted and stored in <see cref="ResultMessages"/>. If the exception
        /// cannot be converted to SqlException, the message is written to the messages list.
        /// </summary>
        /// <param name="dbe">The exception to unwrap</param>
        private void UnwrapDbException(DbException dbe)
        {
            SqlException se = dbe as SqlException;
            if (se != null)
            {
                foreach (var error in se.Errors)
                {
                    SqlError sqlError = error as SqlError;
                    if (sqlError != null)
                    {
                        int lineNumber = sqlError.LineNumber + StartLine;
                        string message = String.Format("Msg {0}, Level {1}, State {2}, Line {3}{4}{5}",
                            sqlError.Number, sqlError.Class, sqlError.State, lineNumber,
                            Environment.NewLine, sqlError.Message);
                        resultMessages.Add(message);
                    }
                }
            }
            else
            {
                resultMessages.Add(dbe.Message);
            }
        }

        #endregion

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
                foreach (ResultSet r in ResultSets)
                {
                    r.Dispose();
                }
            }

            disposed = true;
        }

        ~Batch()
        {
            Dispose(false);
        }

        #endregion
    }
}
