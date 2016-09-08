//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Reader for SSMS formatted file streams
    /// </summary>
    /// <remarks>
    /// Most of this code is based on code from the Microsoft.SqlServer.Management.UI.Grid, SSMS DataStorage
    /// $\Data Tools\SSMS_XPlat\sql\ssms\core\DataStorage\src\FileStreamReader.cs
    /// </remarks>
    public class ServiceBufferFileStreamReader : IFileStreamReader
    {
        private const int DefaultBufferSize = 8192;

        #region Member Variables

        private byte[] buffer;

        private readonly IFileStreamWrapper fileStream;

        #endregion

        /// <summary>
        /// Constructs a new ServiceBufferFileStreamReader and initializes its state
        /// </summary>
        /// <param name="fileWrapper">The filestream wrapper to read from</param>
        /// <param name="fileName">The name of the file to read from</param>
        public ServiceBufferFileStreamReader(IFileStreamWrapper fileWrapper, string fileName)
        {
            // Open file for reading/writing
            fileStream = fileWrapper;
            fileStream.Init(fileName, DefaultBufferSize, true);

            // Create internal buffer
            buffer = new byte[DefaultBufferSize];
        }

        #region IFileStreamStorage Implementation

        /// <summary>
        /// Reads a row from the file, based on the columns provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file where the row starts</param>
        /// <param name="columns">The columns that were encoded</param>
        /// <returns>The objects from the row</returns>
        public object[] ReadRow(long fileOffset, IEnumerable<DbColumnWrapper> columns)
        {
            // Initialize for the loop
            long currentFileOffset = fileOffset;
            List<object> results = new List<object>();

            // Iterate over the columns
            foreach (DbColumnWrapper column in columns)
            {
                // We will pivot based on the type of the column
                Type colType;
                if (column.IsSqlVariant)
                {
                    // For SQL Variant columns, the type is written first in string format
                    FileStreamReadResult<string> sqlVariantTypeResult = ReadString(currentFileOffset);
                    currentFileOffset += sqlVariantTypeResult.TotalLength;

                    // If the typename is null, then the whole value is null
                    if (sqlVariantTypeResult.IsNull)
                    {
                        results.Add(null);
                        continue;
                    }

                    // The typename is stored in the string
                    colType = Type.GetType(sqlVariantTypeResult.Value);

                    // Workaround .NET bug, see sqlbu# 440643 and vswhidbey# 599834
                    // TODO: Is this workaround necessary for .NET Core?
                    if (colType == null && sqlVariantTypeResult.Value == "System.Data.SqlTypes.SqlSingle")
                    {
                        colType = typeof(SqlSingle);
                    }
                }
                else
                {
                    colType = column.DataType;
                }

                if (colType == typeof(string))
                {
                    // String - most frequently used data type
                    FileStreamReadResult<string> result = ReadString(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    results.Add(result.IsNull ? null : result.Value);
                }
                else if (colType == typeof(SqlString))
                {
                    // SqlString
                    FileStreamReadResult<string> result = ReadString(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    results.Add(result.IsNull ? null : (SqlString) result.Value);
                }
                else if (colType == typeof(short))
                {
                    // Int16
                    FileStreamReadResult<short> result = ReadInt16(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(result.Value);
                    }
                }
                else if (colType == typeof(SqlInt16))
                {
                    // SqlInt16
                    FileStreamReadResult<short> result = ReadInt16(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add((SqlInt16)result.Value);
                    }
                }
                else if (colType == typeof(int))
                {
                    // Int32
                    FileStreamReadResult<int> result = ReadInt32(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(result.Value);
                    }
                }
                else if (colType == typeof(SqlInt32))
                {
                    // SqlInt32
                    FileStreamReadResult<int> result = ReadInt32(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add((SqlInt32)result.Value);
                    }
                }
                else if (colType == typeof(long))
                {
                    // Int64
                    FileStreamReadResult<long> result = ReadInt64(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(result.Value);
                    }
                }
                else if (colType == typeof(SqlInt64))
                {
                    // SqlInt64
                    FileStreamReadResult<long> result = ReadInt64(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add((SqlInt64)result.Value);
                    }
                }
                else if (colType == typeof(byte))
                {
                    // byte
                    FileStreamReadResult<byte> result = ReadByte(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(result.Value);
                    }
                }
                else if (colType == typeof(SqlByte))
                {
                    // SqlByte
                    FileStreamReadResult<byte> result = ReadByte(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add((SqlByte)result.Value);
                    }
                }
                else if (colType == typeof(char))
                {
                    // Char
                    FileStreamReadResult<char> result = ReadChar(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(result.Value);
                    }
                }
                else if (colType == typeof(bool))
                {
                    // Bool
                    FileStreamReadResult<bool> result = ReadBoolean(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(result.Value);
                    }
                }
                else if (colType == typeof(SqlBoolean))
                {
                    // SqlBoolean
                    FileStreamReadResult<bool> result = ReadBoolean(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add((SqlBoolean)result.Value);
                    }
                }
                else if (colType == typeof(double))
                {
                    // double
                    FileStreamReadResult<double> result = ReadDouble(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(result.Value);
                    }
                }
                else if (colType == typeof(SqlDouble))
                {
                    // SqlByte
                    FileStreamReadResult<double> result = ReadDouble(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add((SqlDouble)result.Value);
                    }
                }
                else if (colType == typeof(float))
                {
                    // float
                    FileStreamReadResult<float> result = ReadSingle(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(result.Value);
                    }
                }
                else if (colType == typeof(SqlSingle))
                {
                    // SqlSingle
                    FileStreamReadResult<float> result = ReadSingle(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add((SqlSingle)result.Value);
                    }
                }
                else if (colType == typeof(decimal))
                {
                    // Decimal
                    FileStreamReadResult<decimal> result = ReadDecimal(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(result.Value);
                    }
                }
                else if (colType == typeof(SqlDecimal))
                {
                    // SqlDecimal
                    FileStreamReadResult<SqlDecimal> result = ReadSqlDecimal(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(result.Value);
                    }
                }
                else if (colType == typeof(DateTime))
                {
                    // DateTime
                    FileStreamReadResult<DateTime> result = ReadDateTime(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(result.Value);
                    }
                }
                else if (colType == typeof(SqlDateTime))
                {
                    // SqlDateTime
                    FileStreamReadResult<DateTime> result = ReadDateTime(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add((SqlDateTime)result.Value);
                    }
                }
                else if (colType == typeof(DateTimeOffset))
                {
                    // DateTimeOffset
                    FileStreamReadResult<DateTimeOffset> result = ReadDateTimeOffset(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(result.Value);
                    }
                }
                else if (colType == typeof(TimeSpan))
                {
                    // TimeSpan
                    FileStreamReadResult<TimeSpan> result = ReadTimeSpan(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(result.Value);
                    }
                }
                else if (colType == typeof(byte[]))
                {
                    // Byte Array
                    FileStreamReadResult<byte[]> result = ReadBytes(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull || (column.IsUdt && result.Value.Length == 0))
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(result.Value);
                    }
                }
                else if (colType == typeof(SqlBytes))
                {
                    // SqlBytes
                    FileStreamReadResult<byte[]> result = ReadBytes(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    results.Add(result.IsNull ? null : new SqlBytes(result.Value));
                }
                else if (colType == typeof(SqlBinary))
                {
                    // SqlBinary
                    FileStreamReadResult<byte[]> result = ReadBytes(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    results.Add(result.IsNull ? null : new SqlBinary(result.Value));
                }
                else if (colType == typeof(SqlGuid))
                {
                    // SqlGuid
                    FileStreamReadResult<byte[]> result = ReadBytes(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(new SqlGuid(result.Value));
                    }
                }
                else if (colType == typeof(SqlMoney))
                {
                    // SqlMoney
                    FileStreamReadResult<decimal> result = ReadDecimal(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    if (result.IsNull)
                    {
                        results.Add(null);
                    }
                    else
                    {
                        results.Add(new SqlMoney(result.Value));
                    }
                }
                else
                {
                    // Treat everything else as a string
                    FileStreamReadResult<string> result = ReadString(currentFileOffset);
                    currentFileOffset += result.TotalLength;
                    results.Add(result.IsNull ? null : result.Value);
                }
            }

            return results.ToArray();
        }

        private FileStreamReadResult<T> ReadValue<T>(long fileOffset, Func<T> )

        /// <summary>
        /// Reads a short from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the short from</param>
        /// <returns>A short</returns>
        public FileStreamReadResult<short> ReadInt16(long fileOffset)
        {

            LengthResult length = ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 2, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            short val = default(short);
            if (!isNull)
            {
                fileStream.ReadData(buffer, length.ValueLength);
                val = BitConverter.ToInt16(buffer, 0);
            }

            return new FileStreamReadResult<short>(val, length.TotalLength, isNull);
        }

        /// <summary>
        /// Reads a int from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the int from</param>
        /// <returns>An int</returns>
        public FileStreamReadResult<int> ReadInt32(long fileOffset)
        {
            LengthResult length = ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 4, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            int val = default(int);
            if (!isNull)
            {
                fileStream.ReadData(buffer, length.ValueLength);
                val = BitConverter.ToInt32(buffer, 0);
            }
            return new FileStreamReadResult<int>(val, length.TotalLength, isNull);
        }

        /// <summary>
        /// Reads a long from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the long from</param>
        /// <returns>A long</returns>
        public FileStreamReadResult<long> ReadInt64(long fileOffset)
        {
            LengthResult length = ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 8, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            long val = default(long);
            if (!isNull)
            {
                fileStream.ReadData(buffer, length.ValueLength);
                val = BitConverter.ToInt64(buffer, 0);
            }
            return new FileStreamReadResult<long>(val, length.TotalLength, isNull);
        }

        /// <summary>
        /// Reads a byte from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the byte from</param>
        /// <returns>A byte</returns>
        public FileStreamReadResult<byte> ReadByte(long fileOffset)
        {
            LengthResult length = ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 1, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            byte val = default(byte);
            if (!isNull)
            {
                fileStream.ReadData(buffer, length.ValueLength);
                val = buffer[0];
            }
            return new FileStreamReadResult<byte>(val, length.TotalLength, isNull);
        }

        /// <summary>
        /// Reads a char from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the char from</param>
        /// <returns>A char</returns>
        public FileStreamReadResult<char> ReadChar(long fileOffset)
        {
            LengthResult length = ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 2, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            char val = default(char);
            if (!isNull)
            {
                fileStream.ReadData(buffer, length.ValueLength);
                val = BitConverter.ToChar(buffer, 0);
            }
            return new FileStreamReadResult<char>(val, length.TotalLength, isNull);
        }

        /// <summary>
        /// Reads a bool from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the bool from</param>
        /// <returns>A bool</returns>
        public FileStreamReadResult<bool> ReadBoolean(long fileOffset)
        {
            LengthResult length = ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 1, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            bool val = default(bool);
            if (!isNull)
            {
                fileStream.ReadData(buffer, length.ValueLength);
                val = buffer[0] == 0x01;
            }
            return new FileStreamReadResult<bool>(val, length.TotalLength, isNull);
        }

        /// <summary>
        /// Reads a single from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the single from</param>
        /// <returns>A single</returns>
        public FileStreamReadResult<float> ReadSingle(long fileOffset)
        {
            LengthResult length = ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 4, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            float val = default(float);
            if (!isNull)
            {
                fileStream.ReadData(buffer, length.ValueLength);
                val = BitConverter.ToSingle(buffer, 0);
            }
            return new FileStreamReadResult<float>(val, length.TotalLength, isNull);
        }

        /// <summary>
        /// Reads a double from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the double from</param>
        /// <returns>A double</returns>
        public FileStreamReadResult<double> ReadDouble(long fileOffset)
        {
            LengthResult length = ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 8, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            double val = default(double);
            if (!isNull)
            {
                fileStream.ReadData(buffer, length.ValueLength);
                val = BitConverter.ToDouble(buffer, 0);
            }
            return new FileStreamReadResult<double>(val, length.TotalLength, isNull);
        }

        /// <summary>
        /// Reads a SqlDecimal from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the SqlDecimal from</param>
        /// <returns>A SqlDecimal</returns>
        public FileStreamReadResult<SqlDecimal> ReadSqlDecimal(long offset)
        {
            LengthResult length = ReadLength(offset);
            Debug.Assert(length.ValueLength == 0 || (length.ValueLength - 3)%4 == 0,
                string.Format("Invalid data length: {0}", length.ValueLength));

            bool isNull = length.ValueLength == 0;
            SqlDecimal val = default(SqlDecimal);
            if (!isNull)
            {
                fileStream.ReadData(buffer, length.ValueLength);

                int[] arrInt32 = new int[(length.ValueLength - 3)/4];
                Buffer.BlockCopy(buffer, 3, arrInt32, 0, length.ValueLength - 3);
                val = new SqlDecimal(buffer[0], buffer[1], 1 == buffer[2], arrInt32);
            }
            return new FileStreamReadResult<SqlDecimal>(val, length.TotalLength, isNull);
        }

        /// <summary>
        /// Reads a decimal from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the decimal from</param>
        /// <returns>A decimal</returns>
        public FileStreamReadResult<decimal> ReadDecimal(long offset)
        {
            LengthResult length = ReadLength(offset);
            Debug.Assert(length.ValueLength%4 == 0, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            decimal val = default(decimal);
            if (!isNull)
            {
                fileStream.ReadData(buffer, length.ValueLength);

                int[] arrInt32 = new int[length.ValueLength/4];
                Buffer.BlockCopy(buffer, 0, arrInt32, 0, length.ValueLength);
                val = new decimal(arrInt32);
            }
            return new FileStreamReadResult<decimal>(val, length.TotalLength, isNull);
        }

        /// <summary>
        /// Reads a DateTime from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the DateTime from</param>
        /// <returns>A DateTime</returns>
        public FileStreamReadResult<DateTime> ReadDateTime(long offset)
        {
            FileStreamReadResult<long> ticks = ReadInt64(offset);
            DateTime val = default(DateTime);
            if (!ticks.IsNull)
            {
                val = new DateTime(ticks.Value);
            }
            return new FileStreamReadResult<DateTime>(val, ticks.TotalLength, ticks.IsNull);
        }

        /// <summary>
        /// Reads a DateTimeOffset from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the DateTimeOffset from</param>
        /// <returns>A DateTimeOffset</returns>
        public FileStreamReadResult<DateTimeOffset> ReadDateTimeOffset(long offset)
        {
            // DateTimeOffset is represented by DateTime.Ticks followed by TimeSpan.Ticks
            // both as Int64 values

            // read the DateTime ticks
            DateTimeOffset val = default(DateTimeOffset);
            FileStreamReadResult<long> dateTimeTicks = ReadInt64(offset);
            int totalLength = dateTimeTicks.TotalLength;
            if (dateTimeTicks.TotalLength > 0 && !dateTimeTicks.IsNull)
            {
                // read the TimeSpan ticks
                FileStreamReadResult<long> timeSpanTicks = ReadInt64(offset + dateTimeTicks.TotalLength);
                Debug.Assert(!timeSpanTicks.IsNull, "TimeSpan ticks cannot be null if DateTime ticks are not null!");

                totalLength += timeSpanTicks.TotalLength;
                
                // build the DateTimeOffset
                val = new DateTimeOffset(new DateTime(dateTimeTicks.Value), new TimeSpan(timeSpanTicks.Value));
            }
            return new FileStreamReadResult<DateTimeOffset>(val, totalLength, dateTimeTicks.IsNull);
        }

        /// <summary>
        /// Reads a TimeSpan from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the TimeSpan from</param>
        /// <returns>A TimeSpan</returns>
        public FileStreamReadResult<TimeSpan> ReadTimeSpan(long offset)
        {
            FileStreamReadResult<long> timeSpanTicks = ReadInt64(offset);
            TimeSpan val = default(TimeSpan);
            if (!timeSpanTicks.IsNull)
            {
                val = new TimeSpan(timeSpanTicks.Value);
            }
            return new FileStreamReadResult<TimeSpan>(val, timeSpanTicks.TotalLength, timeSpanTicks.IsNull);
        }

        /// <summary>
        /// Reads a string from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the string from</param>
        /// <returns>A string</returns>
        public FileStreamReadResult<string> ReadString(long offset)
        {
            LengthResult fieldLength = ReadLength(offset);
            Debug.Assert(fieldLength.ValueLength%2 == 0, "Invalid data length");

            if (fieldLength.ValueLength == 0) // there is no data
            {
                // If the total length is 5 (5 bytes for length, 0 for value), then the string is empty
                // Otherwise, the string is null
                bool isNull = fieldLength.TotalLength != 5;
                return new FileStreamReadResult<string>(isNull ? null : string.Empty,
                    fieldLength.TotalLength, isNull);
            }

            // positive length
            AssureBufferLength(fieldLength.ValueLength);
            fileStream.ReadData(buffer, fieldLength.ValueLength);
            return new FileStreamReadResult<string>(Encoding.Unicode.GetString(buffer, 0, fieldLength.ValueLength), fieldLength.TotalLength, false);
        }

        /// <summary>
        /// Reads bytes from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the bytes from</param>
        /// <returns>A byte array</returns>
        public FileStreamReadResult<byte[]> ReadBytes(long offset)
        {
            LengthResult fieldLength = ReadLength(offset);

            if (fieldLength.ValueLength == 0)
            {
                // If the total length is 5 (5 bytes for length, 0 for value), then the byte array is 0x
                // Otherwise, the byte array is null
                bool isNull = fieldLength.TotalLength != 5;
                return new FileStreamReadResult<byte[]>(isNull ? null : new byte[0],
                    fieldLength.TotalLength, isNull);
            }

            // positive length
            byte[] val = new byte[fieldLength.ValueLength];
            fileStream.ReadData(val, fieldLength.ValueLength);
            return new FileStreamReadResult<byte[]>(val, fieldLength.TotalLength, false);
        }

        /// <summary>
        /// Reads the length of a field at the specified offset in the file
        /// </summary>
        /// <param name="offset">Offset into the file to read the field length from</param>
        /// <returns>A LengthResult</returns>
        internal LengthResult ReadLength(long offset)
        {
            // read in length information
            int lengthValue;
            int lengthLength = fileStream.ReadData(buffer, 1, offset);
            if (buffer[0] != 0xFF)
            {
                // one byte is enough
                lengthValue = Convert.ToInt32(buffer[0]);
            }
            else
            {
                // read in next 4 bytes
                lengthLength += fileStream.ReadData(buffer, 4);

                // reconstruct the length
                lengthValue = BitConverter.ToInt32(buffer, 0);
            }

            return new LengthResult {LengthLength = lengthLength, ValueLength = lengthValue};
        }

        #endregion

        /// <summary>
        /// Internal struct used for representing the length of a field from the file
        /// </summary>
        internal struct LengthResult
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
            public int TotalLength
            {
                get { return LengthLength + ValueLength; }
            }
        }

        /// <summary>
        /// Creates a new buffer that is of the specified length if the buffer is not already
        /// at least as long as specified.
        /// </summary>
        /// <param name="newBufferLength">The minimum buffer size</param>
        private void AssureBufferLength(int newBufferLength)
        {
            if (buffer.Length < newBufferLength)
            {
                buffer = new byte[newBufferLength];
            }
        }

        #region IDisposable Implementation

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                fileStream.Dispose();
            }

            disposed = true;
        }

        ~ServiceBufferFileStreamReader()
        {
            Dispose(false);
        }

        #endregion

    }
}
