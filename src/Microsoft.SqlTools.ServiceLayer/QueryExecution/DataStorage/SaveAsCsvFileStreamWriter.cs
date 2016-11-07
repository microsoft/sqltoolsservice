
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class SaveAsCsvFileStreamWriter : IFileStreamWriter
    {

        private const int DefaultBufferLength = 8192;

        #region Member Variables

        private readonly IFileStreamWrapper fileStream;
        private bool disposed;
        private bool headerWritten;
        private readonly SaveResultsAsCsvRequestParams saveParams;
        private readonly int? columnStartIndex;
        private readonly int? columnCount;

        #endregion

        public SaveAsCsvFileStreamWriter(IFileStreamWrapper fileWrapper, string fileName, SaveResultsAsCsvRequestParams requestParams)
        {
            // Open the requested file for writing
            fileStream = fileWrapper;
            fileStream.Init(fileName, DefaultBufferLength, FileAccess.ReadWrite);

            saveParams = requestParams;
            if (requestParams.IsSaveSelection)
            {
                // ReSharper disable PossibleInvalidOperationException  IsSaveSelection verifies these values exist
                columnStartIndex = saveParams.ColumnStartIndex.Value;
                columnCount = saveParams.ColumnEndIndex.Value - saveParams.ColumnStartIndex.Value;
                // ReSharper restore PossibleInvalidOperationException
            }
        }

        #region IFileStreamWriter Implementation

        [Obsolete]
        public int WriteRow(StorageDataReader dataReader)
        {
            throw new InvalidOperationException("This type of writer is meant to write values from a list of cell values only.");
        }

        public int WriteRow(IList<DbCellValue> row, IList<DbColumnWrapper> columns)
        {
            int bytesWritten = 0;

            // Write out the header if we haven't already and the user chose to have it
            if (saveParams.IncludeHeaders && !headerWritten)
            {
                // Build the string
                var selectedColumns = columns.Skip(columnStartIndex ?? 0).Take(columnCount ?? columns.Count)
                    .Select(c => EncodeCsvField(c.ColumnName) ?? string.Empty);
                string headerLine = string.Join(",", selectedColumns);

                // Encode it and write it out
                byte[] headerBytes = Encoding.Unicode.GetBytes(headerLine);
                bytesWritten += fileStream.WriteData(headerBytes, headerBytes.Length);

                headerWritten = true;
            }

            // Build the string for the row
            var selectedCells = row.Skip(columnStartIndex ?? 0).Take(columnCount ?? columns.Count)
                .Select(c => EncodeCsvField(c.DisplayValue));
            string rowLine = string.Join(",", selectedCells);

            // Encode it and write it out
            byte[] rowBytes = Encoding.Unicode.GetBytes(rowLine);
            bytesWritten += fileStream.WriteData(rowBytes, rowBytes.Length);

            return bytesWritten;
        }

        public void FlushBuffer()
        {
            fileStream.Flush();
        }

        #endregion

        /// Method ported from SSMS
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
        internal static string EncodeCsvField(string field)
        {
            StringBuilder sbField = new StringBuilder(field);

            //Whether this field has special characters which require it to be embedded in quotes
            bool embedInQuotes = false;

            //Check for leading/trailing spaces
            if (sbField.Length > 0 &&
                (sbField[0] == ' ' ||
                sbField[0] == '\t' ||
                sbField[sbField.Length - 1] == ' ' ||
                sbField[sbField.Length - 1] == '\t'))
            {
                embedInQuotes = true;
            }
            else
            {   //List separator being in the field will require quotes
                if (field.Contains(","))
                {
                    embedInQuotes = true;
                }
                else
                {
                    for (int i = 0; i < sbField.Length; ++i)
                    {
                        //Check whether this character is a special character
                        if (sbField[i] == '\r' ||
                            sbField[i] == '\n' ||
                            sbField[i] == '"')
                        { //If even one character requires embedding the whole field will
                            //be embedded in quotes so we can just break out now
                            embedInQuotes = true;
                            break;
                        }
                    }
                }
            }

            //Replace all quotes in the original field with double quotes
            sbField.Replace("\"", "\"\"");
            string ret = sbField.ToString();

            if (embedInQuotes)
            {
                ret = "\"" + ret + "\"";
            }

            return ret;
        }

        #region IDisposable Implementation

        private void Dispose(bool disposing)
        {
            if (disposed || !disposing)
            {
                disposed = true;
                return;
            }

            fileStream.Flush();
            fileStream.Dispose();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}