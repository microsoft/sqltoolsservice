//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

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
        public SaveAsInsertFileStreamWriter(Stream stream, SaveResultsAsInsertRequestParams requestParams, IReadOnlyList<DbColumnWrapper> columns)
            : base(stream, requestParams, columns)
        {
            encoding = ParseEncoding(requestParams.Encoding, Encoding.UTF8);
            tableName = requestParams.TableName ?? "TableName";
            includeHeaders = requestParams.IncludeHeaders;

            // Get the column names for the selected columns
            columnNames = columns.Skip(ColumnStartIndex)
                .Take(ColumnCount)
                .Select(c => c.ColumnName)
                .ToArray();
        }

        /// <summary>
        /// Writes a row of data as an INSERT statement.
        /// </summary>
        /// <param name="row">The data of the row to output to the file</param>
        /// <param name="columns">The columns for the row to output</param>
        public override void WriteRow(IList<DbCellValue> row, IReadOnlyList<DbColumnWrapper> columns)
        {
            // Build the INSERT statement
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("INSERT INTO ");
            stringBuilder.Append(EscapeIdentifier(tableName));
            
            // Add column names if requested
            if (includeHeaders)
            {
                stringBuilder.Append(" (");
                stringBuilder.Append(string.Join(", ", columnNames.Select(EscapeIdentifier)));
                stringBuilder.Append(")");
            }

            stringBuilder.Append(" VALUES (");

            // Add the values
            var selectedCells = row.Skip(ColumnStartIndex)
                .Take(ColumnCount)
                .Select(FormatValue);
            
            stringBuilder.Append(string.Join(", ", selectedCells));
            stringBuilder.Append(");");
            stringBuilder.AppendLine();

            // Write to the stream
            byte[] insertBytes = encoding.GetBytes(stringBuilder.ToString());
            FileStream.Write(insertBytes, 0, insertBytes.Length);
        }

        /// <summary>
        /// Formats a database cell value for insertion into an INSERT statement
        /// </summary>
        /// <param name="cellValue">The cell value to format</param>
        /// <returns>Formatted string representation of the value</returns>
        private string FormatValue(DbCellValue cellValue)
        {
            if (cellValue == null || cellValue.IsNull || cellValue.DisplayValue == null)
            {
                return "NULL";
            }

            string value = cellValue.DisplayValue;

            // For string values, wrap in single quotes and escape single quotes
            if (NeedsQuoting(value))
            {
                return "'" + value.Replace("'", "''") + "'";
            }

            return value;
        }

        /// <summary>
        /// Determines if a value needs to be quoted (i.e., if it's not a number)
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <returns>True if the value needs quoting, false otherwise</returns>
        private bool NeedsQuoting(string value)
        {
            // Simple heuristic: if it's not a number (int, float, decimal), it needs quoting
            return !decimal.TryParse(value, out _) && !bool.TryParse(value, out _);
        }

        /// <summary>
        /// Escapes SQL identifiers by wrapping them in square brackets if they contain special characters
        /// </summary>
        /// <param name="identifier">The identifier to escape</param>
        /// <returns>Escaped identifier</returns>
        private string EscapeIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return identifier;
            }

            // Check if the identifier needs escaping (contains spaces or special characters)
            if (identifier.Contains(" ") || identifier.Contains("-") || !char.IsLetter(identifier[0]))
            {
                return $"[{identifier.Replace("]", "]]")}]";
            }

            return identifier;
        }
    }
}