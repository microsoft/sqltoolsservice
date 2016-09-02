//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
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
        #region Properties

        /// <summary>
        /// The name of the temporary file we're using to output these results in
        /// </summary>
        private readonly string outputFileName;

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
        /// The factory to use to get reading/writing handlers
        /// </summary>
        private readonly IFileStreamFactory fileStreamFactory;

        /// <summary>
        /// File stream reader that will be reused to make rapid-fire retrieval of result subsets
        /// quick and low perf impact.
        /// </summary>
        private IFileStreamReader fileStreamReader;

        /// <summary>
        /// Whether or not the 
        /// </summary>
        private bool HasBeenRead { get; set; }

        /// <summary>
        /// Maximum number of characters to store for a field
        /// </summary>
        public int MaxCharsToStore { get; set; }

        /// <summary>
        /// Maximum number of characters to store for an XML field
        /// </summary>
        public int MaxXmlCharsToStore { get; set; }

        /// <summary>
        /// The number of rows for this result set
        /// </summary>
        public long RowCount { get; private set; }

        #endregion

        /// <summary>
        /// Creates a new result set and initializes its state
        /// </summary>
        /// <param name="reader">The reader from executing a query</param>
        /// <param name="factory">Factory for creating a reader/writer</param>
        public ResultSet(DbDataReader reader, IFileStreamFactory factory)
        {
            // Sanity check to make sure we got a reader
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader), "Reader cannot be null");
            }
            DataReader = new StorageDataReader(reader);

            // Initialize the storage
            outputFileName = Path.GetTempFileName();
            if (outputFileName.Length == 0)
            {
                throw new FileNotFoundException("Failed to get output file name");
            }
            FileOffsets = new LongList<long>();

            // Store the factory
            fileStreamFactory = factory;
            HasBeenRead = false;
        }

        /// <summary>
        /// Reads from the reader until there are no more results to read
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for cancelling the query</param>
        public async Task ReadResultToEnd(CancellationToken cancellationToken)
        {
            // Open a writer for the file
            using (IFileStreamWriter fileWriter = fileStreamFactory.GetWriter(outputFileName, MaxCharsToStore, MaxXmlCharsToStore))
            {
                // If we can initialize the columns using the column schema, use that
                if (!DataReader.DbDataReader.CanGetColumnSchema())
                {
                    throw new InvalidOperationException("Could not retrieve column schema for result set.");
                }
                Columns = DataReader.Columns;
                long currentFileOffset = 0;

                while (await DataReader.ReadAsync(cancellationToken))
                {
                    RowCount++;
                    FileOffsets.Add(currentFileOffset);
                    currentFileOffset += await fileWriter.WriteRow(DataReader);
                }
            }

            // Mark that result has been read
            HasBeenRead = true;
            fileStreamReader = fileStreamFactory.GetReader(outputFileName);
        }

        /// <summary>
        /// Generates a subset of the rows from the result set
        /// </summary>
        /// <param name="startRow">The starting row of the results</param>
        /// <param name="rowCount">How many rows to retrieve</param>
        /// <returns>A subset of results</returns>
        public async Task<ResultSetSubset> GetSubset(int startRow, int rowCount)
        {
            // Sanity check to make sure that the results have been read beforehand
            if (!HasBeenRead || fileStreamReader == null)
            {
                throw new InvalidOperationException("Cannot read subset unless the results have been read from the server");
            }

            // Sanity check to make sure that the row and the row count are within bounds
            if (startRow < 0 || startRow >= RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(startRow), "Start row cannot be less than 0 " +
                                                                        "or greater than the number of rows in the resultset");
            }
            if (rowCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowCount), "Row count must be a positive integer");
            }

            // Figure out which rows we need to read back
            IEnumerable<long> rowOffsets = FileOffsets.Skip(startRow).Take(rowCount);

            // Iterate over the rows we need and process them into output
            List<object[]> rows = new List<object[]>();
            foreach (long rowOffset in rowOffsets)
            {
                rows.Add(await fileStreamReader.ReadRow(rowOffset, Columns));
            }

            // Retrieve the subset of the results as per the request
            return new ResultSetSubset
            {
                Rows = rows.ToArray(),
                RowCount = rows.Count
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
                fileStreamReader?.Dispose();
                fileStreamFactory.DisposeFile(outputFileName);
            }

            disposed = true;
        }

        ~ResultSet()
        {
            Dispose(false);
        }

        #endregion
    }
}
