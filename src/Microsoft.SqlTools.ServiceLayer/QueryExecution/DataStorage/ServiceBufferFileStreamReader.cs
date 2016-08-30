using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class ServiceBufferFileStreamReader : IFileStreamReader
    {

        private const int DefaultBufferSize = 8192;

        private byte[] buffer;

        private FileStreamWrapper FileStream { get; set; }

        public ServiceBufferFileStreamReader(string fileName)
        {
            // Open file for reading/writing
            FileStream = new FileStreamWrapper();
            FileStream.Init(fileName, DefaultBufferSize);

            // Create internal buffer
            buffer = new byte[DefaultBufferSize];
        }

        #region IFileStreamStorage Implementation

        public async Task<object[]> ReadRow(long fileOffset, IEnumerable<DbColumnWrapper> columns)
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
                    FileStreamReadResult<string> sqlVariantTypeResult = await ReadString(currentFileOffset, false);
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
                    FileStreamReadResult<string> result = await ReadString(currentFileOffset, false);
                    currentFileOffset += result.TotalLength;
                    results.Add(result.IsNull ? null : result.Value);
                }
                else if (colType == typeof(SqlString))
                {
                    // SqlString
                    FileStreamReadResult<string> result = await ReadString(currentFileOffset, false);
                    currentFileOffset += result.TotalLength;
                    results.Add(result.IsNull ? null : (SqlString) result.Value);
                }
                else if (colType == typeof(short))
                {
                    // Int16
                    FileStreamReadResult<short> result = await ReadInt16(currentFileOffset, false);
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
                    FileStreamReadResult<short> result = await ReadInt16(currentFileOffset, false);
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
                    FileStreamReadResult<int> result = await ReadInt32(currentFileOffset, false);
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
                    FileStreamReadResult<int> result = await ReadInt32(currentFileOffset, false);
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
                    FileStreamReadResult<long> result = await ReadInt64(currentFileOffset, false);
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
                    FileStreamReadResult<long> result = await ReadInt64(currentFileOffset, false);
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
                    FileStreamReadResult<byte> result = await ReadByte(currentFileOffset, false);
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
                    FileStreamReadResult<byte> result = await ReadByte(currentFileOffset, false);
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
                    FileStreamReadResult<char> result = await ReadChar(currentFileOffset, false);
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
                    FileStreamReadResult<bool> result = await ReadBoolean(currentFileOffset, false);
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
                    FileStreamReadResult<bool> result = await ReadBoolean(currentFileOffset, false);
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
                    FileStreamReadResult<double> result = await ReadDouble(currentFileOffset, false);
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
                    FileStreamReadResult<double> result = await ReadDouble(currentFileOffset, false);
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
                    FileStreamReadResult<float> result = await ReadSingle(currentFileOffset, false);
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
                    FileStreamReadResult<float> result = await ReadSingle(currentFileOffset, false);
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
                    FileStreamReadResult<decimal> result = await ReadDecimal(currentFileOffset, false);
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
                    FileStreamReadResult<SqlDecimal> result = await ReadSqlDecimal(currentFileOffset, false);
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
                    FileStreamReadResult<DateTime> result = await ReadDateTime(currentFileOffset, false);
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
                    FileStreamReadResult<DateTime> result = await ReadDateTime(currentFileOffset, false);
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
                    FileStreamReadResult<DateTimeOffset> result = await ReadDateTimeOffset(currentFileOffset, false);
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
                    FileStreamReadResult<TimeSpan> result = await ReadTimeSpan(currentFileOffset, false);
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
                    FileStreamReadResult<byte[]> result = await ReadBytes(currentFileOffset, false);
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
                    FileStreamReadResult<byte[]> result = await ReadBytes(currentFileOffset, false);
                    currentFileOffset += result.TotalLength;
                    results.Add(result.IsNull ? null : new SqlBytes(result.Value));
                }
                else if (colType == typeof(SqlBinary))
                {
                    // SqlBinary
                    FileStreamReadResult<byte[]> result = await ReadBytes(currentFileOffset, false);
                    currentFileOffset += result.TotalLength;
                    results.Add(result.IsNull ? null : new SqlBinary(result.Value));
                }
                else if (colType == typeof(SqlGuid))
                {
                    // SqlGuid
                    FileStreamReadResult<byte[]> result = await ReadBytes(currentFileOffset, false);
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
                    FileStreamReadResult<decimal> result = await ReadDecimal(currentFileOffset, false);
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
                    FileStreamReadResult<string> result = await ReadString(currentFileOffset, false);
                    currentFileOffset += result.TotalLength;
                    results.Add(result.IsNull ? null : result.Value);
                }
            }

            return results.ToArray();
        }

        public async Task<FileStreamReadResult<short>> ReadInt16(long fileOffset, bool skipValue)
        {

            LengthResult length = await ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 2, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            short val = default(short);
            if (!skipValue && !isNull)
            {
                await FileStream.ReadData(buffer, length.ValueLength);
                val = BitConverter.ToInt16(buffer, 0);
            }

            return new FileStreamReadResult<short>(val, length.TotalLength, isNull);
        }

        public async Task<FileStreamReadResult<int>> ReadInt32(long fileOffset, bool skipValue)
        {
            LengthResult length = await ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 4, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            int val = default(int);
            if (!skipValue && !isNull)
            {
                await FileStream.ReadData(buffer, length.ValueLength);
                val = BitConverter.ToInt32(buffer, 0);
            }
            return new FileStreamReadResult<int>(val, length.TotalLength, isNull);
        }

        public async Task<FileStreamReadResult<long>> ReadInt64(long fileOffset, bool skipValue)
        {
            LengthResult length = await ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 8, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            long val = default(long);
            if (!skipValue && !isNull)
            {
                await FileStream.ReadData(buffer, length.ValueLength);
                val = BitConverter.ToInt64(buffer, 0);
            }
            return new FileStreamReadResult<long>(val, length.TotalLength, isNull);
        }

        public async Task<FileStreamReadResult<byte>> ReadByte(long fileOffset, bool skipValue)
        {
            LengthResult length = await ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 1, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            byte val = default(byte);
            if (!skipValue && !isNull)
            {
                await FileStream.ReadData(buffer, length.ValueLength);
                val = buffer[0];
            }
            return new FileStreamReadResult<byte>(val, length.TotalLength, isNull);
        }

        public async Task<FileStreamReadResult<char>> ReadChar(long fileOffset, bool skipValue)
        {
            LengthResult length = await ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 2, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            char val = default(char);
            if (!skipValue && !isNull)
            {
                await FileStream.ReadData(buffer, length.ValueLength);
                val = BitConverter.ToChar(buffer, 0);
            }
            return new FileStreamReadResult<char>(val, length.TotalLength, isNull);
        }

        public async Task<FileStreamReadResult<bool>> ReadBoolean(long fileOffset, bool skipValue)
        {
            LengthResult length = await ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 1, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            bool val = default(bool);
            if (!skipValue && !isNull)
            {
                await FileStream.ReadData(buffer, length.ValueLength);
                val = buffer[0] == 0x01;
            }
            return new FileStreamReadResult<bool>(val, length.TotalLength, isNull);
        }

        public async Task<FileStreamReadResult<float>> ReadSingle(long fileOffset, bool skipValue)
        {
            LengthResult length = await ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 4, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            float val = default(float);
            if (!skipValue && !isNull)
            {
                await FileStream.ReadData(buffer, length.ValueLength);
                val = BitConverter.ToSingle(buffer, 0);
            }
            return new FileStreamReadResult<float>(val, length.TotalLength, isNull);
        }

        public async Task<FileStreamReadResult<double>> ReadDouble(long fileOffset, bool skipValue)
        {
            LengthResult length = await ReadLength(fileOffset);
            Debug.Assert(length.ValueLength == 0 || length.ValueLength == 8, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            double val = default(double);
            if (!skipValue && !isNull)
            {
                await FileStream.ReadData(buffer, length.ValueLength);
                val = BitConverter.ToSingle(buffer, 0);
            }
            return new FileStreamReadResult<double>(val, length.TotalLength, isNull);
        }

        public async Task<FileStreamReadResult<SqlDecimal>> ReadSqlDecimal(long offset, bool skipValue)
        {
            LengthResult length = await ReadLength(offset);
            Debug.Assert(length.ValueLength == 0 || (length.ValueLength - 3)%4 == 0,
                string.Format("Invalid data length: {0}", length.ValueLength));

            bool isNull = length.ValueLength == 0;
            SqlDecimal val = default(SqlDecimal);
            if (!skipValue && !isNull)
            {
                await FileStream.ReadData(buffer, length.ValueLength);

                int[] arrInt32 = new int[(length.ValueLength - 3)/4];
                Buffer.BlockCopy(buffer, 3, arrInt32, 0, length.ValueLength - 3);
                val = new SqlDecimal(buffer[0], buffer[1], 1 == buffer[2], arrInt32);
            }
            return new FileStreamReadResult<SqlDecimal>(val, length.TotalLength, isNull);
        }

        public async Task<FileStreamReadResult<decimal>> ReadDecimal(long offset, bool skipValue)
        {
            LengthResult length = await ReadLength(offset);
            Debug.Assert(length.ValueLength%4 == 0, "Invalid data length");

            bool isNull = length.ValueLength == 0;
            decimal val = default(decimal);
            if (!skipValue && !isNull) // value is not needed or is a null
            {
                await FileStream.ReadData(buffer, length.ValueLength);

                int[] arrInt32 = new int[length.ValueLength/4];
                Buffer.BlockCopy(buffer, 0, arrInt32, 0, length.ValueLength);
                val = new decimal(arrInt32);
            }
            return new FileStreamReadResult<decimal>(val, length.TotalLength, isNull);
        }

        public async Task<FileStreamReadResult<DateTime>> ReadDateTime(long offset, bool skipValue)
        {
            FileStreamReadResult<long> ticks = await ReadInt64(offset, skipValue);
            DateTime val = default(DateTime);
            if (!skipValue && !ticks.IsNull)
            {
                val = new DateTime(ticks.Value);
            }
            return new FileStreamReadResult<DateTime>(val, ticks.TotalLength, ticks.IsNull);
        }

        public async Task<FileStreamReadResult<DateTimeOffset>> ReadDateTimeOffset(long offset, bool skipValue)
        {
            // DateTimeOffset is represented by DateTime.Ticks followed by TimeSpan.Ticks
            // both as Int64 values

            // read the DateTime ticks
            DateTimeOffset val = default(DateTimeOffset);
            FileStreamReadResult<long> dateTimeTicks = await ReadInt64(offset, skipValue);
            int totalLength = dateTimeTicks.TotalLength;
            if (dateTimeTicks.TotalLength > 0 && !dateTimeTicks.IsNull)
            {
                // read the TimeSpan ticks
                FileStreamReadResult<long> timeSpanTicks =
                    await ReadInt64(offset + dateTimeTicks.TotalLength, skipValue);
                Debug.Assert(!timeSpanTicks.IsNull, "TimeSpan ticks cannot be null if DateTime ticks are not null!");

                totalLength += timeSpanTicks.TotalLength;
                if (!skipValue)
                {
                    // build the DateTimeOffset
                    val = new DateTimeOffset(new DateTime(dateTimeTicks.Value), new TimeSpan(timeSpanTicks.Value));
                }
            }
            return new FileStreamReadResult<DateTimeOffset>(val, totalLength, dateTimeTicks.IsNull);
        }

        public async Task<FileStreamReadResult<TimeSpan>> ReadTimeSpan(long offset, bool skipValue)
        {
            FileStreamReadResult<long> timeSpanTicks = await ReadInt64(offset, skipValue);
            TimeSpan val = default(TimeSpan);
            if (!skipValue && !timeSpanTicks.IsNull)
            {
                val = new TimeSpan(timeSpanTicks.Value);
            }
            return new FileStreamReadResult<TimeSpan>(val, timeSpanTicks.TotalLength, timeSpanTicks.IsNull);
        }

        public async Task<FileStreamReadResult<string>> ReadString(long offset, bool skipValue)
        {
            LengthResult fieldLength = await ReadLength(offset);
            Debug.Assert(fieldLength.ValueLength%2 == 0, "Invalid data length");

            if (skipValue)
            {
                // value is not needed, only length of the field
                return new FileStreamReadResult<string>(null, fieldLength.TotalLength, false);
            }

            if (fieldLength.ValueLength == 0) // there is no data
            {
                // If the total length is 5 (5 bytes for length, 0 for value), then the string is empty
                // Otherwise, the string is null
                bool isNull = fieldLength.TotalLength == 5;
                return new FileStreamReadResult<string>(isNull ? null : string.Empty,
                    fieldLength.TotalLength, isNull);
            }

            // positive length
            AssureBufferLength(fieldLength.ValueLength);
            await FileStream.ReadData(buffer, fieldLength.ValueLength);
            return new FileStreamReadResult<string>(Encoding.Unicode.GetString(buffer, 0, fieldLength.ValueLength), fieldLength.TotalLength, false);
        }

        public async Task<FileStreamReadResult<byte[]>> ReadBytes(long offset, bool skipValue)
        {
            LengthResult fieldLength = await ReadLength(offset);
            if (skipValue) // value is not needed, only length of the field
            {
                return new FileStreamReadResult<byte[]>(null, fieldLength.TotalLength, false);
            }

            if (fieldLength.ValueLength == 0)
            {
                // there is no data
                // If the total length is 5 (5 bytes for length, 0 for value), then the byte array is 0x
                // Otherwise, the byte array is null
                bool isNull = fieldLength.TotalLength == 5;
                return new FileStreamReadResult<byte[]>(isNull ? null : new byte[0],
                    fieldLength.TotalLength, isNull);
            }

            // positive length
            byte[] val = new byte[fieldLength.ValueLength];
            await FileStream.ReadData(val, fieldLength.ValueLength);
            return new FileStreamReadResult<byte[]>(val, fieldLength.TotalLength, false);
        }

        internal async Task<LengthResult> ReadLength(long offset)
        {
            // read in length information
            int lengthValue;
            int lengthLength = await FileStream.ReadData(buffer, 1, offset);
            if (buffer[0] != 0xFF)
            {
                // one byte is enough
                lengthValue = Convert.ToInt32(buffer[0]);
            }
            else
            {
                // read in next 4 bytes
                lengthLength += await FileStream.ReadData(buffer, 4);

                // reconstruct the length
                lengthValue = BitConverter.ToInt32(buffer, 0);
            }

            return new LengthResult {LengthLength = lengthLength, ValueLength = lengthValue};
        }

        #endregion

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
                FileStream.Dispose();
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
