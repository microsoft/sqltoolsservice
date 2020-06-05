// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Writer for writing rows of results to a text file
    /// </summary>
    public class ResultsToTextFormatter
    {

        #region Member Variables

        private readonly ResultsToTextRequestParams resultsToTextRequestParams;
        private bool headerWritten;

        //table that maps most of the types that we're getting back from the server
        //to the maximum length of a string that represents an object of such type
        //We have it for all simple types. All other types will be handled as special
        //case - right now they are Boolean, DateTime, String and Byte[]
        private Hashtable typeToCharNumTable;
        private Hashtable numericsTypes;
        private StringCollection columnsFormatCollection = new StringCollection();

        int[] columnWidths;

        private bool printColumnHeaders = true;
        private bool rightAlignNumerics = false;


        #endregion

        #region Constants

        protected static string stringType = "System.String".ToLowerInvariant();
        protected static string sqlStringType = "System.Data.SqlTypes.SqlString".ToLowerInvariant();

        protected static string objectType = "System.Object".ToLowerInvariant();
        protected static string boolType = "System.Boolean".ToLowerInvariant();
        protected static string sqlBoolType = "System.Data.SqlTypes.SqlBoolean".ToLowerInvariant();
        protected static string dateTimeType = "System.DateTime".ToLowerInvariant();
        protected static string sqlDateTimeType = "System.Data.SqlTypes.SqlDateTime".ToLowerInvariant();
        protected static string byteArrayType = "System.Byte[]".ToLowerInvariant();
        protected static string sqlByteArrayType = "System.Data.SqlTypes.SqlByte[]".ToLowerInvariant();
        protected static string sqlBinaryType = "System.Data.SqlTypes.SqlBinary".ToLowerInvariant();
        protected static string guidType = "System.Guid".ToLowerInvariant();
        protected static string sqlGuidType = "System.Data.SqlTypes.SqlGuid".ToLowerInvariant();

        protected static string xmlServerSideTypeName = "xml";
        protected static string dateServerSideTypeName = "date";
        protected static string timeServerSideTypeName = "time";
        protected static string datetime2ServerSideTypeName = "datetime2";
        protected static string datetimeoffsetServerSideTypeName = "datetimeoffset";
        internal const int maxCharsPerColumn = 2097152;
        private const int InitialRowBuilderCapacity = 1024;

        #endregion
        
        public ResultsToTextFormatter(ResultsToTextRequestParams requestParams)
        {
            resultsToTextRequestParams = requestParams;
            typeToCharNumTable = new Hashtable();

            //populate the hashtable
            //NOTE: use MinValue for integer types to account for possible '-' sign
            typeToCharNumTable.Add("System.Int16".ToLowerInvariant(), Int16.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture).Length);
            typeToCharNumTable.Add("System.Data.SqlTypes.SqlInt16".ToLowerInvariant(), SqlInt16.MinValue.ToString().Length);

            typeToCharNumTable.Add("System.Int32".ToLowerInvariant(), Int32.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture).Length);
            typeToCharNumTable.Add("System.Data.SqlTypes.SqlInt32".ToLowerInvariant(), SqlInt32.MinValue.ToString().Length);

            typeToCharNumTable.Add("System.Int64".ToLowerInvariant(), Int64.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture).Length);
            typeToCharNumTable.Add("System.Data.SqlTypes.SqlInt64".ToLowerInvariant(), SqlInt64.MinValue.ToString().Length);

            typeToCharNumTable.Add("System.Char".ToLowerInvariant(), Char.MinValue.ToString().Length);

            typeToCharNumTable.Add("System.Byte".ToLowerInvariant(), Byte.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture).Length);
            typeToCharNumTable.Add("System.Data.SqlTypes.SqlByte".ToLowerInvariant(), SqlByte.MinValue.ToString().Length);

            typeToCharNumTable.Add("System.Double".ToLowerInvariant(), Double.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture).Length);
            typeToCharNumTable.Add("System.Data.SqlTypes.SqlDouble".ToLowerInvariant(), SqlDouble.MinValue.ToString().Length);

            typeToCharNumTable.Add("System.Single".ToLowerInvariant(), Single.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture).Length);
            typeToCharNumTable.Add("System.Data.SqlTypes.SqlSingle".ToLowerInvariant(), SqlSingle.MinValue.ToString().Length);

            typeToCharNumTable.Add("System.Decimal".ToLowerInvariant(), Decimal.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture).Length);
            typeToCharNumTable.Add("System.Data.SqlTypes.SqlDecimal".ToLowerInvariant(), SqlDecimal.MinValue.ToString().Length);

            typeToCharNumTable.Add("System.Data.SqlTypes.SqlMoney".ToLowerInvariant(), SqlMoney.MinValue.ToString().Length);

            //initialize hashtable of types that correspond to numerics
            numericsTypes = new Hashtable();

            numericsTypes.Add("System.Int16".ToLowerInvariant(), 0);
            numericsTypes.Add("System.Data.SqlTypes.SqlInt16".ToLowerInvariant(), 0);

            numericsTypes.Add("System.Int32".ToLowerInvariant(), 0);
            numericsTypes.Add("System.Data.SqlTypes.SqlInt32".ToLowerInvariant(), 0);

            numericsTypes.Add("System.Int64".ToLowerInvariant(), 0);
            numericsTypes.Add("System.Data.SqlTypes.SqlInt64".ToLowerInvariant(), 0);

            numericsTypes.Add("System.Byte".ToLowerInvariant(), 0);
            numericsTypes.Add("System.Data.SqlTypes.SqlByte".ToLowerInvariant(), 0);

            numericsTypes.Add("System.Double".ToLowerInvariant(), 0);
            numericsTypes.Add("System.Data.SqlTypes.SqlDouble".ToLowerInvariant(), 0);

            numericsTypes.Add("System.Single".ToLowerInvariant(), 0);
            numericsTypes.Add("System.Data.SqlTypes.SqlSingle".ToLowerInvariant(), 0);

            numericsTypes.Add("System.Decimal".ToLowerInvariant(), 0);
            numericsTypes.Add("System.Data.SqlTypes.SqlDecimal".ToLowerInvariant(), 0);

            numericsTypes.Add("System.Data.SqlTypes.SqlMoney".ToLowerInvariant(), 0);
        }

        /// <summary>
        /// Helper method to format and encode a row if the result is column aligned
        /// </summary>
        private string FormatAndEncodeRow(IList<DbCellValue> row, IList<DbColumnWrapper> columns, 
            char delimiter, char textIdentifier, bool isHeader = false)
        {
            StringBuilder headerStringBuilder = new StringBuilder();
            int columnCount = columns.Count();
            for (int i = 0; i < columnCount; i++)
            {
                string columnName = isHeader ? columns[i].ColumnName : row[i].DisplayValue;
                headerStringBuilder.AppendFormat(columnsFormatCollection[i], 
                    EncodeTextField(columnName, delimiter, textIdentifier) ?? string.Empty);
            }
            headerStringBuilder.AppendLine();
            string formattedString = headerStringBuilder.ToString();
            if (isHeader)
            {
                //we need to add dashes;
                StringBuilder separatorBuilder = new StringBuilder();
                for (int j = 0; j < columnCount; j++)
                {
                    for (int k = 0; k < columnWidths[j]; k++)
                    {
                        separatorBuilder.Append('-');
                    }

                    if (j != (columnCount - 1))
                    {
                        //append space for all but the very last column header
                        separatorBuilder.Append(' ');
                    }
                    formattedString += separatorBuilder.ToString();
                }

            }
            return formattedString;
        }

        /// <summary>
        /// Helper method to calculate the column widths when columns are aligned
        /// </summary>
        //create array with column widths and format strings that will be used to
        //output cell values
        private void CreateColumnWidthsAndFormatStrings(IList<DbCellValue> row, IList<DbColumnWrapper> columns)
        {
            Debug.Assert(columns != null);
            Debug.Assert(columnsFormatCollection != null);

            columnWidths = new int[columns.Count];

            //we want to decide how many characters every column will have
            //when output as string.
            //After that, we want to create format string that will be
            //used to output values for cells, padding them when needed
            string colProviderType = null;
            string colServerType = null;
            int nMaxColumnLength = 0;
            int nColumnNameLength = 0;
            object hashEntry = null;
            //this is the format of DateTime2 in Sql Server 2008. The string defined in Microsoft.SqlServer.DataStorage
            //is of the form yyyy-MM-dd HH:mm:ss{0} which limits the length to 22 and so misses the precision of 7
            //fractional digits.Instead of modifying the shared dll, a local variable is defined and used when
            //datetime2 datatype is encountered.
            string DateTime2FormatString = "yyyy-MM-dd HH:mm:ss.fffffff";
            for (int i = 0; i < columns.Count; i++)
            {
                colProviderType = columns[i].DataType.FullName.ToLowerInvariant();
                // colServerType = m_curResultSet.GetServerDataTypeName(i).ToLowerInvariant();

                //first, see if it is one of standard type that we precoded into hashtable
                hashEntry = typeToCharNumTable[colProviderType];
                if (hashEntry != null)
                {
                    Logger.Write(TraceEventType.Information,
                        string.Format("ColumnAlign: found hash entry of col {0}, type = {1}", i, colProviderType));
                    nMaxColumnLength = (int)hashEntry;
                }
                else
                {
                    //no, it is one of special types
                    if (colProviderType == stringType || colProviderType == sqlStringType || colProviderType == objectType)
                    {
                        Logger.Write(TraceEventType.Information, string.Format(CultureInfo.InvariantCulture,
                            "ColumnAlign: col {0} is String", i));
 
                        //use value from meta data
                        nMaxColumnLength = (int)columns[i].ColumnSize;
                    }
                    else if (colProviderType == boolType || colProviderType == sqlBoolType)
                    {
                        Logger.Write(TraceEventType.Information,
                            string.Format("ColumnAlign: col {0} is Boolean", i));
                        //boolean has True/False -> at most 5 chars (in "False")
                        nMaxColumnLength = 5;
                    }
                    else if (colProviderType == dateTimeType || colProviderType == sqlDateTimeType)
                    {
                        if (colServerType == dateServerSideTypeName)
                        {
                            Logger.Write(TraceEventType.Information,
                                string.Format("ColumnAlign: col {0} is Date", i));

                            // get column width from format string used by the file stream buffer
                            nMaxColumnLength = ServiceBufferFileStreamReader.DateFormatString.Length;
                        }
                        else if (colServerType == datetime2ServerSideTypeName)
                        {
                            Logger.Write(TraceEventType.Information,
                                string.Format("ColumnAlign: col {0} is DateTime2", i));

                            // get column width from format string used by the file stream buffer
                            nMaxColumnLength = DateTime2FormatString.Length;
                        }
                        else
                        {
                            Logger.Write(TraceEventType.Information,
                                string.Format("ColumnAlign: col {0} is DateTime", i));

                            // get column width from format string used by the file stream buffer
                            nMaxColumnLength = ServiceBufferFileStreamReader.DateTimeFormatString.Length;
                        }
                    }
                    else if (colServerType == timeServerSideTypeName)
                    {
                        Logger.Write(TraceEventType.Information,
                            string.Format("ColumnAlign: col {0} is Time", i));

                        nMaxColumnLength = 16;
                    }
                    else if (colServerType == datetimeoffsetServerSideTypeName)
                    {
                        Logger.Write(TraceEventType.Information,
                            string.Format("ColumnAlign: col {0} is DateTimeOffset", i));

                        nMaxColumnLength = 34;
                    }
                    else if (colProviderType == byteArrayType || colProviderType == sqlByteArrayType || colProviderType == sqlBinaryType)
                    {
                        Logger.Write(TraceEventType.Information,
                            string.Format("ColumnAlign: col {0} is Byte[]", i));
                        //use value from meta data
                        nMaxColumnLength = (int)columns[i].ColumnSize;

                        if (nMaxColumnLength == System.Int32.MaxValue)//it sets it like that sometimes
                        {
                            nMaxColumnLength = maxCharsPerColumn;
                        }
                        else
                        {
                            //for every byte we'll use 2 hex digits + storage will add "0x" prefix
                            nMaxColumnLength = nMaxColumnLength * 2 + 2;
                        }
                    }
                    else if (colProviderType == guidType || colProviderType == sqlGuidType)
                    {
                        Logger.Write(TraceEventType.Information,
                            string.Format("ColumnAlign: col {0} is Guid", i));
                        nMaxColumnLength = 36; //Guid.ToString returns it in the following format: 6F9619FF-8B86-D011-B42D-00C04FC964FF
                    }
                    else if (0 == string.Compare(xmlServerSideTypeName, colServerType, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Write(TraceEventType.Information,
                            string.Format("ColumnAlign: col {0} is XML", i));
                        //use value from meta data
                        nMaxColumnLength = (int)columns[i].ColumnSize;
                    }
                    else
                    {
                        Logger.Write(TraceEventType.Error,
                            string.Format("ColumnAlign: col {0} is unexpected type {1}", i, colProviderType));
                        //UNEXPECTED!!! Should never happen
                        Debug.Assert(false, String.Format(System.Globalization.CultureInfo.InvariantCulture, "could not determine type of column # {0}, type = {1}", i, colProviderType));
                        nMaxColumnLength = maxCharsPerColumn;
                    }
                }

                Logger.Write(TraceEventType.Information,
                    string.Format("ColumnAlign: initial length for col {0} is {1}", i, nMaxColumnLength));

                //make sure that we have enough space for column name
                nColumnNameLength = columns[i].ColumnName.Length;
                if (nMaxColumnLength < nColumnNameLength && printColumnHeaders)
                {
                    nMaxColumnLength = nColumnNameLength;
                }

                //make sure that it is less then maximum allowed by user
                if (nMaxColumnLength > maxCharsPerColumn)
                {
                    Logger.Write(TraceEventType.Information,
                        string.Format("ColumnAlign: adjusting col {0} length for max chars", i));
                    nMaxColumnLength = maxCharsPerColumn;
                    if (colProviderType == byteArrayType || colProviderType == sqlByteArrayType || colProviderType == sqlBinaryType)
                    {
                        //for byte array, add 2 characters for "0x" prefix
                        nMaxColumnLength += 2;
                    }
                }


                //finally, make sure that we can write "NULL"
                if (nMaxColumnLength < 4)
                {
                    Logger.Write(TraceEventType.Information, 
                        string.Format("ColumnAlign: adjusting col {0} length for NULL", i));
                    //length of NULL
                    nMaxColumnLength = 4;
                }

                Logger.Write(TraceEventType.Information,
                    string.Format("ColumnAlign: final length of col {0} is {1}", i, nMaxColumnLength));

                //OK, we've figured out maximum width for this column. Store it along with format string
                //that we can use to output cells in text mode
                columnWidths[i] = nMaxColumnLength;
                if (!rightAlignNumerics ||
                    (rightAlignNumerics && !numericsTypes.Contains(colProviderType)))
                {
                    //no need to right align

                    //we don't want to add extra spaces for the very last column
                    if (i != (columns.Count - 1))
                    {
                        columnsFormatCollection.Add("{0,-" + nMaxColumnLength + "}");
                    }
                    else
                    {
                        columnsFormatCollection.Add("{0}");
                    }
                }
                else
                {
                    //need to right align
                    columnsFormatCollection.Add("{0," + nMaxColumnLength + "}");
                }
                Logger.Write(TraceEventType.Information,
                    string.Format("ColumnAlign: format string of col {0} is \"{1}\", rightAlignNumerics = {2}",
                    i, columnsFormatCollection[i], rightAlignNumerics));
            }
        }

        /// <summary>
        /// Writes a row of data as a text row. If this is the first row and the user has requested
        /// it, the headers for the column will be emitted as well.
        /// </summary>
        /// <param name="row">The data of the row to output to the file</param>
        /// <param name="columns">
        /// The entire list of columns for the result set. They will be filtered down as per the
        /// request params.
        /// </param>
        public string WriteRow(IList<DbCellValue> row, IList<DbColumnWrapper> columns)
        {
            char delimiter = ' ';
            resultsToTextRequestParams.IsColumnAligned = true;
            if(!string.IsNullOrEmpty(resultsToTextRequestParams.Delimiter))
            {
                // first char in string
                delimiter = resultsToTextRequestParams.Delimiter[0];
            }

            string lineSeperator = Environment.NewLine;
            if(!string.IsNullOrEmpty(resultsToTextRequestParams.LineSeperator))
            {
                lineSeperator = resultsToTextRequestParams.LineSeperator;
            }

            char textIdentifier = '"';
            if(!string.IsNullOrEmpty(resultsToTextRequestParams.TextIdentifier))
            {
                // first char in string
                textIdentifier = resultsToTextRequestParams.TextIdentifier[0];
            }

            // Write out the header if we haven't already and the user chose to have it
            if (resultsToTextRequestParams.IncludeHeaders && !headerWritten)
            {
                string headerLine;

                // Build the string
                if (resultsToTextRequestParams.IsColumnAligned) 
                {
                    CreateColumnWidthsAndFormatStrings(row, columns);
                    headerLine = FormatAndEncodeRow(row, columns, delimiter, textIdentifier, true);
                } 
                else 
                {
                    var selectedColumns = columns.Take(columns.Count).Select(c => EncodeTextField(c.ColumnName, 
                        delimiter, textIdentifier) ?? string.Empty);
                    headerLine = string.Join(delimiter, selectedColumns);
                }

                headerWritten = true;
            }

            // Build the string for the row
            string rowString;
            if (resultsToTextRequestParams.IsColumnAligned) 
            {
                rowString = FormatAndEncodeRow(row, columns, delimiter, textIdentifier, false);
            }
            else
            {
                var selectedCells = row.Take(columns.Count)
                    .Select(c => EncodeTextField(c.DisplayValue, delimiter, textIdentifier));
                rowString = string.Join(delimiter, selectedCells);
            }

            rowString += lineSeperator;
            return rowString;
        }

        /// <summary>
        /// Encodes a single field for inserting into a text record. The following rules are applied:
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
        /// <returns>The text encoded version of the original field</returns>
        internal static string EncodeTextField(string field, char delimiter, char textIdentifier)
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

        public ResultsToTextRequestParams RequestParams 
        { 
            get { return resultsToTextRequestParams; } 
        }
    }
}