// 
// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts;
using Microsoft.Kusto.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Class that represents a resultset the was generated from a query. Contains logic for
    /// storing and retrieving results. Is contained by a Batch class.
    /// </summary>
    public class ResultSet : IDisposable
    {
        #region Constants

        // Column names of 'for xml' and 'for json' queries
        private const string NameOfForXmlColumn = "XML_F52E2B61-18A1-11d1-B105-00805F49916B";
        private const string NameOfForJsonColumn = "JSON_F52E2B61-18A1-11d1-B105-00805F49916B";
        private const string YukonXmlShowPlanColumn = "Microsoft SQL Server 2005 XML Showplan";
        private const uint MaxResultsTimerPulseMilliseconds = 1000;
        private const uint MinResultTimerPulseMilliseconds = 10;
        #endregion

        #region Member Variables

        /// <summary>
        /// For IDisposable pattern, whether or not object has been disposed
        /// </summary>
        private bool disposed;

        /// <summary>
        /// A list of offsets into the buffer file that correspond to where rows start
        /// </summary>
        private readonly LongList<long> fileOffsets;

        /// <summary>
        /// The factory to use to get reading/writing handlers
        /// </summary>
        private readonly IFileStreamFactory fileStreamFactory;

        /// <summary>
        /// Whether or not the result set has been read in from the database,
        /// set as internal in order to fake value in unit tests.
        /// This gets set as soon as we start reading.
        /// </summary>
        internal bool hasStartedRead = false;

        /// <summary>
        /// Set when all results have been read for this resultSet from the server
        /// </summary>
        private bool hasCompletedRead = false;

        /// <summary>
        /// The name of the temporary file we're using to output these results in
        /// </summary>
        private readonly string outputFileName;

        /// <summary>
        /// Row count to use in special scenarios where we want to override the number of rows.
        /// </summary>
        private long? rowCountOverride=null;

        /// <summary>
        /// The special action which applied to this result set
        /// </summary>
        private readonly SpecialAction specialAction;

        /// <summary>
        /// Total number of bytes written to the file. Used to jump to end of the file for append
        /// scenarios. Internal for unit test validation.
        /// </summary>
        internal long totalBytesWritten;

        private readonly Timer resultsTimer;

        private readonly SemaphoreSlim sendResultsSemphore = new SemaphoreSlim(1);

        #endregion

        /// <summary>
        /// Creates a new result set and initializes its state
        /// </summary>
        /// <param name="ordinal">The ID of the resultset, the ordinal of the result within the batch</param>
        /// <param name="batchOrdinal">The ID of the batch, the ordinal of the batch within the query</param>
        /// <param name="factory">Factory for creating a reader/writer</param>
        public ResultSet(int ordinal, int batchOrdinal, IFileStreamFactory factory)
        {
            Id = ordinal;
            BatchId = batchOrdinal;

            // Initialize the storage
            totalBytesWritten = 0;
            outputFileName = factory.CreateFile();
            fileOffsets = new LongList<long>();
            specialAction = new SpecialAction();

            // Store the factory
            fileStreamFactory = factory;
            hasStartedRead = false;
            hasCompletedRead = false;
            SaveTasks = new ConcurrentDictionary<string, Task>();
            resultsTimer = new Timer(SendResultAvailableOrUpdated);
        }

        #region Eventing

        /// <summary>
        /// Asynchronous handler for when saving query results succeeds
        /// </summary>
        /// <param name="parameters">Request parameters for identifying the request</param>
        public delegate Task SaveAsAsyncEventHandler(SaveResultsRequestParams parameters);

        /// <summary>
        /// Asynchronous handler for when saving query results fails
        /// </summary>
        /// <param name="parameters">Request parameters for identifying the request</param>
        /// <param name="message">Message to send back describing why the request failed</param>
        public delegate Task SaveAsFailureAsyncEventHandler(SaveResultsRequestParams parameters, string message);

        /// <summary>
        /// Asynchronous handler for when a resultset is available/updated/completed
        /// </summary>
        /// <param name="resultSet">The result set that completed</param>
        public delegate Task ResultSetAsyncEventHandler(ResultSet resultSet);

        /// <summary>
        /// Event that will be called when the result set has completed execution
        /// </summary>
        public event ResultSetAsyncEventHandler ResultCompletion;

        /// <summary>
        /// Event that will be called when the resultSet first becomes available. This is as soon as we start reading the results.
        /// </summary>
        public event ResultSetAsyncEventHandler ResultAvailable;

        /// <summary>
        /// Event that will be called when additional rows in the result set are available (rowCount available has increased)
        /// </summary>
        public event ResultSetAsyncEventHandler ResultUpdated;


        #endregion

        #region Properties

        /// <summary>
        /// The columns for this result set
        /// </summary>
        public DbColumnWrapper[] Columns { get; private set; }

        /// <summary>
        /// ID of the result set, relative to the batch
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// ID of the batch set, relative to the query
        /// </summary>
        public int BatchId { get; private set; }

        /// <summary>
        /// The number of rows for this result set
        /// </summary>
        public long RowCount => rowCountOverride != null ? Math.Min(rowCountOverride.Value, fileOffsets.Count) : fileOffsets.Count;

        /// <summary>
        /// All save tasks currently saving this ResultSet
        /// </summary>
        internal ConcurrentDictionary<string, Task> SaveTasks { get; set; }

        /// <summary>
        /// Generates a summary of this result set
        /// </summary>
        public ResultSetSummary Summary
        {
            get
            {
                return new ResultSetSummary
                {
                    ColumnInfo = Columns,
                    Id = Id,
                    BatchId = BatchId,
                    RowCount = RowCount,
                    Complete = hasCompletedRead,
                    SpecialAction = hasCompletedRead ? ProcessSpecialAction() : null
                };
            }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Generates a subset of the rows from the result set
        /// </summary>
        /// <param name="startRow">The starting row of the results</param>
        /// <param name="rowCount">How many rows to retrieve</param>
        /// <returns>A subset of results</returns>
        public Task<ResultSetSubset> GetSubset(long startRow, int rowCount)
        {
            // Sanity check to make sure that results read has started
            if (!hasStartedRead)
            {
                throw new InvalidOperationException(SR.QueryServiceResultSetNotRead);
            }

            // Sanity check to make sure that the row and the row count are within bounds
            if (startRow < 0 || startRow >= RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(startRow), SR.QueryServiceResultSetStartRowOutOfRange);
            }
            if (rowCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowCount), SR.QueryServiceResultSetRowCountOutOfRange);
            }

            return Task.Factory.StartNew(() =>
            {

                DbCellValue[][] rows = null;

                using (IFileStreamReader fileStreamReader = fileStreamFactory.GetReader(outputFileName))
                {
                    // Figure out which rows we need to read back
                    IEnumerable<long> rowOffsets = fileOffsets.LongSkip(startRow).Take(rowCount);

                    // Iterate over the rows we need and process them into output
                    // ReSharper disable once AccessToDisposedClosure   The lambda is used immediately in .ToArray call
                    rows = rowOffsets.Select((offset, id) => fileStreamReader.ReadRow(offset, id, Columns).ToArray()).ToArray();
                }
                // Retrieve the subset of the results as per the request
                return new ResultSetSubset
                {
                    Rows = rows,
                    RowCount = rows.Length
                };
            });
        }

        /// <summary>
        /// Generates the execution plan from the table returned 
        /// </summary>
        /// <returns>An execution plan object</returns>
        public Task<ExecutionPlan> GetExecutionPlan()
        {
            // Process the action just in case it hasn't been yet 
            ProcessSpecialAction();

            // Sanity check to make sure that results read has started
            if (!hasStartedRead)
            {
                throw new InvalidOperationException(SR.QueryServiceResultSetNotRead);
            }
            // Check that we this result set contains a showplan 
            if (!specialAction.ExpectYukonXMLShowPlan)
            {
                throw new Exception(SR.QueryServiceExecutionPlanNotFound);
            }


            return Task.Factory.StartNew(() =>
            { 
                string content;
                string format = null;

                using (IFileStreamReader fileStreamReader = fileStreamFactory.GetReader(outputFileName))
                {
                    // Determine the format and get the first col/row of XML
                    content = fileStreamReader.ReadRow(0, 0, Columns)[0].DisplayValue;

                    if (specialAction.ExpectYukonXMLShowPlan) 
                    {
                        format = "xml";
                    }
                }
                    
                return new ExecutionPlan
                {
                    Format = format,
                    Content = content
                };
            });
        }

        /// <summary>
        /// Reads from the reader until there are no more results to read
        /// </summary>
        /// <param name="dbDataReader">The data reader for getting results from the db</param>
        /// <param name="cancellationToken">Cancellation token for cancelling the query</param>
        public async Task ReadResultToEnd(IDataReader dbDataReader, CancellationToken cancellationToken)
        {
            // Sanity check to make sure we got a reader
            //
            Validate.IsNotNull(nameof(dbDataReader), dbDataReader);

            Task availableTask = null;
            try
            {
                // Verify the request hasn't been cancelled
                cancellationToken.ThrowIfCancellationRequested();

                StorageDataReader dataReader = new StorageDataReader(dbDataReader);

                // Open a writer for the file
                //
                var fileWriter = fileStreamFactory.GetWriter(outputFileName);
                using (fileWriter)
                {
                    Columns = dataReader.Columns;

                    // Mark that read of result has started
                    //
                    hasStartedRead = true;

                    // Invoke the SendCurrentResults() asynchronously that will send the results available notification
                    //   and also trigger the timer to send periodic updates.
                    //
                    availableTask = SendCurrentResults();

                    while (await dataReader.ReadAsync(cancellationToken))
                    {
                        fileOffsets.Add(totalBytesWritten);
                        totalBytesWritten += fileWriter.WriteRow(dataReader);
                    }
                }
            }
            finally
            {

                // await the completion of available notification in case it is not already done before proceeding
                //
                await availableTask;

                // now set the flag to indicate that we are done reading. this equates to Complete flag to be marked 'True' in any future notifications.
                //
                hasCompletedRead = true;


                // Make a final call to SendCurrentResults() and await its completion. If the previously scheduled task already took care of latest status send then this should be a no-op
                //
                await SendCurrentResults();


                // and finally:
                // Make a call to send ResultCompletion and await its completion. This is just for backward compatibility with older protocol
                //
                await (ResultCompletion?.Invoke(this) ?? Task.CompletedTask);
            }
        }

        /// <summary>
        /// Saves the contents of this result set to a file using the IFileStreamFactory provided
        /// </summary>
        /// <param name="saveParams">Parameters for saving the results to a file</param>
        /// <param name="fileFactory">
        /// Factory for creating a stream reader/writer combo for writing results to disk
        /// </param>
        /// <param name="successHandler">Handler for a successful write of all rows</param>
        /// <param name="failureHandler">Handler for unsuccessful write of all rows</param>
        public void SaveAs(SaveResultsRequestParams saveParams, IFileStreamFactory fileFactory,
            SaveAsAsyncEventHandler successHandler, SaveAsFailureAsyncEventHandler failureHandler)
        {
            // Sanity check the save params and file factory
            Validate.IsNotNull(nameof(saveParams), saveParams);
            Validate.IsNotNull(nameof(fileFactory), fileFactory);

            // Make sure the resultset has finished being read
            if (!hasCompletedRead)
            {
                throw new InvalidOperationException(SR.QueryServiceSaveAsResultSetNotComplete);
            }

            // Make sure there isn't a task for this file already
            Task existingTask;
            if (SaveTasks.TryGetValue(saveParams.FilePath, out existingTask))
            {
                if (existingTask.IsCompleted)
                {
                    // The task has completed, so let's attempt to remove it
                    if (!SaveTasks.TryRemove(saveParams.FilePath, out existingTask))
                    {
                        throw new InvalidOperationException(SR.QueryServiceSaveAsMiscStartingError);
                    }
                }
                else
                {
                    // The task hasn't completed, so we shouldn't continue
                    throw new InvalidOperationException(SR.QueryServiceSaveAsInProgress);
                }
            }

            // Create the new task
            Task saveAsTask = new Task(async () =>
            {
                try
                {
                    // Set row counts depending on whether save request is for entire set or a subset
                    long rowEndIndex = RowCount;
                    int rowStartIndex = 0;
                    if (saveParams.IsSaveSelection)
                    {
                        // ReSharper disable PossibleInvalidOperationException  IsSaveSelection verifies these values exist
                        rowEndIndex = saveParams.RowEndIndex.Value + 1;
                        rowStartIndex = saveParams.RowStartIndex.Value;
                        // ReSharper restore PossibleInvalidOperationException
                    }

                    using (var fileReader = fileFactory.GetReader(outputFileName))
                    using (var fileWriter = fileFactory.GetWriter(saveParams.FilePath))
                    {
                        // Iterate over the rows that are in the selected row set
                        for (long i = rowStartIndex; i < rowEndIndex; ++i)
                        {
                            var row = fileReader.ReadRow(fileOffsets[i], i, Columns);
                            fileWriter.WriteRow(row, Columns);
                        }
                        if (successHandler != null)
                        {
                            await successHandler(saveParams);
                        }
                    }
                }
                catch (Exception e)
                {
                    fileFactory.DisposeFile(saveParams.FilePath);
                    if (failureHandler != null)
                    {
                        await failureHandler(saveParams, e.Message);
                    }
                }
            });
            
            // Add exception handling to the save task
            Task taskWithHandling = saveAsTask.ContinueWithOnFaulted(async t =>
            {
                if (failureHandler != null)
                {
                    await failureHandler(saveParams, t.Exception.Message);
                }
            });

            // If saving the task fails, return a failure
            if (!SaveTasks.TryAdd(saveParams.FilePath, taskWithHandling))
            {
                throw new InvalidOperationException(SR.QueryServiceSaveAsMiscStartingError);
            }

            // Task was saved, so start up the task
            saveAsTask.Start();
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            resultsTimer.Dispose();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            // Check if saveTasks are running for this ResultSet
            if (!SaveTasks.IsEmpty)
            {
                // Wait for tasks to finish before disposing ResultSet
                Task.WhenAll(SaveTasks.Values.ToArray()).ContinueWith(antecedent =>
                {
                    if (disposing)
                    {
                        fileStreamFactory.DisposeFile(outputFileName);
                    }
                    disposed = true;
                });
            }
            else
            {
                // If saveTasks is empty, continue with dispose
                if (disposing)
                {
                    fileStreamFactory.DisposeFile(outputFileName);
                }
                disposed = true;
            }
        }

        #endregion

        #region Private Helper Methods
        /// <summary>
        /// Sends the ResultsUpdated message if the number of rows has changed since last send.
        /// </summary>
        /// <param name="stateInfo"></param>
        private void SendResultAvailableOrUpdated (object stateInfo = null)
        {
            // Make the call to send current results and synchronously wait for it to finish
            //
            SendCurrentResults().Wait();
        }

        private async Task SendCurrentResults()
        {
            try
            {

                // Wait to acquire the sendResultsSemphore before proceeding, as we want only one instance of this method executing at any given time.
                //
                sendResultsSemphore.Wait();

                ResultSet currentResultSetSnapshot = (ResultSet) MemberwiseClone();
                if (LastUpdatedSummary == null) // We need to send results available message.
                {
                    // Fire off results Available task and await it
                    //
                    await (ResultAvailable?.Invoke(currentResultSetSnapshot) ?? Task.CompletedTask);
                }
                else if (LastUpdatedSummary.Complete) // If last result summary sent had already set the Complete flag
                {
                    // We do not need to do anything except that make sure that RowCount has not update since last send.
                    Debug.Assert(LastUpdatedSummary.RowCount == currentResultSetSnapshot.RowCount,
                        $"Already reported rows should be equal to current RowCount, if had already sent completion flag as set in last message, countReported:{LastUpdatedSummary.RowCount}, current total row count: {currentResultSetSnapshot.RowCount}, row count override: {currentResultSetSnapshot.rowCountOverride}, this.rowCountOverride: {this.rowCountOverride} and this.RowCount: {this.RowCount}, LastUpdatedSummary: {LastUpdatedSummary}");
                }
                else // We need to send results updated message.
                {
                    // Previously reported rows should be less than or equal to current number of rows about to be reported
                    //
                    Debug.Assert(LastUpdatedSummary.RowCount <= currentResultSetSnapshot.RowCount,
                        $"Already reported rows should less than or equal to current total RowCount, countReported:{LastUpdatedSummary.RowCount}, current total row count: {currentResultSetSnapshot.RowCount}, row count override: {currentResultSetSnapshot.rowCountOverride}, this.rowCountOverride: {this.rowCountOverride} and this.RowCount: {this.RowCount}, LastUpdatedSummary: {LastUpdatedSummary}");

                    // If there has been no change in rowCount since last update and we have not yet completed read then log and increase the timer duration
                    //
                    if (!currentResultSetSnapshot.hasCompletedRead &&
                        LastUpdatedSummary.RowCount == currentResultSetSnapshot.RowCount)
                    {
                        Logger.Write(TraceEventType.Warning,
                            $"The result set:{Summary} has not made any progress in last {ResultTimerInterval} milliseconds and the read of this result set is not yet complete!");
                        ResultsIntervalMultiplier++;
                    }

                    // Fire off results updated task and await it
                    //
                    await (ResultUpdated?.Invoke(currentResultSetSnapshot) ?? Task.CompletedTask);
                }

                // Update the LastUpdatedSummary to be the value captured in current snapshot
                //
                LastUpdatedSummary = currentResultSetSnapshot.Summary;

                // Setup timer for the next callback
                //
                if (currentResultSetSnapshot.hasCompletedRead)
                {
                    // If we have already completed reading then we are done and we do not need to send any more updates. Switch off timer.
                    //
                    resultsTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                else
                {
                    // If we have not yet completed reading then set the timer so this method gets called again after ResultTimerInterval milliseconds
                    //
                    resultsTimer.Change(ResultTimerInterval, Timeout.Infinite);
                }
            }
            finally
            { 
                // Release the sendResultsSemphore so the next invocation gets unblocked
                //
                sendResultsSemphore.Release();
            }
        }

        private uint ResultsIntervalMultiplier { get; set; } = 1;

        internal uint ResultTimerInterval => Math.Max(Math.Min(MaxResultsTimerPulseMilliseconds, (uint)RowCount / 500 /* 1 millisec per 500 rows*/), MinResultTimerPulseMilliseconds * ResultsIntervalMultiplier);

        internal ResultSetSummary LastUpdatedSummary { get; set; } = null;
        

        /// <summary>
        /// Determine the special action, if any, for this result set
        /// </summary>
        private SpecialAction ProcessSpecialAction() 
        {           

            // Check if this result set is a showplan 
            if (Columns.Length == 1 && string.Compare(Columns[0].ColumnName, YukonXmlShowPlanColumn, StringComparison.OrdinalIgnoreCase) == 0)
            {
                specialAction.ExpectYukonXMLShowPlan = true;
            }

            return specialAction;
        }

        #endregion
    }
}
