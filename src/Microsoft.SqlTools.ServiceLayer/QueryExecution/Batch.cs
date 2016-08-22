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

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// This class represents a batch within a query
    /// </summary>
    public class Batch
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
        /// Messages that have come back from the server
        /// </summary>
        public List<string> ResultMessages { get; set; }

        /// <summary>
        /// The result sets of the batch execution
        /// </summary>
        public List<ResultSet> ResultSets { get; set; }

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
                    RowCount = set.Rows.Count
                }).ToArray();
            }
        }

        #endregion

        public Batch(string batchText)
        {
            // Sanity check for input
            if (string.IsNullOrEmpty(batchText))
            {
                throw new ArgumentNullException(nameof(batchText), "Query text cannot be null");
            }

            // Initialize the internal state
            BatchText = batchText;
            HasExecuted = false;
            ResultSets = new List<ResultSet>();
            ResultMessages = new List<string>();
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
                                ResultMessages.Add(reader.RecordsAffected >= 0
                                    ? string.Format(RowsAffectedFormat, reader.RecordsAffected)
                                    : "Command(s) completed successfully.");
                                continue;
                            }

                            // Read until we hit the end of the result set
                            ResultSet resultSet = new ResultSet();
                            while (await reader.ReadAsync(cancellationToken))
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

                            // Add a message for the number of rows the query returned
                            ResultMessages.Add(string.Format(RowsAffectedFormat, resultSet.Rows.Count));
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
        public ResultSetSubset GetSubset(int resultSetIndex, int startRow, int rowCount)
        {
            // Sanity check to make sure we have valid numbers
            if (resultSetIndex < 0 || resultSetIndex >= ResultSets.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(resultSetIndex), "Result set index cannot be less than 0" +
                                                                             "or greater than the number of result sets");
            }

            // Retrieve the result set
            return ResultSets[resultSetIndex].GetSubset(startRow, rowCount);
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
            ResultMessages.Add(args.Message);
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
                        string message = String.Format("Msg {0}, Level {1}, State {2}, Line {3}{4}{5}",
                            sqlError.Number, sqlError.Class, sqlError.State, sqlError.LineNumber,
                            Environment.NewLine, sqlError.Message);
                        ResultMessages.Add(message);
                    }
                }
            }
            else
            {
                ResultMessages.Add(dbe.Message);
            }
        }

        #endregion
    }
}
