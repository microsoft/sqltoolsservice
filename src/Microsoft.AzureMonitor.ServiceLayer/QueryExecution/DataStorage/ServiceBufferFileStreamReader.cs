//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using Microsoft.AzureMonitor.ServiceLayer.Localization;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.DataContracts.SqlContext;
using Microsoft.SqlTools.Utility;

namespace Microsoft.AzureMonitor.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Reader for service buffer formatted file streams
    /// </summary>
    public class ServiceBufferFileStreamReader : IFileStreamReader
    {

        #region Constants

        private const int DefaultBufferSize = 8192;
        private const string DateFormatString = "yyyy-MM-dd";
        private const string TimeFormatString = "HH:mm:ss";

        #endregion

        #region Member Variables

        private delegate FileStreamReadResult ReadMethod(long fileOffset, long rowId, DbColumnWrapper column);

        private byte[] _buffer;
        private readonly QueryExecutionSettings _executionSettings;
        private readonly Stream _fileStream;
        private readonly Dictionary<Type, ReadMethod> _readMethods;

        #endregion

        /// <summary>
        /// Constructs a new ServiceBufferFileStreamReader and initializes its state
        /// </summary>
        /// <param name="stream">The filestream to read from</param>
        /// <param name="settings">The query execution settings</param>
        public ServiceBufferFileStreamReader(Stream stream, QueryExecutionSettings settings)
        {
            Validate.IsNotNull(nameof(stream), stream);
            Validate.IsNotNull(nameof(settings), settings);

            // Open file for reading/writing
            if (!stream.CanRead || !stream.CanSeek)
            {
                throw new InvalidOperationException("Stream must be readable and seekable");
            }
            _fileStream = stream;

            _executionSettings = settings;

            // Create internal buffer
            _buffer = new byte[DefaultBufferSize];

            // Create the methods that will be used to read back
            _readMethods = new Dictionary<Type, ReadMethod>
            {
                {typeof(string),         (o, id, col) => ReadString(o, id)},
                {typeof(short),          (o, id, col) => ReadInt16(o, id)},
                {typeof(int),            (o, id, col) => ReadInt32(o, id)},
                {typeof(long),           (o, id, col) => ReadInt64(o, id)},
                {typeof(byte),           (o, id, col) => ReadByte(o, id)},
                {typeof(char),           (o, id, col) => ReadChar(o, id)},
                {typeof(bool),           (o, id, col) => ReadBoolean(o, id)},
                {typeof(double),         (o, id, col) => ReadDouble(o, id)},
                {typeof(float),          (o, id, col) => ReadSingle(o, id)},
                {typeof(decimal),        (o, id, col) => ReadDecimal(o, id)},
                {typeof(DateTime),       ReadDateTime},
                {typeof(DateTimeOffset), (o, id, col) => ReadDateTimeOffset(o, id)},
                {typeof(TimeSpan),       (o, id, col) => ReadTimeSpan(o, id)},
                {typeof(byte[]),         (o, id, col) => ReadBytes(o, id)},
                {typeof(Guid),           (o, id, col) => ReadGuid(o, id)},

                {typeof(SqlString),      (o, id, col) => ReadString(o, id)},
                {typeof(SqlInt16),       (o, id, col) => ReadInt16(o, id)},
                {typeof(SqlInt32),       (o, id, col) => ReadInt32(o, id)},
                {typeof(SqlInt64),       (o, id, col) => ReadInt64(o, id)},
                {typeof(SqlByte),        (o, id, col) => ReadByte(o, id)},
                {typeof(SqlBoolean),     (o, id, col) => ReadBoolean(o, id)},
                {typeof(SqlDouble),      (o, id, col) => ReadDouble(o, id)},
                {typeof(SqlSingle),      (o, id, col) => ReadSingle(o, id)},
                {typeof(SqlDecimal),     (o, id, col) => ReadSqlDecimal(o, id)},
                {typeof(SqlDateTime),    ReadDateTime},
                {typeof(SqlBytes),       (o, id, col) => ReadBytes(o, id)},
                {typeof(SqlBinary),      (o, id, col) => ReadBytes(o, id)},
                {typeof(SqlGuid),        (o, id, col) => ReadGuid(o, id)},
                {typeof(SqlMoney),       (o, id, col) => ReadMoney(o, id)},
            };
        }

        #region IFileStreamStorage Implementation

        /// <summary>
        /// Reads a row from the file, based on the columns provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file where the row starts</param>
        /// <param name="rowId">Internal ID of the row to set for all cells in this row</param>
        /// <param name="columns">The columns that were encoded</param>
        /// <returns>The objects from the row, ready for output to the client</returns>
        public IList<DbCellValue> ReadRow(long fileOffset, long rowId, IEnumerable<DbColumnWrapper> columns)
        {
            // Initialize for the loop
            long currentFileOffset = fileOffset;
            List<DbCellValue> results = new List<DbCellValue>();

            // Iterate over the columns
            foreach (DbColumnWrapper column in columns)
            {
                Type colType = column.DataType;
                
                // Use the right read function for the type to read the data from the file
                if (!_readMethods.TryGetValue(colType, out ReadMethod readFunc))
                {
                    // Treat everything else as a string
                    readFunc = _readMethods[typeof(string)];
                }
                FileStreamReadResult result = readFunc(currentFileOffset, rowId, column);
                currentFileOffset += result.TotalLength;
                results.Add(result.Value);
            }

            return results;
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Creates a new buffer that is of the specified length if the buffer is not already
        /// at least as long as specified.
        /// </summary>
        /// <param name="newBufferLength">The minimum buffer size</param>
        private void AssureBufferLength(int newBufferLength)
        {
            if (_buffer.Length < newBufferLength)
            {
                _buffer = new byte[newBufferLength];
            }
        }

        /// <summary>
        /// Reads the value of a cell from the file wrapper, checks to see if it null using
        /// <paramref name="isNullFunc"/>, and converts it to the proper output type using
        /// <paramref name="convertFunc"/>.
        /// </summary>
        /// <param name="offset">Offset into the file to read from</param>
        /// <param name="rowId">Internal ID of the row to set on all cells in this row</param>
        /// <param name="convertFunc">Function to use to convert the buffer to the target type</param>
        /// <param name="isNullFunc">
        /// If provided, this function will be used to determine if the value is null
        /// </param>
        /// <param name="toStringFunc">Optional function to use to convert the object to a string.</param>
        /// <param name="setInvariantCultureDisplayValue">Optional parameter indicates whether the culture invariant display value should be provided.</param>
        /// <typeparam name="T">The expected type of the cell. Used to keep the code honest</typeparam>
        /// <returns>The object, a display value, and the length of the value + its length</returns>
        private FileStreamReadResult ReadCellHelper<T>(long offset, long rowId,
            Func<int, T> convertFunc,
            Func<int, bool> isNullFunc = null,
            Func<T, string> toStringFunc = null,
            bool setInvariantCultureDisplayValue = false)
        {
            LengthResult length = ReadLength(offset);
            DbCellValue result = new DbCellValue { RowId = rowId };

            if (isNullFunc?.Invoke(length.TotalLength) ?? length.ValueLength == 0)
            {
                result.RawObject = null;
                result.DisplayValue = SR.QueryServiceCellNull;
                result.IsNull = true;
            }
            else
            {
                AssureBufferLength(length.ValueLength);
                _fileStream.Read(_buffer, 0, length.ValueLength);
                T resultObject = convertFunc(length.ValueLength);
                result.RawObject = resultObject;
                result.DisplayValue = toStringFunc == null ? result.RawObject.ToString() : toStringFunc(resultObject);
                if (setInvariantCultureDisplayValue)
                {
                    string icDisplayValue = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", result.RawObject);

                    // Only set the value when it is different from the DisplayValue to reduce the size of the result
                    //
                    if (icDisplayValue != result.DisplayValue)
                    {
                        result.InvariantCultureDisplayValue = icDisplayValue;
                    }
                }
                result.IsNull = false;
            }

            return new FileStreamReadResult(result, length.TotalLength);
        }

        /// <summary>
        /// Reads a short from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the short from</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <returns>A short</returns>
        private FileStreamReadResult ReadInt16(long fileOffset, long rowId)
        {
            return ReadCellHelper(fileOffset, rowId, length => BitConverter.ToInt16(_buffer, 0));
        }

        /// <summary>
        /// Reads a int from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the int from</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <returns>An int</returns>
        private FileStreamReadResult ReadInt32(long fileOffset, long rowId)
        {
            return ReadCellHelper(fileOffset, rowId, length => BitConverter.ToInt32(_buffer, 0));
        }

        /// <summary>
        /// Reads a long from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the long from</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <returns>A long</returns>
        private FileStreamReadResult ReadInt64(long fileOffset, long rowId)
        {
            return ReadCellHelper(fileOffset, rowId, length => BitConverter.ToInt64(_buffer, 0));
        }

        /// <summary>
        /// Reads a byte from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the byte from</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <returns>A byte</returns>
        private FileStreamReadResult ReadByte(long fileOffset, long rowId)
        {
            return ReadCellHelper(fileOffset, rowId, length => _buffer[0]);
        }

        /// <summary>
        /// Reads a char from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the char from</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <returns>A char</returns>
        private FileStreamReadResult ReadChar(long fileOffset, long rowId)
        {
            return ReadCellHelper(fileOffset, rowId, length => BitConverter.ToChar(_buffer, 0));
        }

        /// <summary>
        /// Reads a bool from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the bool from</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <returns>A bool</returns>
        private FileStreamReadResult ReadBoolean(long fileOffset, long rowId)
        {
            // Override the stringifier with numeric values if the user prefers that
            return ReadCellHelper(fileOffset, rowId, length => _buffer[0] == 0x1,
                toStringFunc: val => _executionSettings.DisplayBitAsNumber
                    ? val ? "1" : "0"
                    : val.ToString());
        }

        /// <summary>
        /// Reads a single from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the single from</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <returns>A single</returns>
        private FileStreamReadResult ReadSingle(long fileOffset, long rowId)
        {
            return ReadCellHelper(fileOffset, rowId, length => BitConverter.ToSingle(_buffer, 0), setInvariantCultureDisplayValue: true);
        }

        /// <summary>
        /// Reads a double from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the double from</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <returns>A double</returns>
        private FileStreamReadResult ReadDouble(long fileOffset, long rowId)
        {
            return ReadCellHelper(fileOffset, rowId, length => BitConverter.ToDouble(_buffer, 0), setInvariantCultureDisplayValue: true);
        }

        /// <summary>
        /// Reads a SqlDecimal from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the SqlDecimal from</param>
        /// <param name="rowId"></param>
        /// <returns>A SqlDecimal</returns>
        private FileStreamReadResult ReadSqlDecimal(long offset, long rowId)
        {
            return ReadCellHelper(offset, rowId, length =>
            {
                int[] arrInt32 = new int[(length - 3) / 4];
                Buffer.BlockCopy(_buffer, 3, arrInt32, 0, length - 3);
                return new SqlDecimal(_buffer[0], _buffer[1], _buffer[2] == 1, arrInt32);
            }, setInvariantCultureDisplayValue: true);
        }

        /// <summary>
        /// Reads a decimal from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the decimal from</param>
        /// <param name="rowId"></param>
        /// <returns>A decimal</returns>
        private FileStreamReadResult ReadDecimal(long offset, long rowId)
        {
            return ReadCellHelper(offset, rowId, length =>
            {
                int[] arrInt32 = new int[length / 4];
                Buffer.BlockCopy(_buffer, 0, arrInt32, 0, length);
                return new decimal(arrInt32);
            }, setInvariantCultureDisplayValue: true);
        }

        /// <summary>
        /// Reads a DateTime from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the DateTime from</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <param name="col">Column metadata, used for determining what precision to output</param>
        /// <returns>A DateTime</returns>
        private FileStreamReadResult ReadDateTime(long offset, long rowId, DbColumnWrapper col)
        {
            return ReadCellHelper(offset, rowId, length =>
            {
                long ticks = BitConverter.ToInt64(_buffer, 0);
                return new DateTime(ticks);

            }, null, dt =>
            {
                // Switch based on the type of column
                // For anything else that returns as a CLR DateTime, just show date and time
                var formatString = $"{DateFormatString} {TimeFormatString}";

                return dt.ToString(formatString);
            });
        }

        /// <summary>
        /// Reads a DateTimeOffset from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the DateTimeOffset from</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <returns>A DateTimeOffset</returns>
        private FileStreamReadResult ReadDateTimeOffset(long offset, long rowId)
        {
            // DateTimeOffset is represented by DateTime.Ticks followed by TimeSpan.Ticks
            // both as Int64 values
            return ReadCellHelper(offset, rowId, length =>
            {
                long dtTicks = BitConverter.ToInt64(_buffer, 0);
                long dtOffset = BitConverter.ToInt64(_buffer, 8);
                return new DateTimeOffset(new DateTime(dtTicks), new TimeSpan(dtOffset));
            }, null, dt =>
            {
                string formatString = $"{DateFormatString} {TimeFormatString}.fffffff zzz";

                return dt.ToString(formatString);
            });
        }

        /// <summary>
        /// Reads a TimeSpan from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the TimeSpan from</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <returns>A TimeSpan</returns>
        private FileStreamReadResult ReadTimeSpan(long offset, long rowId)
        {
            return ReadCellHelper(offset, rowId, length =>
            {
                long ticks = BitConverter.ToInt64(_buffer, 0);
                return new TimeSpan(ticks);
            });
        }

        /// <summary>
        /// Reads a string from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the string from</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <returns>A string</returns>
        private FileStreamReadResult ReadString(long offset, long rowId)
        {
            return ReadCellHelper(offset, rowId, length =>
                length > 0
                    ? Encoding.Unicode.GetString(_buffer, 0, length)
                    : string.Empty, totalLength => totalLength == 1);
        }

        /// <summary>
        /// Reads bytes from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the bytes from</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <returns>A byte array</returns>
        private FileStreamReadResult ReadBytes(long offset, long rowId)
        {
            return ReadCellHelper(offset, rowId, length =>
            {
                byte[] output = new byte[length];
                Buffer.BlockCopy(_buffer, 0, output, 0, length);
                return output;
            }, totalLength => totalLength == 1,
            bytes =>
            {
                StringBuilder sb = new StringBuilder("0x");
                foreach (byte b in bytes)
                {
                    sb.AppendFormat("{0:X2}", b);
                }
                return sb.ToString();
            });
        }

        /// <summary>
        /// Reads the bytes that make up a GUID at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the bytes from</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <returns>A system guid type object</returns>
        private FileStreamReadResult ReadGuid(long offset, long rowId)
        {
            return ReadCellHelper(offset, rowId, length =>
            {
                byte[] output = new byte[length];
                Buffer.BlockCopy(_buffer, 0, output, 0, length);
                return new Guid(output);
            }, totalLength => totalLength == 1);
        }

        /// <summary>
        /// Reads a SqlMoney type from the offset provided
        /// into a 
        /// </summary>
        /// <param name="offset">Offset into the file to read the value</param>
        /// <param name="rowId">Internal ID of the row that will be stored in the cell</param>
        /// <returns>A sql money type object</returns>
        private FileStreamReadResult ReadMoney(long offset, long rowId)
        {
            return ReadCellHelper(offset, rowId, length =>
            {
                int[] arrInt32 = new int[length / 4];
                Buffer.BlockCopy(_buffer, 0, arrInt32, 0, length);
                return new SqlMoney(new decimal(arrInt32));
            });
        }

        /// <summary>
        /// Reads the length of a field at the specified offset in the file
        /// </summary>
        /// <param name="offset">Offset into the file to read the field length from</param>
        /// <returns>A LengthResult</returns>
        private LengthResult ReadLength(long offset)
        {
            // read in length information
            int lengthValue;
            _fileStream.Seek(offset, SeekOrigin.Begin);
            int lengthLength = _fileStream.Read(_buffer, 0, 1);
            if (_buffer[0] != 0xFF)
            {
                // one byte is enough
                lengthValue = Convert.ToInt32(_buffer[0]);
            }
            else
            {
                // read in next 4 bytes
                lengthLength += _fileStream.Read(_buffer, 0, 4);

                // reconstruct the length
                lengthValue = BitConverter.ToInt32(_buffer, 0);
            }

            return new LengthResult { LengthLength = lengthLength, ValueLength = lengthValue };
        }

        #endregion

        /// <summary>
        /// Internal struct used for representing the length of a field from the file
        /// </summary>
        private struct LengthResult
        {
            /// <summary>
            /// How many bytes the length takes up
            /// </summary>
            public int LengthLength { get; set; }

            /// <summary>
            /// How many bytes the value takes up
            /// </summary>
            public int ValueLength { get; set; }

            /// <summary>
            /// <see cref="LengthLength"/> + <see cref="ValueLength"/>
            /// </summary>
            public int TotalLength => LengthLength + ValueLength;
        }

        #region IDisposable Implementation

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _fileStream.Dispose();
            }

            _disposed = true;
        }

        ~ServiceBufferFileStreamReader()
        {
            Dispose(false);
        }

        #endregion

    }
}
