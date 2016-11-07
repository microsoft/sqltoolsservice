// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    public class ResultSet : IDisposable
    {
        #region Constants

        // Column names of 'for xml' and 'for json' queries
        private const string NameOfForXMLColumn = "XML_F52E2B61-18A1-11d1-B105-00805F49916B";
        private const string NameOfForJSONColumn = "JSON_F52E2B61-18A1-11d1-B105-00805F49916B";

        #endregion

        #region Member Variables

        /// <summary>
        /// For IDisposable pattern, whether or not object has been disposed
        /// </summary>
        private bool disposed;

        /// <summary>
        /// The factory to use to get reading/writing handlers
        /// </summary>
        private readonly IFileStreamFactory fileStreamFactory;

        /// <summary>
        /// Whether or not the result set has been read in from the database
        /// </summary>
        private bool hasBeenRead;

        /// <summary>
        /// Whether resultSet is a 'for xml' or 'for json' result
        /// </summary>
        private bool isSingleColumnXmlJsonResultSet;

        /// <summary>
        /// The name of the temporary file we're using to output these results in
        /// </summary>
        private readonly string outputFileName;

        /// <summary>
        /// Whether the resultSet is in the process of being disposed
        /// </summary>
        private bool isBeingDisposed;

        /// <summary>
        /// All save tasks currently saving this ResultSet
        /// </summary>
        private readonly ConcurrentDictionary<string, Task> saveTasks;

        #endregion

        /// <summary>
        /// Creates a new result set and initializes its state
        /// </summary>
        /// <param name="reader">The reader from executing a query</param>
        /// <param name="factory">Factory for creating a reader/writer</param>
        public ResultSet(DbDataReader reader, IFileStreamFactory factory)
        {
            // Sanity check to make sure we got a reader
            Validate.IsNotNull(nameof(reader), SR.QueryServiceResultSetReaderNull);

            DataReader = new StorageDataReader(reader);

            // Initialize the storage
            outputFileName = factory.CreateFile();
            FileOffsets = new LongList<long>();

            // Store the factory
            fileStreamFactory = factory;
            hasBeenRead = false;
            saveTasks = new ConcurrentDictionary<string, Task>();
        }

        #region Properties

        public delegate Task SaveAsAsyncEventHandler(SaveResultsRequestParams parameters);

        public delegate Task SaveAsFailureAsyncEventHandler(
            SaveResultsRequestParams pararmeters, Exception thrownException);

        /// <summary>
        /// Whether the resultSet is in the process of being disposed
        /// </summary>
        /// <returns></returns>
        internal bool IsBeingDisposed
        {
            get
            {
                return isBeingDisposed;
            }
        }

        /// <summary>
        /// The columns for this result set
        /// </summary>
        public DbColumnWrapper[] Columns { get; private set; }

        /// <summary>
        /// The reader to use for this resultset
        /// </summary>
        private StorageDataReader DataReader { get; set; }

        /// <summary>
        /// A list of offsets into the buffer file that correspond to where rows start
        /// </summary>
        private LongList<long> FileOffsets { get; set; }

        /// <summary>
        /// The number of rows for this result set
        /// </summary>
        public long RowCount { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates a subset of the rows from the result set
        /// </summary>
        /// <param name="startRow">The starting row of the results</param>
        /// <param name="rowCount">How many rows to retrieve</param>
        /// <returns>A subset of results</returns>
        public Task<ResultSetSubset> GetSubset(int startRow, int rowCount)
        {
            // Sanity check to make sure that the results have been read beforehand
            if (!hasBeenRead)
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

                string[][] rows;

                using (IFileStreamReader fileStreamReader = fileStreamFactory.GetReader(outputFileName))
                {
                    // If result set is 'for xml' or 'for json',
                    // Concatenate all the rows together into one row
                    if (isSingleColumnXmlJsonResultSet)
                    {
                        // Iterate over all the rows and process them into a list of string builders
                        IEnumerable<string> rowValues = FileOffsets.Select(rowOffset => fileStreamReader.ReadRow(rowOffset, Columns)[0].DisplayValue);
                        rows = new[] { new[] { string.Join(string.Empty, rowValues) } };

                    }
                    else
                    {
                        // Figure out which rows we need to read back
                        IEnumerable<long> rowOffsets = FileOffsets.Skip(startRow).Take(rowCount);

                        // Iterate over the rows we need and process them into output
                        rows = rowOffsets.Select(rowOffset =>
                            fileStreamReader.ReadRow(rowOffset, Columns).Select(cell => cell.DisplayValue).ToArray())
                            .ToArray();

                    }
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
        /// Reads from the reader until there are no more results to read
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for cancelling the query</param>
        public async Task ReadResultToEnd(CancellationToken cancellationToken)
        {
            // Mark that result has been read
            hasBeenRead = true;

            // Open a writer for the file
            using (IFileStreamWriter fileWriter = fileStreamFactory.GetWriter(outputFileName))
            {
                // If we can initialize the columns using the column schema, use that
                if (!DataReader.DbDataReader.CanGetColumnSchema())
                {
                    throw new InvalidOperationException(SR.QueryServiceResultSetNoColumnSchema);
                }
                Columns = DataReader.Columns;
                long currentFileOffset = 0;

                while (await DataReader.ReadAsync(cancellationToken))
                {
                    RowCount++;
                    FileOffsets.Add(currentFileOffset);
                    currentFileOffset += fileWriter.WriteRow(DataReader);
                }
            }
            // Check if resultset is 'for xml/json'. If it is, set isJson/isXml value in column metadata
            SingleColumnXmlJsonResultSet();
        }

        public void SaveAsCsv(SaveResultsAsCsvRequestParams saveParams, IFileStreamFactory csvFactory, 
            SaveAsAsyncEventHandler successHandler, SaveAsFailureAsyncEventHandler failureHandler)
        {
            // Make sure there isn't a task for this file already
            Task existingTask;
            if (saveTasks.TryGetValue(saveParams.FilePath, out existingTask))
            {
                if (existingTask.IsCompleted)
                {
                    // The task has completed, so let's attempt to remove it
                    if (!saveTasks.TryRemove(saveParams.FilePath, out existingTask))
                    {
                        failureHandler?.Invoke(saveParams, null);
                        return;
                    }
                }
                else
                {
                    // The task hasn't completed, so we shouldn't continue
                    failureHandler?.Invoke(saveParams, null);
                    return;
                }
            }

            // Create the new task
            Task saveAsTask = new Task(() =>
            {
                try
                {
                    // Set row counts depending on whether save request is for entire set or a subset
                    long rowEndIndex = RowCount;
                    int rowStartIndex = 0;
                    if (saveParams.IsSaveSelection)
                    {
                        // ReSharper disable PossibleInvalidOperationException  IsSaveSelection verifies these values exist
                        rowEndIndex = saveParams.RowEndIndex.Value;
                        rowStartIndex = saveParams.RowStartIndex.Value;
                        // ReSharper restore PossibleInvalidOperationException
                    }

                    using (var fileReader = csvFactory.GetReader(outputFileName))
                    using (var fileWriter = csvFactory.GetWriter(saveParams.FilePath))
                    {
                        // Iterate over the rows that are in the selected row set
                        for (long i = rowStartIndex; i <= rowEndIndex; ++i)
                        {
                            fileWriter.WriteRow(fileReader.ReadRow(FileOffsets[i], Columns), Columns);
                        }
                        successHandler?.Invoke(saveParams).Wait();
                    }
                }
                catch (Exception e)
                {
                    csvFactory.DisposeFile(saveParams.FilePath);
                    failureHandler?.Invoke(saveParams, e).Wait();
                }
            });

            // If saving the task fails, return a failure
            if (!saveTasks.TryAdd(saveParams.FilePath, saveAsTask))
            {
                failureHandler?.Invoke(saveParams, null);
            }

            // Task was saved, so start up the task
            saveAsTask.Start();
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

            isBeingDisposed = true;
            // Check if saveTasks are running for this ResultSet
            if (!saveTasks.IsEmpty)
            {
                // Wait for tasks to finish before disposing ResultSet
                Task.WhenAll(saveTasks.Values.ToArray()).ContinueWith((antecedent) =>
                {
                    if (disposing)
                    {
                        fileStreamFactory.DisposeFile(outputFileName);
                    }
                    disposed = true;
                    isBeingDisposed = false;
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
                isBeingDisposed = false;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// If the result set represented by this class corresponds to a single XML
        /// column that contains results of "for xml" query, set isXml = true 
        /// If the result set represented by this class corresponds to a single JSON
        /// column that contains results of "for json" query, set isJson = true
        /// </summary>
        private void SingleColumnXmlJsonResultSet() {

            if (Columns?.Length == 1 && RowCount != 0)
            {   
                if (Columns[0].ColumnName.Equals(NameOfForXMLColumn, StringComparison.Ordinal))
                {
                    Columns[0].IsXml = true;
                    isSingleColumnXmlJsonResultSet = true;
                    RowCount = 1;
                }
                else if (Columns[0].ColumnName.Equals(NameOfForJSONColumn, StringComparison.Ordinal))
                {
                    Columns[0].IsJson = true;
                    isSingleColumnXmlJsonResultSet = true;
                    RowCount = 1;
                }                
            }
        }

        #endregion

        #region Internal Methods to Add and Remove save tasks
        internal void AddSaveTask(string key, Task saveTask)
        {
            saveTasks.TryAdd(key, saveTask);
        }

        internal void RemoveSaveTask(string key)
        {
            Task completedTask;
            saveTasks.TryRemove(key, out completedTask);
        }

        internal Task GetSaveTask(string key)
        {
            Task completedTask;
            saveTasks.TryRemove(key, out completedTask);
            return completedTask;
        }
        #endregion
    }
}
