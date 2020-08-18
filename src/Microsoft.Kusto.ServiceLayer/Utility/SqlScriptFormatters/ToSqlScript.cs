﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.Utility.SqlScriptFormatters
{
    /// <summary>
    /// Provides utility for converting arbitrary objects into strings that are ready to be
    /// inserted into SQL strings
    /// </summary>
    public static class ToSqlScript
    {
        #region Constants

        public const string NullString = "NULL";

        private static readonly Dictionary<string, Func<object, DbColumn, string>> FormatFunctions =
           new Dictionary<string, Func<object, DbColumn, string>>
           {                                                                                        // CLR Type --------
                {"bigint", (val, col) => SimpleFormatter(val)},                                     // long
                {"bit", (val, col) => FormatBool(val)},                                             // bool
                {"int", (val, col) => SimpleFormatter(val)},                                        // int
                {"smallint", (val, col) => SimpleFormatter(val)},                                   // short
                {"tinyint", (val, col) => SimpleFormatter(val)},                                    // byte
                {"money", FormatDecimalLike},                                                       // Decimal
                {"smallmoney", FormatDecimalLike},                                                  // Decimal
                {"decimal", FormatDecimalLike},                                                     // Decimal
                {"numeric", FormatDecimalLike},                                                     // Decimal
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

        #endregion
        
        #region Public Methods

        /// <summary>
        /// Extracts a DbColumn's datatype and turns it into script ready 
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        /// <seealso cref="Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel.SmoColumnCustomNodeHelper.GetTypeSpecifierLabel"/>
        /// <exception cref="InvalidOperationException"></exception>
        public static string FormatColumnType(DbColumn column, bool useSemanticEquivalent = false)
        {
            string typeName = column.DataTypeName.ToUpperInvariant();
            
            // TODO: This doesn't support UDTs at all.
            // TODO: It's unclear if this will work on a case-sensitive db collation
            
            // Strip any unecessary info from the front certain types
            if (typeName.EndsWith("HIERARCHYID") || typeName.EndsWith("GEOGRAPHY") || typeName.EndsWith("GEOMETRY"))
            {
                string[] typeNameComponents = typeName.Split(".");
                typeName = typeNameComponents[typeNameComponents.Length - 1];
            }
            
            // Replace timestamp columns with semantic equivalent if requested
            if (useSemanticEquivalent && typeName == "TIMESTAMP")
            {
                typeName = "VARBINARY(8)";
            }
            
            // If the type supports length parameters, the add those
            switch (typeName)
            {
                // Types with length
                case "CHAR":
                case "NCHAR":
                case "VARCHAR":
                case "NVARCHAR":
                case "BINARY":
                case "VARBINARY":
                    if (!column.ColumnSize.HasValue)
                    {
                        throw new InvalidOperationException(SR.SqlScriptFormatterLengthTypeMissingSize);
                    }

                    string length = column.ColumnSize.Value == int.MaxValue
                        ? "MAX"
                        : column.ColumnSize.Value.ToString();

                    typeName += $"({length})";
                    break;
                    
                // Types with precision and scale
                case "NUMERIC":
                case "DECIMAL":
                    if (!column.NumericPrecision.HasValue || !column.NumericScale.HasValue)
                    {
                        throw new InvalidOperationException(SR.SqlScriptFormatterDecimalMissingPrecision);
                    }
                    typeName += $"({column.NumericPrecision}, {column.NumericScale})";
                    break;
                
                // Types with scale only
                case "DATETIME2":
                case "DATETIMEOFFSET":
                case "TIME":
                    if (!column.NumericScale.HasValue)
                    {
                        throw new InvalidOperationException(SR.SqlScriptFormatterScalarTypeMissingScale);
                    }
                    typeName += $"({column.NumericScale})";
                    break;
            }

            return typeName;
        }

        #endregion
        
        #region Private Helpers
        
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
        
        private static string FormatBool(object value)
        {
            // Attempt to cast to bool
            bool boolValue = (bool)value;
            return boolValue ? "1" : "0";
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
        
        private static string FormatDouble(object value)
        {
            // The "R" formatting means "Round Trip", which preserves fidelity
            return ((double)value).ToString("R");
        }
        
        private static string FormatFloat(object value)
        {
            // The "R" formatting means "Round Trip", which preserves fidelity
            return ((float)value).ToString("R");
        }

        private static string FormatDecimalLike(object value, DbColumn column)
        {
            string numericString = ((decimal)value).ToString(CultureInfo.InvariantCulture);
            string typeString = FormatColumnType(column);
            return $"CAST({numericString} AS {typeString})";
        }
        
        private static string FormatTimeSpan(object value)
        {
            // "c" provides "HH:mm:ss.FFFFFFF", and time column accepts up to 7 precision
            string timeSpanString = ((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture);
            return EscapeQuotedSqlString(timeSpanString);
        }
        
        private static string SimpleFormatter(object value)
        {
            return value.ToString();
        }
        
        private static string SimpleStringFormatter(object value)
        {
            return EscapeQuotedSqlString(value.ToString());
        }
        
        #endregion
    }
}