//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    /// <summary>
    /// Representation of a cell that should have a value inserted or updated
    /// </summary>
    public sealed class CellUpdate
    {
        private const string NullString = @"NULL";
        private const string TextNullString = @"'NULL'";
        private static readonly Regex HexRegex = new Regex("0x[0-9A-F]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Constructs a new cell update based on the the string value provided and the column
        /// for the cell.
        /// </summary>
        /// <param name="column">Column the cell will be under</param>
        /// <param name="valueAsString">The string from the client to convert to an object</param>
        public CellUpdate(DbColumn column, string valueAsString)
        {
            // Store the state that won't be changed
            Column = column;
            Type columnType = column.DataType;

            // Check for null
            if (valueAsString.Equals(NullString, StringComparison.OrdinalIgnoreCase))
            {
                Value = DBNull.Value;
            }
            // Perform different conversions for different column types
            if (columnType == typeof(string) || columnType == typeof(System.Xml.XmlReader))
            {
                // If user typed 'NULL' they mean NULL as text
                Value = valueAsString == TextNullString ? NullString : valueAsString;
            } 
            else if (columnType == typeof(byte[]))
            {
                string trimmedString = valueAsString.Trim();
                int intVal;
                if (int.TryParse(trimmedString, NumberStyles.None, CultureInfo.InvariantCulture, out intVal))
                {
                    // User typed something like 10
                    Value = intVal;
                }
                else if (int.TryParse(trimmedString, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out intVal))
                {
                    // User typed something like 0x10
                    Value = intVal;
                }
                else if (HexRegex.IsMatch(valueAsString))
                {
                    // User typed something like 0xFFFFFFFFFF...
                    Value = valueAsString;
                }
                else
                {
                    // @TODO: What?
                    throw new InvalidOperationException("You cannot use the Result pane to set this field data to values other than NULL.");
                }
            }
            else if (columnType == typeof(Guid))
            {
                Value = Guid.Parse(valueAsString);
            }
            else if (columnType == typeof(TimeSpan))
            {
                Value = TimeSpan.Parse(valueAsString, CultureInfo.CurrentCulture);
            }
            else if (columnType == typeof(DateTimeOffset))
            {
                Value = DateTimeOffset.Parse(valueAsString, CultureInfo.CurrentCulture);
            }
            else if (columnType == typeof(bool))
            {
                // Allow user to enter 1 or 0
                string trimmedString = valueAsString.Trim();
                int intVal;
                if (int.TryParse(trimmedString, out intVal))
                {
                    switch (intVal)
                    {
                        case 1:
                            Value = true;
                            break;
                        case 0:
                            Value = false;
                            break;
                        default:
                            // @TODO: Move to constants file
                            throw new ArgumentOutOfRangeException(nameof(valueAsString),
                                "Boolean columns must be numeric 1 or 0, or string true or false");
                    }
                }
                else
                {
                    // Allow user to enter true or false
                    Value = bool.Parse(valueAsString);
                }
            }
            // @TODO: Microsoft.SqlServer.Types.SqlHierarchyId
            else
            {
                // Attempt to go straight to the destination type, if we know what it is, otherwise
                // leave it as a string
                Value = columnType != null 
                    ? Convert.ChangeType(valueAsString, columnType, CultureInfo.CurrentCulture) 
                    : valueAsString;
            }

            // Set the string value as the value that was generated
            ValueAsString = Value.ToString();
        }

        #region Properties

        /// <summary>
        /// The column that the cell will be placed in
        /// </summary>
        public DbColumn Column { get; }

        /// <summary>
        /// The object representation of the cell provided by the client
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// <see cref="Value"/> converted to a string
        /// </summary>
        public string ValueAsString { get; }

        #endregion
    }
}
