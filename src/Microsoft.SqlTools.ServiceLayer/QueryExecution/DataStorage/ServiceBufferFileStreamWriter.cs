using System;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class ServiceBufferFileStreamWriter : IFileStreamWriter
    {
        #region Properties

        public const int DefaultBufferLength = 8192;
        
        private int MaxCharsToStore { get; set; }
        private int MaxXmlCharsToStore { get; set; }

        private byte[] byteBuffer;
        private IFileStreamWrapper FileStream { get; set; }
        private readonly short[] shortBuffer;
        private readonly int[] intBuffer;
        private readonly long[] longBuffer;
        private readonly char[] charBuffer;
        private readonly double[] doubleBuffer;
        private readonly float[] floatBuffer;

        #endregion

        public ServiceBufferFileStreamWriter(IFileStreamWrapper fileWrapper, string fileName, int maxCharsToStore, int maxXmlCharsToStore)
        {
            // open file for reading/writing
            FileStream = fileWrapper;
            FileStream.Init(fileName, DefaultBufferLength);

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

            // Store max chars to store
            MaxCharsToStore = maxCharsToStore;
            MaxXmlCharsToStore = maxXmlCharsToStore;
        }

        #region IFileStreamWriter Implementation

        public async Task<int> WriteRow(StorageDataReader reader, DbColumnWrapper[] columns)
        {
            // Determine if we have any long fields
            bool hasLongFields = columns.Any(column => column.IsLong.HasValue && column.IsLong.Value);

            object[] values = new object[columns.Length];
            int rowBytes = 0;
            if (!hasLongFields)
            {
                // get all record values in one shot if there are no extra long fields
                reader.GetValues(values);
            }

            // Loop over all the columns and write the values to the temp file
            for (int i = 0; i < columns.Length; i++)
            {
                if (hasLongFields)
                {
                    if (reader.IsDBNull(i))
                    {
                        // Need special case for DBNull because
                        // reader.GetValue doesn't return DBNull in case of SqlXml and CLR type
                        values[i] = DBNull.Value;
                    }
                    else
                    {
                        DbColumnWrapper ci = columns[i];
                        if (ci.IsLongField)
                        {
                            // not a long field 
                            values[i] = reader.GetValue(i);
                        }
                        else
                        {
                            // this is a long field
                            if (ci.IsBytes)
                            {
                                values[i] = reader.GetBytesWithMaxCapacity(i, MaxCharsToStore);
                            }
                            else if (ci.IsChars)
                            {
                                Debug.Assert(MaxCharsToStore > 0);
                                values[i] = reader.GetCharsWithMaxCapacity(i,
                                    ci.IsXml ? MaxXmlCharsToStore : MaxCharsToStore);
                            }
                            else if (ci.IsXml)
                            {
                                Debug.Assert(MaxXmlCharsToStore > 0);
                                values[i] = reader.GetXmlWithMaxCapacity(i, MaxXmlCharsToStore);
                            }
                            else
                            {
                                // we should never get here
                                Debug.Assert(false);
                            }
                        }
                    }
                }

                Type tVal = values[i].GetType(); // get true type of the object

                if (tVal == typeof(DBNull))
                {
                    rowBytes += await WriteNull();
                }
                else
                {
                    if (columns[i].IsSqlVariant)
                    {
                        // serialize type information as a string before the value
                        string val = tVal.ToString();
                        rowBytes += await WriteString(val);
                    }

                    if (tVal == typeof(string))
                    {
                        // String - most frequently used data type
                        string val = (string)values[i];
                        rowBytes += await WriteString(val);
                    }
                    else if (tVal == typeof(SqlString))
                    {
                        // SqlString
                        SqlString val = (SqlString)values[i];
                        if (val.IsNull)
                        {
                            rowBytes += await WriteNull();
                        }
                        else
                        {
                            rowBytes += await WriteString(val.Value);
                        }
                    }
                    else if (tVal == typeof(short))
                    {
                        // Int16
                        short val = (short)values[i];
                        rowBytes += await WriteInt16(val);
                    }
                    else if (tVal == typeof(SqlInt16))
                    {
                        // SqlInt16
                        SqlInt16 val = (SqlInt16)values[i];
                        if (val.IsNull)
                        {
                            rowBytes += await WriteNull();
                        }
                        else
                        {
                            rowBytes += await WriteInt16(val.Value);
                        }
                    }
                    else if (tVal == typeof(int))
                    {
                        // Int32
                        int val = (int)values[i];
                        rowBytes += await WriteInt32(val);
                    }
                    else if (tVal == typeof(SqlInt32))
                    {
                        // SqlInt32
                        SqlInt32 val = (SqlInt32)values[i];
                        if (val.IsNull)
                        {
                            rowBytes += await WriteNull();
                        }
                        else
                        {
                            rowBytes += await WriteInt32(val.Value);
                        }
                    }
                    else if (tVal == typeof(long))
                    {
                        // Int64
                        long val = (long)values[i];
                        rowBytes += await WriteInt64(val);
                    }
                    else if (tVal == typeof(SqlInt64))
                    {
                        // SqlInt64
                        SqlInt64 val = (SqlInt64)values[i];
                        if (val.IsNull)
                        {
                            rowBytes += await WriteNull();
                        }
                        else
                        {
                            rowBytes += await WriteInt64(val.Value);
                        }
                    }
                    else if (tVal == typeof(byte))
                    {
                        // Byte
                        byte val = (byte)values[i];
                        rowBytes += await WriteByte(val);
                    }
                    else if (tVal == typeof(SqlByte))
                    {
                        // SqlByte
                        SqlByte val = (SqlByte)values[i];
                        if (val.IsNull)
                        {
                            rowBytes += await WriteNull();
                        }
                        else
                        {
                            rowBytes += await WriteByte(val.Value);
                        }
                    }
                    else if (tVal == typeof(char))
                    {
                        // Char
                        char val = (char)values[i];
                        rowBytes += await WriteChar(val);
                    }
                    else if (tVal == typeof(bool))
                    {
                        // Boolean
                        bool val = (bool)values[i];
                        rowBytes += await WriteBoolean(val);
                    }
                    else if (tVal == typeof(SqlBoolean))
                    {
                        // SqlBoolean
                        SqlBoolean val = (SqlBoolean)values[i];
                        if (val.IsNull)
                        {
                            rowBytes += await WriteNull();
                        }
                        else
                        {
                            rowBytes += await WriteBoolean(val.Value);
                        }
                    }
                    else if (tVal == typeof(double))
                    {
                        // Double
                        double val = (double)values[i];
                        rowBytes += await WriteDouble(val);
                    }
                    else if (tVal == typeof(SqlDouble))
                    {
                        // SqlDouble
                        SqlDouble val = (SqlDouble)values[i];
                        if (val.IsNull)
                        {
                            rowBytes += await WriteNull();
                        }
                        else
                        {
                            rowBytes += await WriteDouble(val.Value);
                        }
                    }
                    else if (tVal == typeof(SqlSingle))
                    {
                        // SqlSingle
                        SqlSingle val = (SqlSingle)values[i];
                        if (val.IsNull)
                        {
                            rowBytes += await WriteNull();
                        }
                        else
                        {
                            rowBytes += await WriteSingle(val.Value);
                        }
                    }
                    else if (tVal == typeof(decimal))
                    {
                        // Decimal
                        decimal val = (decimal)values[i];
                        rowBytes += await WriteDecimal(val);
                    }
                    else if (tVal == typeof(SqlDecimal))
                    {
                        // SqlDecimal
                        SqlDecimal val = (SqlDecimal)values[i];
                        if (val.IsNull)
                        {
                            rowBytes += await WriteNull();
                        }
                        else
                        {
                            rowBytes += await WriteSqlDecimal(val);
                        }
                    }
                    else if (tVal == typeof(DateTime))
                    {
                        // DateTime
                        DateTime val = (DateTime)values[i];
                        rowBytes += await WriteDateTime(val);
                    }
                    else if (tVal == typeof(DateTimeOffset))
                    {
                        // DateTimeOffset
                        DateTimeOffset val = (DateTimeOffset)values[i];
                        rowBytes += await WriteDateTimeOffset(val);
                    }
                    else if (tVal == typeof(SqlDateTime))
                    {
                        // SqlDateTime
                        SqlDateTime val = (SqlDateTime)values[i];
                        if (val.IsNull)
                        {
                            rowBytes += await WriteNull();
                        }
                        else
                        {
                            rowBytes += await WriteDateTime(val.Value);
                        }
                    }
                    else if (tVal == typeof(TimeSpan))
                    {
                        // TimeSpan
                        TimeSpan val = (TimeSpan)values[i];
                        rowBytes += await WriteTimeSpan(val);
                    }
                    else if (tVal == typeof(byte[]))
                    {
                        // Bytes
                        byte[] val = (byte[])values[i];
                        rowBytes += await WriteBytes(val, val.Length);
                    }
                    else if (tVal == typeof(SqlBytes))
                    {
                        // SqlBytes
                        SqlBytes val = (SqlBytes)values[i];
                        if (val.IsNull)
                        {
                            rowBytes += await WriteNull();
                        }
                        else
                        {
                            rowBytes += await WriteBytes(val.Value, val.Value.Length);
                        }
                    }
                    else if (tVal == typeof(SqlBinary))
                    {
                        // SqlBinary
                        SqlBinary val = (SqlBinary)values[i];
                        if (val.IsNull)
                        {
                            rowBytes += await WriteNull();
                        }
                        else
                        {
                            rowBytes += await WriteBytes(val.Value, val.Value.Length);
                        }
                    }
                    else if (tVal == typeof(SqlGuid))
                    {
                        // SqlGuid
                        SqlGuid val = (SqlGuid)values[i];
                        if (val.IsNull)
                        {
                            rowBytes += await WriteNull();
                        }
                        else
                        {
                            byte[] bytesVal = val.ToByteArray();
                            rowBytes += await WriteBytes(bytesVal, bytesVal.Length);
                        }
                    }
                    else if (tVal == typeof(SqlMoney))
                    {
                        // SqlMoney
                        SqlMoney val = (SqlMoney)values[i];
                        if (val.IsNull)
                        {
                            rowBytes += await WriteNull();
                        }
                        else
                        {
                            rowBytes += await WriteDecimal(val.Value);
                        }
                    }
                    else
                    {
                        // treat everything else as string
                        string val = values[i].ToString();
                        rowBytes += await WriteString(val);
                    }
                }
            }

            // Flush the buffer after every row
            await FlushBuffer();
            return rowBytes;
        }

        // Null
        public async Task<int> WriteNull()
        {
            byteBuffer[0] = 0x00;
            return await FileStream.WriteData(byteBuffer, 1);
        }

        // Int16
        public async Task<int> WriteInt16(short val)
        {
            byteBuffer[0] = 0x02; // length
            shortBuffer[0] = val;
            Buffer.BlockCopy(shortBuffer, 0, byteBuffer, 1, 2);
            return await FileStream.WriteData(byteBuffer, 3);
        }

        // Int32
        public async Task<int> WriteInt32(int val)
        {
            byteBuffer[0] = 0x04; // length
            intBuffer[0] = val;
            Buffer.BlockCopy(intBuffer, 0, byteBuffer, 1, 4);
            return await FileStream.WriteData(byteBuffer, 5);
        }

        public async Task<int> WriteInt32(long offset, int val)
        {
            byteBuffer[0] = 0x04; // length
            intBuffer[0] = val;
            Buffer.BlockCopy(intBuffer, 0, byteBuffer, 1, 4);
            return await FileStream.WriteData(byteBuffer, 5, offset);
        }

        // Int64
        public async Task<int> WriteInt64(long val)
        {
            byteBuffer[0] = 0x08; // length
            longBuffer[0] = val;
            Buffer.BlockCopy(longBuffer, 0, byteBuffer, 1, 8);
            return await FileStream.WriteData(byteBuffer, 9);
        }

        // Char
        public async Task<int> WriteChar(char val)
        {
            byteBuffer[0] = 0x02; // length
            charBuffer[0] = val;
            Buffer.BlockCopy(charBuffer, 0, byteBuffer, 1, 2);
            return await FileStream.WriteData(byteBuffer, 3);
        }

        // Boolean
        public async Task<int> WriteBoolean(bool val)
        {
            byteBuffer[0] = 0x01; // length
            byteBuffer[1] = (byte) (val ? 0x01 : 0x00);
            return await FileStream.WriteData(byteBuffer, 2);
        }

        // Byte
        public async Task<int> WriteByte(byte val)
        {
            byteBuffer[0] = 0x01; // length
            byteBuffer[1] = val;
            return await FileStream.WriteData(byteBuffer, 2);
        }

        // Single
        public async Task<int> WriteSingle(float val)
        {
            byteBuffer[0] = 0x04; // length
            floatBuffer[0] = val;
            Buffer.BlockCopy(floatBuffer, 0, byteBuffer, 1, 4);
            return await FileStream.WriteData(byteBuffer, 5);
        }

        // Double
        public async Task<int> WriteDouble(double val)
        {
            byteBuffer[0] = 0x08; // length
            doubleBuffer[0] = val;
            Buffer.BlockCopy(doubleBuffer, 0, byteBuffer, 1, 8);
            return await FileStream.WriteData(byteBuffer, 9);
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
            iTotalLen += await FileStream.WriteData(byteBuffer, iLen);
            return iTotalLen; // len+data
        }

        // Decimal
        public async Task<int> WriteDecimal(decimal val)
        {
            int[] arrInt32 = decimal.GetBits(val);

            int iLen = arrInt32.Length * 4;
            int iTotalLen = await WriteLength(iLen); // length

            Buffer.BlockCopy(arrInt32, 0, byteBuffer, 0, iLen);
            iTotalLen += await FileStream.WriteData(byteBuffer, iLen);

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
                const int iLen = 5;

                AssureBufferLength(iLen);
                byteBuffer[0] = 0xFF;
                byteBuffer[1] = 0x00;
                byteBuffer[2] = 0x00;
                byteBuffer[3] = 0x00;
                byteBuffer[4] = 0x00;

                iTotalLen = await FileStream.WriteData(byteBuffer, 5);
            }
            else
            {
                // Convert to a unicode byte array
                byte[] bytes = Encoding.Unicode.GetBytes(sVal);

                // convert char array into byte array and write it out							
                iTotalLen = await WriteLength(bytes.Length);
                iTotalLen += await FileStream.WriteData(bytes, bytes.Length);
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

                iTotalLen = await FileStream.WriteData(byteBuffer, iLen);
            }
            else
            {
                iTotalLen = await WriteLength(iLen);
                iTotalLen += await FileStream.WriteData(bytesVal, iLen);
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
                return await FileStream.WriteData(byteBuffer, 1);
            }
            byteBuffer[0] = 0xFF;

            // convert int32 into array of bytes
            intBuffer[0] = iLen;
            Buffer.BlockCopy(intBuffer, 0, byteBuffer, 1, 4);
            return await FileStream.WriteData(byteBuffer, 5);
        }

        public Task FlushBuffer()
        {
            return FileStream.Flush();
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
                FileStream.Flush().Wait();
                FileStream.Dispose();
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
