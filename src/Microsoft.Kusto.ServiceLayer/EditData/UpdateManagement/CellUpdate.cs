//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Kusto.ServiceLayer.EditData.Contracts;
using Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts;
using Microsoft.Kusto.ServiceLayer.Utility.SqlScriptFormatters;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.EditData.UpdateManagement
{
    /// <summary>
    /// Representation of a cell that should have a value inserted or updated
    /// </summary>
    public sealed class CellUpdate
    {
        private const string NullString = @"NULL";
        private const string TextNullString = @"'NULL'";
        private static readonly Regex HexRegex = new Regex("0x[0-9A-F]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly TimeSpan MaxTimespan = TimeSpan.FromHours(24);

        /// <summary>
        /// Constructs a new cell update based on the the string value provided and the column
        /// for the cell.
        /// </summary>
        /// <param name="column">Column the cell will be under</param>
        /// <param name="valueAsString">The string from the client to convert to an object</param>
        public CellUpdate(DbColumnWrapper column, string valueAsString)
        {
            Validate.IsNotNull(nameof(column), column);
            Validate.IsNotNull(nameof(valueAsString), valueAsString);

            // Store the state that won't be changed
            try
            {
                Column = column;
                Type columnType = column.DataType;

                // Check for null
                if (valueAsString == NullString)
                {
                    ProcessNullValue();
                }
                else if (columnType == typeof(byte[]))
                {
                    // Binary columns need special attention
                    ProcessBinaryCell(valueAsString);
                }
                else if (columnType == typeof(string))
                {
                    ProcessTextCell(valueAsString);
                }
                else if (columnType == typeof(Guid))
                {
                    Value = Guid.Parse(valueAsString);
                    ValueAsString = Value.ToString();
                }
                else if (columnType == typeof(TimeSpan))
                {
                    ProcessTimespanColumn(valueAsString);
                }
                else if (columnType == typeof(DateTimeOffset))
                {
                    Value = DateTimeOffset.Parse(valueAsString, CultureInfo.CurrentCulture);
                    ValueAsString = Value.ToString();
                }
                else if (columnType == typeof(bool))
                {
                    ProcessBooleanCell(valueAsString);
                }
                // @TODO: Microsoft.SqlServer.Types.SqlHierarchyId
                else
                {
                    // Attempt to go straight to the destination type, if we know what it is, otherwise
                    // leave it as a string
                    Value = columnType != null
                        ? Convert.ChangeType(valueAsString, columnType, CultureInfo.CurrentCulture)
                        : valueAsString;
                    ValueAsString = Value.ToString();
                }
            }
            catch (FormatException fe)
            {
                // Pretty up the exception so the user can learn a bit from it
                // NOTE: Other formatting errors raised by helpers are InvalidOperationException to
                //       avoid being prettied here
                throw new FormatException(SR.EditDataInvalidFormat(column.ColumnName, ToSqlScript.FormatColumnType(column)), fe);
            }
        }

        #region Properties

        /// <summary>
        /// Converts the cell update to a DbCellValue
        /// </summary>
        public DbCellValue AsDbCellValue
        {
            get
            {
                return new DbCellValue
                {
                    DisplayValue = ValueAsString,
                    IsNull = Value == DBNull.Value,
                    RawObject = Value
                };
            }
        }

        /// <summary>
        /// Generates a new EditCell that represents the contents of the cell update
        /// </summary>
        public EditCell AsEditCell
        {
            get { return new EditCell(AsDbCellValue, true); }
        }

        /// <summary>
        /// The column that the cell will be placed in
        /// </summary>
        public DbColumnWrapper Column { get; }

        /// <summary>
        /// The object representation of the cell provided by the client
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// <see cref="Value"/> converted to a string
        /// </summary>
        public string ValueAsString { get; private set; }

        #endregion

        #region Private Helpers

        private void ProcessBinaryCell(string valueAsString)
        {
            string trimmedString = valueAsString.Trim();

            byte[] byteArray;
            uint uintVal;
            if (uint.TryParse(trimmedString, NumberStyles.None, CultureInfo.InvariantCulture, out uintVal))
            {
                // Get the bytes
                byteArray = BitConverter.GetBytes(uintVal);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(byteArray);
                }
                Value = byteArray;

                // User typed something numeric (may be hex or dec)
                if ((uintVal & 0xFFFFFF00) == 0)
                {
                    // Value can fit in a single byte
                    Value = new[] { byteArray[3] };
                }
                else if ((uintVal & 0xFFFF0000) == 0)
                {
                    // Value can fit in two bytes
                    Value = new[] { byteArray[2], byteArray[3] };
                }
                else if ((uintVal & 0xFF000000) == 0)
                {
                    // Value can fit in three bytes
                    Value = new[] { byteArray[1], byteArray[2], byteArray[3] };
                }
            }
            else if (HexRegex.IsMatch(valueAsString))
            {
                // User typed something that starts with a hex identifier (0x)
                // Strip off the 0x, pad with zero if necessary
                trimmedString = trimmedString.Substring(2);
                if (trimmedString.Length % 2 == 1)
                {
                    trimmedString = "0" + trimmedString;
                }

                // Convert to a byte array
                byteArray = new byte[trimmedString.Length / 2];
                for (int i = 0; i < trimmedString.Length; i += 2)
                {
                    string bString = $"{trimmedString[i]}{trimmedString[i + 1]}";
                    byte bVal = byte.Parse(bString, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                    byteArray[i / 2] = bVal;
                }
                Value = byteArray;
            }
            else
            {
                throw new InvalidOperationException(SR.EditDataInvalidFormatBinary);
            }

            // Generate the hex string as the return value
            ValueAsString = "0x" + BitConverter.ToString((byte[])Value).Replace("-", string.Empty);
        }

        private void ProcessBooleanCell(string valueAsString)
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
                        throw new InvalidOperationException(SR.EditDataInvalidFormatBoolean);
                }
            }
            else
            {
                // Allow user to enter true or false
                Value = bool.Parse(valueAsString);
            }

            ValueAsString = Value.ToString();
        }

        private void ProcessTimespanColumn(string valueAsString)
        {
            TimeSpan ts = TimeSpan.Parse(valueAsString, CultureInfo.CurrentCulture);
            if (ts >= MaxTimespan)
            {
                throw new InvalidOperationException(SR.EditDataTimeOver24Hrs);
            }

            Value = ts;
            ValueAsString = Value.ToString();
        }

        private void ProcessNullValue()
        {
            // Make sure that nulls are allowed if we set it to null
            if (!Column.AllowDBNull.HasTrue())
            {
                throw new InvalidOperationException(SR.EditDataNullNotAllowed);
            }

            Value = DBNull.Value;
            ValueAsString = NullString;
        }

        private void ProcessTextCell(string valueAsString)
        {
            // Special case for strings because the string value should stay the same as provided
            // If user typed 'NULL' they mean NULL as text
            Value = valueAsString == TextNullString ? NullString : valueAsString;
            
            // Make sure that the value fits inside the size of the column
            if (Column.ColumnSize.HasValue && valueAsString.Length > Column.ColumnSize)
            {
                string columnSizeString = $"({Column.ColumnSize.Value})";
                string columnTypeString = Column.DataTypeName.ToUpperInvariant() + columnSizeString;
                throw new InvalidOperationException(SR.EditDataValueTooLarge(valueAsString, columnTypeString));
            }
            
            ValueAsString = valueAsString;
        }

        #endregion
    }
}
