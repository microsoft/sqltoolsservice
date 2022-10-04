//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Writer for writing rows of results to a CSV file
    /// </summary>
    public class SaveAsCsvFileStreamWriter : SaveAsStreamWriter
    {

        #region Member Variables

        private readonly char delimiter;
        private readonly Encoding encoding;
        private readonly string lineSeparator;
        private readonly char textIdentifier;
        private readonly string textIdentifierString;

        #endregion

        /// <summary>
        /// Constructor, stores the CSV specific request params locally, chains into the base
        /// constructor
        /// </summary>
        /// <param name="stream">FileStream to access the CSV file output</param>
        /// <param name="requestParams">CSV save as request parameters</param>
        /// <param name="columns">
        /// The entire list of columns for the result set. They will be filtered down as per the
        /// request params.
        /// </param>
        public SaveAsCsvFileStreamWriter(Stream stream, SaveResultsAsCsvRequestParams requestParams, IReadOnlyList<DbColumnWrapper> columns)
            : base(stream, requestParams, columns)
        {
            // Parse the config
            delimiter = ',';
            if (!string.IsNullOrEmpty(requestParams.Delimiter))
            {
                delimiter = requestParams.Delimiter[0];
            }

            lineSeparator = Environment.NewLine;
            if (!string.IsNullOrEmpty(requestParams.LineSeperator))
            {
                lineSeparator = requestParams.LineSeperator;
            }

            textIdentifier = '"';
            if (!string.IsNullOrEmpty(requestParams.TextIdentifier))
            {
                textIdentifier = requestParams.TextIdentifier[0];
            }
            textIdentifierString = textIdentifier.ToString();

            encoding = ParseEncoding(requestParams.Encoding, Encoding.UTF8);

            // Output the header if the user requested it
            if (requestParams.IncludeHeaders)
            {
                // Build the string
                var selectedColumns = columns.Skip(ColumnStartIndex)
                    .Take(ColumnCount)
                    .Select(c => EncodeCsvField(c.ColumnName) ?? string.Empty);

                string headerLine = string.Join(delimiter, selectedColumns);

                // Encode it and write it out
                byte[] headerBytes = encoding.GetBytes(headerLine + lineSeparator);
                FileStream.Write(headerBytes, 0, headerBytes.Length);
            }
        }

        /// <summary>
        /// Writes a row of data as a CSV row. If this is the first row and the user has requested
        /// it, the headers for the column will be emitted as well.
        /// </summary>
        /// <param name="row">The data of the row to output to the file</param>
        /// <param name="columns">The columns for the row to output</param>
        public override void WriteRow(IList<DbCellValue> row, IReadOnlyList<DbColumnWrapper> columns)
        {
            // Build the string for the row
            var selectedCells = row.Skip(ColumnStartIndex)
                .Take(ColumnCount)
                .Select(c => EncodeCsvField(c.DisplayValue));
            string rowLine = string.Join(delimiter, selectedCells);

            // Encode it and write it out
            byte[] rowBytes = encoding.GetBytes(rowLine + lineSeparator);
            FileStream.Write(rowBytes, 0, rowBytes.Length);
        }

        /// <summary>
        /// Encodes a single field for inserting into a CSV record. The following rules are applied:
        /// <list type="bullet">
        /// <item><description>All double quotes (") are replaced with a pair of consecutive double quotes</description></item>
        /// </list>
        /// The entire field is also surrounded by a pair of double quotes if any of the following conditions are met:
        /// <list type="bullet">
        /// <item><description>The field begins or ends with a space</description></item>
        /// <item><description>The field begins or ends with a tab</description></item>
        /// <item><description>The field contains the delimiter string</description></item>
        /// <item><description>The field contains the '\n' character</description></item>
        /// <item><description>The field contains the '\r' character</description></item>
        /// <item><description>The field contains the '"' character</description></item>
        /// </list>
        /// </summary>
        /// <param name="field">The field to encode</param>
        /// <returns>The CSV encoded version of the original field</returns>
        internal string EncodeCsvField(string field)
        {
            // Special case for nulls
            if (field == null)
            {
                return "NULL";
            }

            // Replace all quotes in the original field with double quotes
            string ret = field.Replace(textIdentifierString, textIdentifierString + textIdentifierString);

            // Whether this field has special characters which require it to be embedded in quotes
            bool embedInQuotes = field.IndexOfAny(new[] { delimiter, '\r', '\n', textIdentifier }) >= 0 // Contains special characters
                                 || field.StartsWith(" ") || field.EndsWith(" ")          // Start/Ends with space
                                 || field.StartsWith("\t") || field.EndsWith("\t");       // Starts/Ends with tab
            if (embedInQuotes)
            {
                ret = $"{textIdentifier}{ret}{textIdentifier}";
            }

            return ret;
        }
    }
}