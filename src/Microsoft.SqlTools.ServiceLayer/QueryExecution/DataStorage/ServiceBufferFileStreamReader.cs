using System;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

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
                val = BitConverter.ToInt32(buffer, 0);
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
            return new FileStreamReadResult<string>(Encoding.Unicode.GetString(buffer), fieldLength.TotalLength, false);
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
                lengthValue = Convert.ToInt32(buffer);
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
