//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Writer for writing rows of results to an INSERT statements file
    /// </summary>
    public class SaveAsInsertFileStreamWriter : SaveAsStreamWriter
    {

        #region Member Variables

        private readonly Encoding encoding;
        private readonly string tableName;
        private readonly bool includeHeaders;
        private readonly string[] columnNames;
        private readonly List<string> batchedRows;
        private const int BatchSize = 1000;
        private readonly string lineSeparator;

        #endregion

        /// <summary>
        /// Constructor, stores the INSERT specific request params locally, chains into the base
        /// constructor
        /// </summary>
        /// <param name="stream">FileStream to access the INSERT file output</param>
        /// <param name="requestParams">INSERT save as request parameters</param>
        /// <param name="columns">
        /// The entire list of columns for the result set. They will be filtered down as per the
        /// request params.
        /// </param>
        public SaveAsInsertFileStreamWriter(Stream stream, SaveResultsAsInsertRequestParams requestParams, IReadOnlyList<DbColumnWrapper> columns, string overrideLineSeparator = null)
            : base(stream, requestParams, columns)
        {
            encoding = ParseEncoding(requestParams.Encoding, Encoding.UTF8);
            lineSeparator = overrideLineSeparator ?? Environment.NewLine;
            tableName = requestParams.TableName ?? "TableName";
            includeHeaders = requestParams.IncludeHeaders;
            batchedRows = new List<string>();

            // Get the column names for the selected columns
            columnNames = columns.Skip(ColumnStartIndex)
                .Take(ColumnCount)
                .Select(c => c.ColumnName)
                .ToArray();
        }

        /// <summary>
        /// Writes a row of data as part of a batched INSERT statement.
        /// </summary>
        /// <param name="row">The data of the row to output to the file</param>
        /// <param name="columns">The columns for the row to output</param>
        public override void WriteRow(IList<DbCellValue> row, IReadOnlyList<DbColumnWrapper> columns)
        {
            // Format the values for this row
            var selectedCells = row.Skip(ColumnStartIndex)
                .Take(ColumnCount)
                .Select(SqlValueFormatter.FormatValue);

            string rowValues = "(" + string.Join(", ", selectedCells) + ")";
            batchedRows.Add(rowValues);

            // Write batch if we've reached the batch size
            if (batchedRows.Count >= BatchSize)
            {
                WriteBatch();
            }
        }

        /// <summary>
        /// Writes the current batch of rows as a single INSERT statement
        /// </summary>
        private void WriteBatch()
        {
            if (batchedRows.Count == 0)
                return;

            var stringBuilder = new StringBuilder();
            stringBuilder.Append("INSERT INTO ");
            stringBuilder.Append(SqlValueFormatter.EscapeIdentifier(tableName));

            // Add column names if requested
            if (includeHeaders)
            {
                stringBuilder.Append(" (");
                stringBuilder.Append(string.Join(", ", columnNames.Select(SqlValueFormatter.EscapeIdentifier)));
                stringBuilder.Append(")");
            }

            stringBuilder.Append(lineSeparator);
            stringBuilder.Append("VALUES");
            stringBuilder.Append(lineSeparator);
            
            // Add all batched rows with proper indentation
            for (int i = 0; i < batchedRows.Count; i++)
            {
                stringBuilder.Append("    ");  // 4-space indentation
                stringBuilder.Append(batchedRows[i]);
                if (i < batchedRows.Count - 1)
                {
                    stringBuilder.Append(",");
                    stringBuilder.Append(lineSeparator);
                }
                else
                {
                    stringBuilder.Append(";");
                    stringBuilder.Append(lineSeparator);
                }
            }
            
            stringBuilder.Append(lineSeparator);

            // Write to the stream
            byte[] insertBytes = encoding.GetBytes(stringBuilder.ToString());
            FileStream.Write(insertBytes, 0, insertBytes.Length);

            // Clear the batch
            batchedRows.Clear();
        }

        /// <summary>
        /// Override dispose to ensure any remaining batched rows are written
        /// </summary>
        /// <param name="disposing">Whether the object is being disposed</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Write any remaining rows before disposing
                WriteBatch();
            }
            base.Dispose(disposing);
        }
    }
}
