//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Helper methods for formatting SQL values and identifiers consistently across save and copy scenarios.
    /// </summary>
    internal static class SqlValueFormatter
    {
        /// <summary>
        /// Formats a database cell value for use in SQL statements such as INSERT or IN clauses.
        /// </summary>
        public static string FormatValue(DbCellValue cellValue)
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
        /// Escapes SQL identifiers by wrapping them in square brackets if they contain special characters.
        /// </summary>
        public static string EscapeIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return identifier;
            }

            if (identifier.Contains(" ") || identifier.Contains("-") || !char.IsLetter(identifier[0]))
            {
                return $"[{identifier.Replace("]", "]]")}]";
            }

            return identifier;
        }

        private static bool NeedsQuoting(string value)
        {
            return !decimal.TryParse(value, out _) && !bool.TryParse(value, out _);
        }
    }
}
