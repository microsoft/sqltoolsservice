using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    /// <summary>
    /// Provides utility for converting arbitrary objects into strings that are ready to be
    /// inserted into SQL strings
    /// </summary>
    public class SqlValueScriptFormatter
    {
        public const string NullString = "NULL";

        /// <summary>
        /// Converts a cell value into a string for SQL script
        /// </summary>
        /// <param name="value">The cell to convert</param>
        /// <param name="column">The column metadata for the cell to insert</param>
        /// <returns>String version of the cell value for use in SQL scripts</returns>
        public static string Format(DbCellValue value, DbColumn column)
        {
            Validate.IsNotNull(nameof(value), value);
            Validate.IsNotNull(nameof(column), column);

            // Handle nulls firstly
            if (value.RawObject == null)
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

        private static readonly Dictionary<string, Func<DbCellValue, DbColumn, string>> FormatFunctions = 
            new Dictionary<string, Func<DbCellValue, DbColumn, string>>
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

        private static string SimpleFormatter(DbCellValue value)
        {
            return value.RawObject.ToString();
        }

        private static string SimpleStringFormatter(DbCellValue value)
        {
            return EscapeQuotedSqlString(value.RawObject.ToString());
        }

        private static string FormatMoney(DbCellValue value, string type)
        {
            // we have to manually format the string by ToStringing the value first, and then converting 
            // the potential (European formatted) comma to a period.
            string numericString = ((decimal)value.RawObject).ToString(CultureInfo.InvariantCulture);
            return $"CAST({numericString} AS {type})";
        }

        private static string FormatFloat(DbCellValue value)
        {
            // The "R" formatting means "Round Trip", which preserves fidelity
            return ((float)value.RawObject).ToString("R");
        }

        private static string FormatDouble(DbCellValue value)
        {
            // The "R" formatting means "Round Trip", which preserves fidelity
            return ((double)value.RawObject).ToString("R");
        }

        private static string FormatBool(DbCellValue value)
        {
            // Attempt to cast to bool
            bool boolValue = (bool)value.RawObject;
            return boolValue ? "1" : "0";
        }

        private static string FormatPreciseNumeric(DbCellValue value, DbColumn column, string type)
        {
            // Make sure we have numeric precision and numeric scale
            if (!column.NumericPrecision.HasValue || !column.NumericScale.HasValue)
            {
                // @TODO Move to constants
                throw new InvalidOperationException("Decimal column is missing numeric precision or numeric scale");
            }

            // Convert the value to a decimal, then convert that to a string
            string numericString = ((decimal) value.RawObject).ToString(CultureInfo.InvariantCulture);
            return string.Format(CultureInfo.InvariantCulture, "CAST({0} AS {1}({2}, {3}))",
                numericString, type, column.NumericPrecision.Value, column.NumericScale.Value);
        }

        private static string FormatTimeSpan(DbCellValue value)
        {
            // "c" provides "HH:mm:ss.FFFFFFF", and time column accepts up to 7 precision
            string timeSpanString = ((TimeSpan) value.RawObject).ToString("c", CultureInfo.InvariantCulture);
            return EscapeQuotedSqlString(timeSpanString);
        } 

        private static string FormatDateTime(DbCellValue value, string format)
        {
            string dateTimeString = ((DateTime) value.RawObject).ToString(format, CultureInfo.InvariantCulture);
            return EscapeQuotedSqlString(dateTimeString);
        }

        private static string FormatDateTimeOffset(DbCellValue value)
        {
            string dateTimeString = ((DateTimeOffset) value.RawObject).ToString(CultureInfo.InvariantCulture);
            return EscapeQuotedSqlString(dateTimeString);
        }

        private static string FormatBinary(DbCellValue value)
        {
            byte[] bytes = value.RawObject as byte[];
            if (bytes == null)
            {
                // Bypass processing if we can't turn this into a byte[]
                return "NULL";
            }

            return "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
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
    }
}
