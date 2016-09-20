//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Writer for service buffer formatted file streams
    /// </summary>
    public class ServiceBufferFileStreamWriter : IFileStreamWriter
    {
        #region Properties

        public const int DefaultBufferLength = 8192;
        
        private int MaxCharsToStore { get; set; }
        private int MaxXmlCharsToStore { get; set; }

        private IFileStreamWrapper FileStream { get; set; }
        private byte[] byteBuffer;
        private readonly short[] shortBuffer;
        private readonly int[] intBuffer;
        private readonly long[] longBuffer;
        private readonly char[] charBuffer;
        private readonly double[] doubleBuffer;
        private readonly float[] floatBuffer;

        #endregion

        /// <summary>
        /// Constructs a new writer
        /// </summary>
        /// <param name="fileWrapper">The file wrapper to use as the underlying file stream</param>
        /// <param name="fileName">Name of the file to write to</param>
        /// <param name="maxCharsToStore">Maximum number of characters to store for long text fields</param>
        /// <param name="maxXmlCharsToStore">Maximum number of characters to store for XML fields</param>
        public ServiceBufferFileStreamWriter(IFileStreamWrapper fileWrapper, string fileName, int maxCharsToStore, int maxXmlCharsToStore)
        {
            // open file for reading/writing
            FileStream = fileWrapper;
            FileStream.Init(fileName, DefaultBufferLength, FileAccess.ReadWrite);

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

            writeMethods = new Dictionary<Type, Func<object, int>>
            {
                {typeof(string), val => WriteString((string) val)},
                {typeof(short), val => WriteInt16((short) val)},
                {typeof(int), val => WriteInt32((int) val)},
                {typeof(long), val => WriteInt64((long) val)},
                {typeof(byte), val => WriteByte((byte) val)},
                {typeof(char), val => WriteChar((char) val)},
                {typeof(bool), val => WriteBoolean((bool) val)},
                {typeof(double), val => WriteDouble((double) val) },
                {typeof(float), val => WriteSingle((float) val) },
                {typeof(decimal), val => WriteDecimal((decimal) val) },
                {typeof(DateTime), val => WriteDateTime((DateTime) val) },
                {typeof(DateTimeOffset), val => WriteDateTimeOffset((DateTimeOffset) val) },
                {typeof(TimeSpan), val => WriteTimeSpan((TimeSpan) val) },
                {typeof(byte[]), val => WriteBytes((byte[]) val)},

                {typeof(SqlString), val => WriteNullable((SqlString) val, obj => WriteString((string) obj))},
                {typeof(SqlInt16), val => WriteNullable((SqlInt16) val, obj => WriteInt16((short) obj))},
                {typeof(SqlInt32), val => WriteNullable((SqlInt32) val, obj => WriteInt32((int) obj))},
                {typeof(SqlInt64), val => WriteNullable((SqlInt64) val, obj => WriteInt64((long) obj)) },
                {typeof(SqlByte), val => WriteNullable((SqlByte) val, obj => WriteByte((byte) obj)) },
                {typeof(SqlBoolean), val => WriteNullable((SqlBoolean) val, obj => WriteBoolean((bool) obj)) },
                {typeof(SqlDouble), val => WriteNullable((SqlDouble) val, obj => WriteDouble((double) obj)) },
                {typeof(SqlSingle), val => WriteNullable((SqlSingle) val, obj => WriteSingle((float) obj)) },
                {typeof(SqlDecimal), val => WriteNullable((SqlDecimal) val, obj => WriteSqlDecimal((SqlDecimal) obj)) },
                {typeof(SqlDateTime), val => WriteNullable((SqlDateTime) val, obj => WriteDateTime((DateTime) obj)) },
                {typeof(SqlBytes), val => WriteNullable((SqlBytes) val, obj => WriteBytes((byte[]) obj)) },
                {typeof(SqlBinary), val => WriteNullable((SqlBinary) val, obj => WriteBytes((byte[]) obj)) },
                {typeof(SqlGuid), val => WriteNullable((SqlGuid) val, obj => WriteGuid((Guid) obj)) },
                {typeof(SqlMoney), val => WriteNullable((SqlMoney) val, obj => WriteMoney((SqlMoney) obj)) }
            };
        }

        private readonly Dictionary<Type, Func<object, int>> writeMethods;

        #region IFileStreamWriter Implementation

        /// <summary>
        /// Writes an entire row to the file stream
        /// </summary>
        /// <param name="reader">A primed reader</param>
        /// <returns>Number of bytes used to write the row</returns>
        public int WriteRow(StorageDataReader reader)
        {
            // Determine if we have any long fields
            bool hasLongFields = reader.Columns.Any(column => column.IsLong);

            object[] values = new object[reader.Columns.Length];
            int rowBytes = 0;
            if (!hasLongFields)
            {
                // get all record values in one shot if there are no extra long fields
                reader.GetValues(values);
            }

            // Loop over all the columns and write the values to the temp file
            for (int i = 0; i < reader.Columns.Length; i++)
            {
                DbColumnWrapper ci = reader.Columns[i];
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
                        if (!ci.IsLong)
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
                    rowBytes += WriteNull();
                }
                else
                {
                    if (ci.IsSqlVariant)
                    {
                        // serialize type information as a string before the value
                        string val = tVal.ToString();
                        rowBytes += WriteString(val);
                    }

                    Func<object, int> writeMethod;
                    if (writeMethods.TryGetValue(tVal, out writeMethod))
                    {
                        rowBytes += writeMethod(values[i]);
                    }
                    else
                    {
                        rowBytes += WriteString(values[i].ToString());
                    }
                }
            }

            // Flush the buffer after every row
            FlushBuffer();
            return rowBytes;
        }

        /// <summary>
        /// Writes null to the file as one 0x00 byte
        /// </summary>
        /// <returns>Number of bytes used to store the null</returns>
        public int WriteNull()
        {
            byteBuffer[0] = 0x00;
            return FileStream.WriteData(byteBuffer, 1);
        }

        /// <summary>
        /// Writes a short to the file
        /// </summary>
        /// <returns>Number of bytes used to store the short</returns>
        public int WriteInt16(short val)
        {
            byteBuffer[0] = 0x02; // length
            shortBuffer[0] = val;
            Buffer.BlockCopy(shortBuffer, 0, byteBuffer, 1, 2);
            return FileStream.WriteData(byteBuffer, 3);
        }

        /// <summary>
        /// Writes a int to the file
        /// </summary>
        /// <returns>Number of bytes used to store the int</returns>
        public int WriteInt32(int val)
        {
            byteBuffer[0] = 0x04; // length
            intBuffer[0] = val;
            Buffer.BlockCopy(intBuffer, 0, byteBuffer, 1, 4);
            return FileStream.WriteData(byteBuffer, 5);
        }

        /// <summary>
        /// Writes a long to the file
        /// </summary>
        /// <returns>Number of bytes used to store the long</returns>
        public int WriteInt64(long val)
        {
            byteBuffer[0] = 0x08; // length
            longBuffer[0] = val;
            Buffer.BlockCopy(longBuffer, 0, byteBuffer, 1, 8);
            return FileStream.WriteData(byteBuffer, 9);
        }

        /// <summary>
        /// Writes a char to the file
        /// </summary>
        /// <returns>Number of bytes used to store the char</returns>
        public int WriteChar(char val)
        {
            byteBuffer[0] = 0x02; // length
            charBuffer[0] = val;
            Buffer.BlockCopy(charBuffer, 0, byteBuffer, 1, 2);
            return FileStream.WriteData(byteBuffer, 3);
        }

        /// <summary>
        /// Writes a bool to the file
        /// </summary>
        /// <returns>Number of bytes used to store the bool</returns>
        public int WriteBoolean(bool val)
        {
            byteBuffer[0] = 0x01; // length
            byteBuffer[1] = (byte) (val ? 0x01 : 0x00);
            return FileStream.WriteData(byteBuffer, 2);
        }

        /// <summary>
        /// Writes a byte to the file
        /// </summary>
        /// <returns>Number of bytes used to store the byte</returns>
        public int WriteByte(byte val)
        {
            byteBuffer[0] = 0x01; // length
            byteBuffer[1] = val;
            return FileStream.WriteData(byteBuffer, 2);
        }

        /// <summary>
        /// Writes a float to the file
        /// </summary>
        /// <returns>Number of bytes used to store the float</returns>
        public int WriteSingle(float val)
        {
            byteBuffer[0] = 0x04; // length
            floatBuffer[0] = val;
            Buffer.BlockCopy(floatBuffer, 0, byteBuffer, 1, 4);
            return FileStream.WriteData(byteBuffer, 5);
        }

        /// <summary>
        /// Writes a double to the file
        /// </summary>
        /// <returns>Number of bytes used to store the double</returns>
        public int WriteDouble(double val)
        {
            byteBuffer[0] = 0x08; // length
            doubleBuffer[0] = val;
            Buffer.BlockCopy(doubleBuffer, 0, byteBuffer, 1, 8);
            return FileStream.WriteData(byteBuffer, 9);
        }

        /// <summary>
        /// Writes a SqlDecimal to the file
        /// </summary>
        /// <returns>Number of bytes used to store the SqlDecimal</returns>
        public int WriteSqlDecimal(SqlDecimal val)
        {
            int[] arrInt32 = val.Data;
            int iLen = 3 + (arrInt32.Length * 4);
            int iTotalLen = WriteLength(iLen); // length

            // precision
            byteBuffer[0] = val.Precision;

            // scale
            byteBuffer[1] = val.Scale;

            // positive
            byteBuffer[2] = (byte)(val.IsPositive ? 0x01 : 0x00);

            // data value
            Buffer.BlockCopy(arrInt32, 0, byteBuffer, 3, iLen - 3);
            iTotalLen += FileStream.WriteData(byteBuffer, iLen);
            return iTotalLen; // len+data
        }

        /// <summary>
        /// Writes a decimal to the file
        /// </summary>
        /// <returns>Number of bytes used to store the decimal</returns>
        public int WriteDecimal(decimal val)
        {
            int[] arrInt32 = decimal.GetBits(val);

            int iLen = arrInt32.Length * 4;
            int iTotalLen = WriteLength(iLen); // length

            Buffer.BlockCopy(arrInt32, 0, byteBuffer, 0, iLen);
            iTotalLen += FileStream.WriteData(byteBuffer, iLen);

            return iTotalLen; // len+data
        }

        /// <summary>
        /// Writes a DateTime to the file
        /// </summary>
        /// <returns>Number of bytes used to store the DateTime</returns>
        public int WriteDateTime(DateTime dtVal)
        {
            return WriteInt64(dtVal.Ticks);
        }

        /// <summary>
        /// Writes a DateTimeOffset to the file
        /// </summary>
        /// <returns>Number of bytes used to store the DateTimeOffset</returns>
        public int WriteDateTimeOffset(DateTimeOffset dtoVal)
        {
            // Write the length, which is the 2*sizeof(long)
            byteBuffer[0] = 0x10; // length (16)

            // Write the two longs, the datetime and the offset
            long[] longBufferOffset = new long[2];
            longBufferOffset[0] = dtoVal.Ticks;
            longBufferOffset[1] = dtoVal.Offset.Ticks;
            Buffer.BlockCopy(longBufferOffset, 0, byteBuffer, 1, 16);
            return FileStream.WriteData(byteBuffer, 17);
        }

        /// <summary>
        /// Writes a TimeSpan to the file
        /// </summary>
        /// <returns>Number of bytes used to store the TimeSpan</returns>
        public int WriteTimeSpan(TimeSpan timeSpan)
        {
            return WriteInt64(timeSpan.Ticks);
        }

        /// <summary>
        /// Writes a string to the file
        /// </summary>
        /// <returns>Number of bytes used to store the string</returns>
        public int WriteString(string sVal)
        {
            Validate.IsNotNull(nameof(sVal), sVal);

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

                iTotalLen = FileStream.WriteData(byteBuffer, 5);
            }
            else
            {
                // Convert to a unicode byte array
                byte[] bytes = Encoding.Unicode.GetBytes(sVal);

                // convert char array into byte array and write it out							
                iTotalLen = WriteLength(bytes.Length);
                iTotalLen += FileStream.WriteData(bytes, bytes.Length);
            }
            return iTotalLen; // len+data
        }

        /// <summary>
        /// Writes a byte[] to the file
        /// </summary>
        /// <returns>Number of bytes used to store the byte[]</returns>
        public int WriteBytes(byte[] bytesVal)
        {
            Validate.IsNotNull(nameof(bytesVal), bytesVal);

            int iTotalLen;
            if (bytesVal.Length == 0) // special case of 0 length byte array "0x"
            {
                AssureBufferLength(5);
                byteBuffer[0] = 0xFF;
                byteBuffer[1] = 0x00;
                byteBuffer[2] = 0x00;
                byteBuffer[3] = 0x00;
                byteBuffer[4] = 0x00;

                iTotalLen = FileStream.WriteData(byteBuffer, 5);
            }
            else
            {
                iTotalLen = WriteLength(bytesVal.Length);
                iTotalLen += FileStream.WriteData(bytesVal, bytesVal.Length);
            }
            return iTotalLen; // len+data
        }

        public int WriteGuid(Guid val)
        {
            byte[] guidBytes = val.ToByteArray();
            return WriteBytes(guidBytes);
        }

        public int WriteMoney(SqlMoney val)
        {
            return WriteDecimal(val.Value);
        }

        /// <summary>
        /// Writes the length of the field using the appropriate number of bytes (ie, 1 if the
        /// length is &lt;255, 5 if the length is &gt;=255)
        /// </summary>
        /// <returns>Number of bytes used to store the length</returns>
        private int WriteLength(int iLen)
        {
            if (iLen < 0xFF)
            {
                // fits in one byte of memory only need to write one byte
                int iTmp = iLen & 0x000000FF;

                byteBuffer[0] = Convert.ToByte(iTmp);
                return FileStream.WriteData(byteBuffer, 1);
            }
            // The length won't fit in 1 byte, so we need to use 1 byte to signify that the length
            // is a full 4 bytes.
            byteBuffer[0] = 0xFF;

            // convert int32 into array of bytes
            intBuffer[0] = iLen;
            Buffer.BlockCopy(intBuffer, 0, byteBuffer, 1, 4);
            return FileStream.WriteData(byteBuffer, 5);
        }

        private int WriteNullable(INullable val, Func<object, int> valueWriteFunc)
        {
            return val.IsNull ? WriteNull() : valueWriteFunc(val);
        }

        /// <summary>
        /// Flushes the internal buffer to the file stream
        /// </summary>
        public void FlushBuffer()
        {
            FileStream.Flush();
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
                FileStream.Flush();
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
