//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Abstract class for implementing writers that save results to file. Stores some basic info
    /// that all save as writer would need.
    /// </summary>
    public abstract class SaveAsStreamWriter : IFileStreamWriter
    {
        /// <summary>
        /// Stores the internal state for the writer that will be necessary for any writer.
        /// </summary>
        /// <param name="stream">The stream that will be written to</param>
        /// <param name="requestParams">The SaveAs request parameters</param>
        /// <param name="columns">
        /// The entire list of columns for the result set. Used to determine which columns to
        /// output.
        /// </param>
        protected SaveAsStreamWriter(Stream stream, SaveResultsRequestParams requestParams, IReadOnlyList<DbColumnWrapper> columns)
        {
            Validate.IsNotNull(nameof(stream), stream);
            Validate.IsNotNull(nameof(columns), columns);

            FileStream = stream;
            if (requestParams.IsSaveSelection)
            {
                // ReSharper disable PossibleInvalidOperationException  IsSaveSelection verifies these values exist
                ColumnStartIndex = requestParams.ColumnStartIndex.Value;
                ColumnEndIndex = requestParams.ColumnEndIndex.Value;
                // ReSharper restore PossibleInvalidOperationException
            }
            else
            {
                // Save request was for the entire result set, use default start/end
                ColumnStartIndex = 0;
                ColumnEndIndex = columns.Count - 1;
            }

            ColumnCount = ColumnEndIndex - ColumnStartIndex + 1;
        }

        #region Properties

        /// <summary>
        /// Index of the first column to write to the output file
        /// </summary>
        protected int ColumnStartIndex { get; }

        /// <summary>
        /// Number of columns to write to the output file
        /// </summary>
        protected int ColumnCount { get; }

        /// <summary>
        /// Index of the last column to write to the output file (inclusive).
        /// </summary>
        protected int ColumnEndIndex { get; }

        /// <summary>
        /// The file stream to use to write the output file
        /// </summary>
        protected Stream FileStream { get; }

        #endregion

        /// <summary>
        /// Not implemented, do not use.
        /// </summary>
        [Obsolete]
        public int WriteRow(StorageDataReader dataReader)
        {
            throw new InvalidOperationException("This type of writer is meant to write values from a list of cell values only.");
        }

        /// <summary>
        /// Writes a row of data to the output file using the format provided by the implementing class.
        /// </summary>
        /// <param name="row">The row of data to output</param>
        /// <param name="columns">The list of columns to output</param>
        public abstract void WriteRow(IList<DbCellValue> row, IReadOnlyList<DbColumnWrapper> columns);

        /// <summary>
        /// Not implemented, do not use.
        /// </summary>
        [Obsolete]
        public void Seek(long offset)
        {
            throw new InvalidOperationException("SaveAs writers are meant to be written once contiguously.");
        }

        /// <summary>
        /// Flushes the file stream buffer
        /// </summary>
        public void FlushBuffer()
        {
            FileStream.Flush();
        }

        /// <summary>
        /// Attempts to parse the provided <paramref name="encoding"/> and return an encoding that
        /// matches the encoding name or codepage number.
        /// </summary>
        /// <param name="encoding">Encoding name or codepage number to parse.</param>
        /// <param name="fallbackEncoding">
        /// Encoding to return if no encoding of provided name/codepage number exists.
        /// </param>
        /// <returns>
        /// Desired encoding object or the <paramref name="fallbackEncoding"/> if the desired
        /// encoding could not be found.
        /// </returns>
        protected static Encoding ParseEncoding(string encoding, Encoding fallbackEncoding)
        {
            // If the encoding is a number, we try to look up a codepage encoding using the
            // parsed number as a codepage. If it is not a number, attempt to look up an
            // encoding with the provided encoding name. If getting the encoding fails in
            // either case, we will return the fallback encoding.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            try
            {
                return int.TryParse(encoding, out int codePage)
                    ? Encoding.GetEncoding(codePage)
                    : Encoding.GetEncoding(encoding);
            }
            catch
            {
                return fallbackEncoding;
            }
        }

        #region IDisposable Implementation

        private bool disposed;

        /// <summary>
        /// Disposes the instance by flushing and closing the file stream
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                FileStream.Dispose();
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
