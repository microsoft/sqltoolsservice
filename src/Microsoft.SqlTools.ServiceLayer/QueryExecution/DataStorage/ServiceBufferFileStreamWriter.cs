using System;
using System.Data.SqlTypes;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class ServiceBufferFileStreamWriter : IFileStreamWriter
    {
        #region Properties

        public const int DefaultBufferLength = 8192;

        private byte[] byteBuffer;
        private IFileStreamWrapper fileStream;
        private short[] shortBuffer;
        private int[] intBuffer;
        private long[] longBuffer;
        private char[] charBuffer;
        private double[] doubleBuffer;
        private float[] floatBuffer;

        #endregion

        public ServiceBufferFileStreamWriter(string fileName)
        {
            // open file for reading/writing
            fileStream = new FileStreamWrapper();
            fileStream.Init(fileName, DefaultBufferLength);

            // create internal buffer
            byteBuffer = new byte[DefaultBufferLength];

            // Create internal buffers for blockcopy of contents to byte array
            // Note: We create them now to avoid the overhead of creating a new array for every write call
            shortBuffer = new short[1];
            intBuffer = new int[1];
            longBuffer = new long[1];
            charBuffer = new char[1];
            doubleBuffer = new double[1];
            floatBuffer = new float[1];
        }

        #region IFileStreamWriter Implementation

        // Null
        public async Task<int> WriteNull()
        {
            byteBuffer[0] = 0x00;
            return await fileStream.WriteData(byteBuffer, 1);
        }

        // Int16
        public async Task<int> WriteInt16(short val)
        {
            byteBuffer[0] = 0x02; // length
            shortBuffer[0] = val;
            Buffer.BlockCopy(shortBuffer, 0, byteBuffer, 1, 2);
            return await fileStream.WriteData(byteBuffer, 3);
        }

        // Int32
        public async Task<int> WriteInt32(int val)
        {
            byteBuffer[0] = 0x04; // length
            intBuffer[0] = val;
            Buffer.BlockCopy(intBuffer, 0, byteBuffer, 1, 4);
            return await fileStream.WriteData(byteBuffer, 5);
        }

        public async Task<int> WriteInt32(long offset, int val)
        {
            byteBuffer[0] = 0x04; // length
            intBuffer[0] = val;
            Buffer.BlockCopy(intBuffer, 0, byteBuffer, 1, 4);
            return await fileStream.WriteData(byteBuffer, 5, offset);
        }

        // Int64
        public async Task<int> WriteInt64(long val)
        {
            byteBuffer[0] = 0x08; // length
            longBuffer[0] = val;
            Buffer.BlockCopy(longBuffer, 0, byteBuffer, 1, 8);
            return await fileStream.WriteData(byteBuffer, 9);
        }

        // Char
        public async Task<int> WriteChar(char val)
        {
            byteBuffer[0] = 0x02; // length
            charBuffer[0] = val;
            Buffer.BlockCopy(charBuffer, 0, byteBuffer, 1, 2);
            return await fileStream.WriteData(byteBuffer, 3);
        }

        // Boolean
        public async Task<int> WriteBoolean(bool val)
        {
            byteBuffer[0] = 0x01; // length
            if (val)
            {
                byteBuffer[1] = 0x01;
            }
            else
            {
                byteBuffer[1] = 0x00;
            }
            return await fileStream.WriteData(byteBuffer, 2);
        }

        // Byte
        public async Task<int> WriteByte(byte val)
        {
            byteBuffer[0] = 0x01; // length
            byteBuffer[1] = val;
            return await fileStream.WriteData(byteBuffer, 2);
        }

        // Single
        public async Task<int> WriteSingle(float val)
        {
            byteBuffer[0] = 0x04; // length
            floatBuffer[0] = val;
            Buffer.BlockCopy(floatBuffer, 0, byteBuffer, 1, 4);
            return await fileStream.WriteData(byteBuffer, 5);
        }

        // Double
        public async Task<int> WriteDouble(double val)
        {
            byteBuffer[0] = 0x08; // length
            doubleBuffer[0] = val;
            Buffer.BlockCopy(doubleBuffer, 0, byteBuffer, 1, 8);
            return await fileStream.WriteData(byteBuffer, 9);
        }

        // SqlDecimal
        public async Task<int> WriteSqlDecimal(SqlDecimal val)
        {
            int[] arrInt32 = val.Data;
            int iLen = 3 + (arrInt32.Length * 4);
            int iTotalLen = await WriteLength(iLen); // length

            // precision
            byteBuffer[0] = val.Precision;

            // scale
            byteBuffer[1] = val.Scale;

            // positive
            byteBuffer[2] = (byte)(val.IsPositive ? 0x01 : 0x00);

            // data value
            Buffer.BlockCopy(arrInt32, 0, byteBuffer, 3, iLen - 3);
            iTotalLen += await fileStream.WriteData(byteBuffer, iLen);
            return iTotalLen; // len+data
        }

        // Decimal
        public async Task<int> WriteDecimal(decimal val)
        {
            int[] arrInt32 = decimal.GetBits(val);

            int iLen = arrInt32.Length * 4;
            int iTotalLen = await WriteLength(iLen); // length

            Buffer.BlockCopy(arrInt32, 0, byteBuffer, 0, iLen);
            iTotalLen += await fileStream.WriteData(byteBuffer, iLen);

            return iTotalLen; // len+data
        }

        // DateTime
        public Task<int> WriteDateTime(DateTime dtVal)
        {
            return WriteInt64(dtVal.Ticks);
        }

        // DateTimeOffset
        public async Task<int> WriteDateTimeOffset(DateTimeOffset dtoVal)
        {
            // DateTimeOffset gets written as a DateTime + TimeOffset
            // both represented as 'Ticks' written as Int64's
            return (await WriteInt64(dtoVal.Ticks)) + (await WriteInt64(dtoVal.Offset.Ticks));
        }

        // TimeSpan
        public Task<int> WriteTimeSpan(TimeSpan timeSpan)
        {
            return WriteInt64(timeSpan.Ticks);
        }

        // String
        public async Task<int> WriteString(string sVal)
        {
            int iTotalLen;
            if (0 == sVal.Length) // special case of 0 length string
            {
                int iLen = 5;

                AssureBufferLength(iLen);
                byteBuffer[0] = 0xFF;
                byteBuffer[1] = 0x00;
                byteBuffer[2] = 0x00;
                byteBuffer[3] = 0x00;
                byteBuffer[4] = 0x00;

                iTotalLen = await fileStream.WriteData(byteBuffer, iLen);
            }
            else
            {
                int iLen = sVal.Length * 2; //writing UNICODE chars
                iTotalLen = await WriteLength(iLen);

                // convert char array into byte array and write it out							
                AssureBufferLength(iLen);
                Buffer.BlockCopy(sVal.ToCharArray(), 0, byteBuffer, 0, iLen);
                iTotalLen += await fileStream.WriteData(byteBuffer, iLen);
            }
            return iTotalLen; // len+data
        }

        // Bytes
        public async Task<int> WriteBytes(byte[] bytesVal, int iLen)
        {
            int iTotalLen;
            if (0 == iLen) // special case of 0 length byte array "0x"
            {
                iLen = 5;

                AssureBufferLength(iLen);
                byteBuffer[0] = 0xFF;
                byteBuffer[1] = 0x00;
                byteBuffer[2] = 0x00;
                byteBuffer[3] = 0x00;
                byteBuffer[4] = 0x00;

                iTotalLen = await fileStream.WriteData(byteBuffer, iLen);
            }
            else
            {
                iTotalLen = await WriteLength(iLen);
                iTotalLen += await fileStream.WriteData(bytesVal, iLen);
            }
            return iTotalLen; // len+data
        }

        internal async Task<int> WriteLength(int iLen)
        {
            if (iLen < 0xFF) // fits in one byte of memory
            {
                // only need to write one byte
                int iTmp = iLen & 0x000000FF;

                byteBuffer[0] = Convert.ToByte(iTmp);
                return await fileStream.WriteData(byteBuffer, 1);
            }
            byteBuffer[0] = 0xFF;

            // convert int32 into array of bytes
            intBuffer[0] = iLen;
            Buffer.BlockCopy(intBuffer, 0, byteBuffer, 1, 4);
            return await fileStream.WriteData(byteBuffer, 5);
        }

        public Task FlushBuffer()
        {
            return fileStream.Flush();
        }

        #endregion

        private void AssureBufferLength(int newBufferLength)
        {
            if (newBufferLength > byteBuffer.Length)
            {
                byteBuffer = new byte[byteBuffer.Length];
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
                fileStream.Flush().Wait();
                fileStream.Dispose();
            }

            disposed = true;
        }

        ~ServiceBufferFileStreamWriter()
        {
            Dispose(false);
        }

        #endregion
    }
}
