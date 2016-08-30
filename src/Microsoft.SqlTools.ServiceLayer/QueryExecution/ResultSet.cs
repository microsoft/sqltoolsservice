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

        public DbColumnWrapper[] Columns { get; set; }

        public long RowCount { get; set; }

        private readonly string bufferFileName;

        private readonly IFileStreamFactory fileStreamFactory;

        private StorageDataReader DataReader { get; set; }

        public bool HasLongFields { get; private set; }

        private LongList<long> FileOffsets { get; set; }

        private long currentFileOffset;

        public int MaxCharsToStore { get; set; }

        public int MaxXmlCharsToStore { get; set; }

        #endregion

        public ResultSet(DbDataReader reader, IFileStreamFactory factory)
        {
            // Sanity check to make sure we got a reader
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader), "Reader cannot be null");
            }
            DataReader = new StorageDataReader(reader);

            // Initialize the storage
            bufferFileName = Path.GetTempFileName();
            if (bufferFileName.Length == 0)
            {
                throw new FileNotFoundException("Failed to get buffer file name");
            }
            FileOffsets = new LongList<long>();

            // Store the factory
            fileStreamFactory = factory;
        }

        public async Task ReadResultToEnd(CancellationToken cancellationToken)
        {
            // Open a writer for the file
            using (IFileStreamWriter fileWriter = fileStreamFactory.GetWriter(bufferFileName, MaxCharsToStore, MaxXmlCharsToStore))
            {

                // If we can initialize the columns using the column schema, use that
                if (!DataReader.DbDataReader.CanGetColumnSchema())
                {
                    throw new InvalidOperationException("Could not retrieve column schema for result set.");
                }
                Columns = DataReader.DbDataReader.GetColumnSchema().Select(column => new DbColumnWrapper(column)).ToArray();
                HasLongFields = Columns.Any(column => column.IsLong.HasValue && column.IsLong.Value);

                while (await DataReader.ReadAsync(cancellationToken))
                {
                    RowCount++;
                    FileOffsets.Add(currentFileOffset);
                    currentFileOffset += await fileWriter.WriteRow(DataReader, Columns);
                }
            }
        }

        /// <summary>
        /// Generates a subset of the rows from the result set
        /// </summary>
        /// <param name="startRow">The starting row of the results</param>
        /// <param name="rowCount">How many rows to retrieve</param>
        /// <returns>A subset of results</returns>
        public async Task<ResultSetSubset> GetSubset(int startRow, int rowCount)
        {
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
            using (IFileStreamReader fileReader = fileStreamFactory.GetReader(bufferFileName))
            {
                IEnumerable<long> rowOffsets = FileOffsets.Skip(startRow).Take(rowCount);

                // Iterate over the rows we need and process them into output
                List<object[]> rows = new List<object[]>();
                foreach (long rowOffset in rowOffsets)
                {
                    rows.Add(await fileReader.ReadRow(rowOffset, Columns));
                }

                // Retrieve the subset of the results as per the request
                return new ResultSetSubset
                {
                    Rows = rows.ToArray(),
                    RowCount = rows.Count
                };
            }
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
                // TODO: Cleanup the file that we opened to buffer results
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
