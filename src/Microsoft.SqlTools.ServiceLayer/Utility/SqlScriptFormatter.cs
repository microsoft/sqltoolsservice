//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    /// <summary>
    /// Provides utility for converting arbitrary objects into strings that are ready to be
    /// inserted into SQL strings
    /// </summary>
    public class SqlScriptFormatter
    {
        #region Constants

        public const string NullString = "NULL";

        private static readonly Dictionary<string, Func<object, DbColumn, string>> FormatFunctions =
           new Dictionary<string, Func<object, DbColumn, string>>
           {                                                                                       // CLR Type --------
                {"bigint", (val, col) => SimpleFormatter(val)},                                     // long
                {"bit", (val, col) => FormatBool(val)},                                             // bool
                {"int", (val, col) => SimpleFormatter(val)},                                        // int
                {"smallint", (val, col) => SimpleFormatter(val)},                                   // short
                {"tinyint", (val, col) => SimpleFormatter(val)},                                    // byte
                {"money", (val, col) => FormatMoney(val, "MONEY")},                                 // Decimal
                {"smallmoney", (val, col) => FormatMoney(val, "SMALLMONEY")},                       // Decimal
                {"decimal", (val, col) => FormatPreciseNumeric(val, col, "DECIMAL")},               // Decimal
                {"numeric", (val, col) => FormatPreciseNumeric(val, col, "NUMERIC")},               // Decimal
                {"real", (val, col) => FormatFloat(val)},                                           // float
                {"float", (val, col) => FormatDouble(val)},                                         // double
                {"smalldatetime", (val, col) => FormatDateTime(val, "yyyy-MM-dd HH:mm:ss")},        // DateTime
                {"datetime", (val, col) => FormatDateTime(val, "yyyy-MM-dd HH:mm:ss.FFF") },        // DateTime
                {"datetime2", (val, col) => FormatDateTime(val, "yyyy-MM-dd HH:mm:ss.FFFFFFF")},    // DateTime
                {"date", (val, col) => FormatDateTime(val, "yyyy-MM-dd")},                          // DateTime
                {"datetimeoffset", (val, col) => FormatDateTimeOffset(val)},                        // DateTimeOffset
                {"time", (val, col) => FormatTimeSpan(val)},                                        // TimeSpan
                {"char", (val, col) => SimpleStringFormatter(val)},                                 // string
                {"nchar", (val, col) => SimpleStringFormatter(val)},                                // string
                {"varchar", (val, col) => SimpleStringFormatter(val)},                              // string
                {"nvarchar", (val, col) => SimpleStringFormatter(val)},                             // string
                {"text", (val, col) => SimpleStringFormatter(val)},                                 // string                     
                {"ntext", (val, col) => SimpleStringFormatter(val)},                                // string
                {"xml", (val, col) => SimpleStringFormatter(val)},                                  // string
                {"binary", (val, col) => FormatBinary(val)},                                        // byte[]
                {"varbinary", (val, col) => FormatBinary(val)},                                     // byte[]
                {"image", (val, col) => FormatBinary(val)},                                         // byte[]
                {"uniqueidentifier", (val, col) => SimpleStringFormatter(val)},                     // Guid
                // Unsupported types:
                // *.sys.hierarchyid - cannot cast byte string to hierarchyid
                // geography         - cannot cast byte string to geography
                // geometry          - cannot cast byte string to geometry
                // timestamp         - cannot insert/update timestamp columns
                // sql_variant       - casting logic isn't good enough
                // sysname           - it doesn't appear possible to insert a sysname column
           };
        
        private static readonly Dictionary<string, Func<DbColumn, string>> ColumnFormatFunctions = 
            new Dictionary<string, Func<DbColumn, string>>
            {
                {"decimal", PreciseScaleColumnFormatter},
                {"numeric", PreciseScaleColumnFormatter},
                {"datetime2", ScaledColumnFormatter},
                {"datetimeoffset", ScaledColumnFormatter},
                {"time", ScaledColumnFormatter},
                {"char", SizedColumnFormatter},
                {"nchar", SizedColumnFormatter},
                {"varchar", SizedColumnFormatter},
                {"nvarchar", SizedColumnFormatter},
                {"binary", SizedColumnFormatter},
                {"varbinary", SizedColumnFormatter},
                {"timestamp", c => "VARBINARY(8)"}        // Timestamps can't be inserted, so use their semantically equivalent type
            };

        private static readonly Type[] NumericTypes = 
        {
            typeof(byte),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(decimal),
            typeof(float),
            typeof(double)
        };

        private static Regex StringRegex = new Regex("^N?'(.*)'$", RegexOptions.Compiled);

        #endregion

        public static string FormatColumnType(DbColumn column)
        {
            Validate.IsNotNull(nameof(column), column);
            
            // Try to get a formatter function for the column, if it doesn't exist, try with the simple formatter
            Func<DbColumn, string> formatFunction;
            if (!ColumnFormatFunctions.TryGetValue(column.DataTypeName.ToLowerInvariant(), out formatFunction))
            {
                // NOTE: Using the simple formatter isn't guaranteed to work for all cases, 
                // especially if the datatype isn't a valid type
                formatFunction = SimpleColumnFormatter;
            }

            return formatFunction(column);
        }
        
        /// <summary>
        /// Converts an object into a string for SQL script
        /// </summary>
        /// <param name="value">The object to convert</param>
        /// <param name="column">The column metadata for the cell to insert</param>
        /// <returns>String version of the cell value for use in SQL scripts</returns>
        public static string FormatValue(object value, DbColumn column)
        {
            Validate.IsNotNull(nameof(column), column);

            // Handle nulls firstly
            if (value == null)
            {
                return NullString;
            }

            // Determine how to format based on the column type
            string dataType = column.DataTypeName.ToLowerInvariant();
            if (!FormatFunctions.ContainsKey(dataType))
            {
                // Attempt to handle UDTs

                // @TODO: to constants file
                throw new ArgumentOutOfRangeException(nameof(column.DataTypeName), "A converter for {column type} is not available");
            }
            return FormatFunctions[dataType](value, column);
        }

        /// <summary>
        /// Converts a cell value into a string for SQL script
        /// </summary>
        /// <param name="value">The cell to convert</param>
        /// <param name="column">The column metadata for the cell to insert</param>
        /// <returns>String version of the cell value for use in SQL scripts</returns>
        public static string FormatValue(DbCellValue value, DbColumn column)
        {
            Validate.IsNotNull(nameof(value), value);

            return FormatValue(value.RawObject, column);
        }

        /// <summary>
        /// Escapes an identifier such as a table name or column name by wrapping it in square brackets
        /// </summary>
        /// <param name="identifier">The identifier to format</param>
        /// <returns>Identifier formatted for use in a SQL script</returns>
        public static string FormatIdentifier(string identifier)
        {
            return $"[{EscapeString(identifier, ']')}]";
        }

        /// <summary>
        /// Escapes a multi-part identifier such as a table name or column name with multiple
        /// parts split by '.'
        /// </summary>
        /// <param name="identifier">The identifier to escape</param>
        /// <returns>The escaped identifier</returns>
        public static string FormatMultipartIdentifier(string identifier)
        {
            // If the object is a multi-part identifier (eg, dbo.tablename) split it, and escape as necessary
            return FormatMultipartIdentifier(identifier.Split('.'));
        }

        /// <summary>
        /// Escapes a multipart identifier such as a table name, given an array of the parts of the
        /// multipart identifier.
        /// </summary>
        /// <param name="identifiers">The parts of the identifier to escape</param>
        /// <returns>An escaped version of the multipart identifier</returns>
        public static string FormatMultipartIdentifier(string[] identifiers)
        {
            IEnumerable<string> escapedParts = identifiers.Select(FormatIdentifier);
            return string.Join(".", escapedParts);
        }

        /// <summary>
        /// Converts a value from a script into a plain version by unwrapping literal wrappers
        /// and unescaping characters.
        /// </summary>
        /// <param name="literal">The value to unwrap</param>
        /// <returns>The unwrapped/unescaped literal</returns>
        public static string UnwrapLiteral(string literal)
        {
            // Always remove parens
            literal = literal.Trim('(', ')');

            // Attempt to unwrap inverted commas around a string
            Match match = StringRegex.Match(literal);
            if (match.Success)
            {
                // Like: N'stuff' or 'stuff'
                return UnEscapeString(match.Groups[1].Value, '\'');
            }
            return literal;
        }

        public static string[] DecodeMultipartIdenfitier(string multipartIdentifier)
        {
            StringBuilder sb = new StringBuilder();
            List<string> namedParts = new List<string>();
            bool insideBrackets = false;
            bool bracketsClosed = false;
            for (int i = 0; i < multipartIdentifier.Length; i++)
            {
                char iChar = multipartIdentifier[i];
                if (insideBrackets)
                {
                    if (iChar == ']')
                    {
                        if (HasNextCharacter(multipartIdentifier, ']', i))
                        {
                            // This is an escaped ]
                            sb.Append(iChar);
                            i++;
                        }
                        else
                        {
                            // This bracket closes the bracket we were in
                            insideBrackets = false;
                            bracketsClosed = true;
                        }
                    }
                    else
                    {
                        // This is a standard character
                        sb.Append(iChar);
                    }
                }
                else
                {
                    switch (iChar)
                    {
                        case '[':
                            if (bracketsClosed)
                            {
                                throw new FormatException();
                            }

                            // We're opening a set of brackets
                            insideBrackets = true;
                            bracketsClosed = false;
                            break;
                        case '.':
                            if (sb.Length == 0)
                            {
                                throw new FormatException();
                            }

                            // We're splitting the identifier into a new part
                            namedParts.Add(sb.ToString());
                            sb = new StringBuilder();
                            bracketsClosed = false;
                            break;
                        default:
                            if (bracketsClosed)
                            {
                                throw new FormatException();
                            }

                            // This is a standard character
                            sb.Append(iChar);
                            break;
                    }
                }
            }
            if (sb.Length == 0)
            {
                throw new FormatException();
            }
            namedParts.Add(sb.ToString());
            return namedParts.ToArray();
        }

        #region Private Helpers

        private static string SimpleColumnFormatter(DbColumn column)
        {
            return column.ColumnName.ToUpperInvariant();
        }

        private static string ScaledColumnFormatter(DbColumn column)
        {
            Validate.IsNotNull(nameof(column.NumericScale), column.NumericScale);
            return string.Format("{0}({1})", column.ColumnName.ToUpperInvariant(), column.NumericScale);
        }
        
        private static string SizedColumnFormatter(DbColumn column)
        {
            Validate.IsNotNull(nameof(column.ColumnSize), column.ColumnSize);
            string size = column.ColumnSize == int.MaxValue
                ? "max"
                : column.ColumnSize.ToString();
            return string.Format("{0}({1})", column.ColumnName.ToUpperInvariant(), size);
        }
        
        private static string PreciseScaleColumnFormatter(DbColumn column)
        {
            Validate.IsNotNull(nameof(column.NumericPrecision), column.NumericPrecision);
            Validate.IsNotNull(nameof(column.NumericScale), column.NumericScale);
            return string.Format("{0}({1},{2})", column.ColumnName.ToUpperInvariant(), column.NumericPrecision, column.NumericPrecision);
        }
        
        private static string SimpleFormatter(object value)
        {
            return value.ToString();
        }

        private static string SimpleStringFormatter(object value)
        {
            return EscapeQuotedSqlString(value.ToString());
        }

        private static string FormatMoney(object value, string type)
        {
            // we have to manually format the string by ToStringing the value first, and then converting 
            // the potential (European formatted) comma to a period.
            string numericString = ((decimal)value).ToString(CultureInfo.InvariantCulture);
            return $"CAST({numericString} AS {type})";
        }

        private static string FormatFloat(object value)
        {
            // The "R" formatting means "Round Trip", which preserves fidelity
            return ((float)value).ToString("R");
        }

        private static string FormatDouble(object value)
        {
            // The "R" formatting means "Round Trip", which preserves fidelity
            return ((double)value).ToString("R");
        }

        private static string FormatBool(object value)
        {
            // Attempt to cast to bool
            bool boolValue = (bool)value;
            return boolValue ? "1" : "0";
        }

        private static string FormatPreciseNumeric(object value, DbColumn column, string type)
        {
            // Make sure we have numeric precision and numeric scale
            if (!column.NumericPrecision.HasValue || !column.NumericScale.HasValue)
            {
                throw new InvalidOperationException(SR.SqlScriptFormatterDecimalMissingPrecision);
            }

            // Convert the value to a decimal, then convert that to a string
            string numericString = ((decimal)value).ToString(CultureInfo.InvariantCulture);
            return string.Format(CultureInfo.InvariantCulture, "CAST({0} AS {1}({2}, {3}))",
                numericString, type, column.NumericPrecision.Value, column.NumericScale.Value);
        }

        private static string FormatTimeSpan(object value)
        {
            // "c" provides "HH:mm:ss.FFFFFFF", and time column accepts up to 7 precision
            string timeSpanString = ((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture);
            return EscapeQuotedSqlString(timeSpanString);
        }

        private static string FormatDateTime(object value, string format)
        {
            string dateTimeString = ((DateTime)value).ToString(format, CultureInfo.InvariantCulture);
            return EscapeQuotedSqlString(dateTimeString);
        }

        private static string FormatDateTimeOffset(object value)
        {
            string dateTimeString = ((DateTimeOffset)value).ToString(CultureInfo.InvariantCulture);
            return EscapeQuotedSqlString(dateTimeString);
        }

        private static string FormatBinary(object value)
        {
            byte[] bytes = value as byte[];
            if (bytes == null)
            {
                // Bypass processing if we can't turn this into a byte[]
                return "NULL";
            }

            return "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
        }

        private static bool HasNextCharacter(string haystack, char needle, int position)
        {
            return position + 1 < haystack.Length
                   && haystack[position + 1] == needle;
        }

        /// <summary>
        /// Returns a valid SQL string packaged in single quotes with single quotes inside escaped
        /// </summary>
        /// <param name="rawString">String to be formatted</param>
        /// <returns>Formatted SQL string</returns>
        private static string EscapeQuotedSqlString(string rawString)
        {
            return $"N'{EscapeString(rawString, '\'')}'";
        }

        /// <summary>
        /// Replaces all instances of <paramref name="escapeCharacter"/> with a duplicate of 
        /// <paramref name="escapeCharacter"/>. For example "can't" becomes "can''t"
        /// </summary>
        /// <param name="value">The string to escape</param>
        /// <param name="escapeCharacter">The character to escape</param>
        /// <returns>The escaped string</returns>
        private static string EscapeString(string value, char escapeCharacter)
        {
            Validate.IsNotNull(nameof(value), value);

            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                sb.Append(c);
                if (escapeCharacter == c)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string UnEscapeString(string value, char escapeCharacter)
        {
            Validate.IsNotNull(nameof(value), value);

            // Replace 2x of the escape character with 1x of the escape character
            return value.Replace(new string(escapeCharacter, 2), escapeCharacter.ToString());
        }

        #endregion
    }
}
