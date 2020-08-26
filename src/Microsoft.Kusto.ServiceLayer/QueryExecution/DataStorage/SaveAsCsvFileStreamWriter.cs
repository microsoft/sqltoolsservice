// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Writer for writing rows of results to a CSV file
    /// </summary>
    public class SaveAsCsvFileStreamWriter : SaveAsStreamWriter
    {

        #region Member Variables

        private readonly SaveResultsAsCsvRequestParams saveParams;
        private bool headerWritten;

        #endregion

        /// <summary>
        /// Constructor, stores the CSV specific request params locally, chains into the base 
        /// constructor
        /// </summary>
        /// <param name="stream">FileStream to access the CSV file output</param>
        /// <param name="requestParams">CSV save as request parameters</param>
        public SaveAsCsvFileStreamWriter(Stream stream, SaveResultsAsCsvRequestParams requestParams)
            : base(stream, requestParams)
        {
            saveParams = requestParams;
        }

        /// <summary>
        /// Writes a row of data as a CSV row. If this is the first row and the user has requested
        /// it, the headers for the column will be emitted as well.
        /// </summary>
        /// <param name="row">The data of the row to output to the file</param>
        /// <param name="columns">
        /// The entire list of columns for the result set. They will be filtered down as per the
        /// request params.
        /// </param>
        public override void WriteRow(IList<DbCellValue> row, IList<DbColumnWrapper> columns)
        {
            char delimiter = ',';
            if(!string.IsNullOrEmpty(saveParams.Delimiter))
            {
                // first char in string
                delimiter = saveParams.Delimiter[0];
            }

            string lineSeperator = Environment.NewLine;
            if(!string.IsNullOrEmpty(saveParams.LineSeperator))
            {
                lineSeperator = saveParams.LineSeperator;
            }

            char textIdentifier = '"';
            if(!string.IsNullOrEmpty(saveParams.TextIdentifier))
            {
                // first char in string
                textIdentifier = saveParams.TextIdentifier[0];
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            int codepage;
            Encoding encoding;
            try
            {
                if(int.TryParse(saveParams.Encoding, out codepage))
                {
                    encoding = Encoding.GetEncoding(codepage);
                }
                else
                {
                    encoding = Encoding.GetEncoding(saveParams.Encoding);
                }
            }
            catch
            {    
                // Fallback encoding when specified codepage is invalid
                encoding = Encoding.GetEncoding("utf-8");
            }

            // Write out the header if we haven't already and the user chose to have it
            if (saveParams.IncludeHeaders && !headerWritten)
            {
                // Build the string
                var selectedColumns = columns.Skip(ColumnStartIndex ?? 0).Take(ColumnCount ?? columns.Count)
                    .Select(c => EncodeCsvField(c.ColumnName, delimiter, textIdentifier) ?? string.Empty);
                
                string headerLine = string.Join(delimiter, selectedColumns);

                // Encode it and write it out
                byte[] headerBytes = encoding.GetBytes(headerLine + lineSeperator);
                FileStream.Write(headerBytes, 0, headerBytes.Length);

                headerWritten = true;
            }

            // Build the string for the row
            var selectedCells = row.Skip(ColumnStartIndex ?? 0)
                .Take(ColumnCount ?? columns.Count)
                .Select(c => EncodeCsvField(c.DisplayValue, delimiter, textIdentifier));
            string rowLine = string.Join(delimiter, selectedCells);

            // Encode it and write it out
            byte[] rowBytes = encoding.GetBytes(rowLine + lineSeperator);
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
        /// <item><description>The field contains the ListSeparator string</description></item>
        /// <item><description>The field contains the '\n' character</description></item>
        /// <item><description>The field contains the '\r' character</description></item>
        /// <item><description>The field contains the '"' character</description></item>
        /// </list>
        /// </summary>
        /// <param name="field">The field to encode</param>
        /// <returns>The CSV encoded version of the original field</returns>
        internal static string EncodeCsvField(string field, char delimiter, char textIdentifier)
        {
            string strTextIdentifier = textIdentifier.ToString();

            // Special case for nulls
            if (field == null)
            {
                return "NULL";
            }

            // Whether this field has special characters which require it to be embedded in quotes
            bool embedInQuotes = field.IndexOfAny(new[] { delimiter, '\r', '\n', textIdentifier }) >= 0 // Contains special characters
                                 || field.StartsWith(" ") || field.EndsWith(" ")          // Start/Ends with space
                                 || field.StartsWith("\t") || field.EndsWith("\t");       // Starts/Ends with tab

            //Replace all quotes in the original field with double quotes
            string ret = field.Replace(strTextIdentifier, strTextIdentifier + strTextIdentifier);

            if (embedInQuotes)
            {
                ret = strTextIdentifier + $"{ret}" + strTextIdentifier;
            }

            return ret;
        }
    }
}