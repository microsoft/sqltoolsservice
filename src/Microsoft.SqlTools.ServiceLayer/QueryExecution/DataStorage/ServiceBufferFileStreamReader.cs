//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Reader for service buffer formatted file streams
    /// </summary>
    public class ServiceBufferFileStreamReader : IFileStreamReader
    {
        private const int DefaultBufferSize = 8192;

        #region Member Variables

        private byte[] buffer;

        private readonly IFileStreamWrapper fileStream;

        private Dictionary<Type, Func<long, FileStreamReadResult>> readMethods;

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
            fileStream.Init(fileName, DefaultBufferSize, FileAccess.Read);

            // Create internal buffer
            buffer = new byte[DefaultBufferSize];

            // Create the methods that will be used to read back
            readMethods = new Dictionary<Type, Func<long, FileStreamReadResult>>
            {
                {typeof(string), ReadString},
                {typeof(short), ReadInt16},
                {typeof(int), ReadInt32},
                {typeof(long), ReadInt64},
                {typeof(byte), ReadByte},
                {typeof(char), ReadChar},
                {typeof(bool), ReadBoolean},
                {typeof(double), ReadDouble},
                {typeof(float), ReadSingle},
                {typeof(decimal), ReadDecimal},
                {typeof(DateTime), ReadDateTime},
                {typeof(DateTimeOffset), ReadDateTimeOffset},
                {typeof(TimeSpan), ReadTimeSpan},
                {typeof(byte[]), ReadBytes},    // TODO: Figure out how to properly display byte[]

                {typeof(SqlString), ReadString},
                {typeof(SqlInt16), ReadInt16},
                {typeof(SqlInt32), ReadInt32},
                {typeof(SqlInt64), ReadInt64},
                {typeof(SqlByte), ReadByte},
                {typeof(SqlBoolean), ReadBoolean},
                {typeof(SqlDouble), ReadDouble},
                {typeof(SqlSingle), ReadSingle},
                {typeof(SqlDecimal), ReadSqlDecimal},
                {typeof(SqlDateTime), ReadDateTime},
                {typeof(SqlBytes), ReadBytes},
                {typeof(SqlBinary), ReadBytes},
                {typeof(SqlGuid), ReadGuid},
                {typeof(SqlMoney), ReadMoney},
            };
        }

        #region IFileStreamStorage Implementation

        /// <summary>
        /// Reads a row from the file, based on the columns provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file where the row starts</param>
        /// <param name="columns">The columns that were encoded</param>
        /// <returns>The objects from the row, ready for output to the client</returns>
        public DbCellValue[] ReadRow(long fileOffset, IEnumerable<DbColumnWrapper> columns)
        {
            // Initialize for the loop
            long currentFileOffset = fileOffset;
            List<DbCellValue> results = new List<DbCellValue>();

            // Iterate over the columns
            foreach (DbColumnWrapper column in columns)
            {
                // We will pivot based on the type of the column
                Type colType;
                if (column.IsSqlVariant)
                {
                    // For SQL Variant columns, the type is written first in string format
                    FileStreamReadResult sqlVariantTypeResult = ReadString(currentFileOffset);
                    currentFileOffset += sqlVariantTypeResult.TotalLength;
                    string sqlVariantType = (string)sqlVariantTypeResult.Value.RawObject;

                    // If the typename is null, then the whole value is null
                    if (sqlVariantTypeResult.Value == null)
                    {
                        results.Add(sqlVariantTypeResult.Value);
                        continue;
                    }

                    // The typename is stored in the string
                    colType = Type.GetType(sqlVariantType);

                    // Workaround .NET bug, see sqlbu# 440643 and vswhidbey# 599834
                    // TODO: Is this workaround necessary for .NET Core?
                    if (colType == null && sqlVariantType == "System.Data.SqlTypes.SqlSingle")
                    {
                        colType = typeof(SqlSingle);
                    }
                }
                else
                {
                    colType = column.DataType;
                }

                // Use the right read function for the type to read the data from the file
                Func<long, FileStreamReadResult> readFunc;
                if(!readMethods.TryGetValue(colType, out readFunc))
                {
                    // Treat everything else as a string
                    readFunc = ReadString;
                } 
                FileStreamReadResult result = readFunc(currentFileOffset);
                currentFileOffset += result.TotalLength;
                results.Add(result.Value);
            }

            return results.ToArray();
        }

        /// <summary>
        /// Reads a short from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the short from</param>
        /// <returns>A short</returns>
        public FileStreamReadResult ReadInt16(long fileOffset)
        {
            return ReadCellHelper(fileOffset, length => BitConverter.ToInt16(buffer, 0));
        }

        /// <summary>
        /// Reads a int from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the int from</param>
        /// <returns>An int</returns>
        public FileStreamReadResult ReadInt32(long fileOffset)
        {
            return ReadCellHelper(fileOffset, length => BitConverter.ToInt32(buffer, 0));
        }

        /// <summary>
        /// Reads a long from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the long from</param>
        /// <returns>A long</returns>
        public FileStreamReadResult ReadInt64(long fileOffset)
        {
            return ReadCellHelper(fileOffset, length => BitConverter.ToInt64(buffer, 0));
        }

        /// <summary>
        /// Reads a byte from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the byte from</param>
        /// <returns>A byte</returns>
        public FileStreamReadResult ReadByte(long fileOffset)
        {
            return ReadCellHelper(fileOffset, length => buffer[0]);
        }

        /// <summary>
        /// Reads a char from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the char from</param>
        /// <returns>A char</returns>
        public FileStreamReadResult ReadChar(long fileOffset)
        {
            return ReadCellHelper(fileOffset, length => BitConverter.ToChar(buffer, 0));
        }

        /// <summary>
        /// Reads a bool from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the bool from</param>
        /// <returns>A bool</returns>
        public FileStreamReadResult ReadBoolean(long fileOffset)
        {
            return ReadCellHelper(fileOffset, length => buffer[0] == 0x1);
        }

        /// <summary>
        /// Reads a single from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the single from</param>
        /// <returns>A single</returns>
        public FileStreamReadResult ReadSingle(long fileOffset)
        {
            return ReadCellHelper(fileOffset, length => BitConverter.ToSingle(buffer, 0));
        }

        /// <summary>
        /// Reads a double from the file at the offset provided
        /// </summary>
        /// <param name="fileOffset">Offset into the file to read the double from</param>
        /// <returns>A double</returns>
        public FileStreamReadResult ReadDouble(long fileOffset)
        {
            return ReadCellHelper(fileOffset, length => BitConverter.ToDouble(buffer, 0));
        }

        /// <summary>
        /// Reads a SqlDecimal from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the SqlDecimal from</param>
        /// <returns>A SqlDecimal</returns>
        public FileStreamReadResult ReadSqlDecimal(long offset)
        {
            return ReadCellHelper(offset, length =>
            {
                int[] arrInt32 = new int[(length - 3) / 4];
                Buffer.BlockCopy(buffer, 3, arrInt32, 0, length - 3);
                return new SqlDecimal(buffer[0], buffer[1], buffer[2] == 1, arrInt32);
            });
        }

        /// <summary>
        /// Reads a decimal from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the decimal from</param>
        /// <returns>A decimal</returns>
        public FileStreamReadResult ReadDecimal(long offset)
        {
            return ReadCellHelper(offset, length =>
            {
                int[] arrInt32 = new int[length / 4];
                Buffer.BlockCopy(buffer, 0, arrInt32, 0, length);
                return new decimal(arrInt32);
            });
        }

        /// <summary>
        /// Reads a DateTime from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the DateTime from</param>
        /// <returns>A DateTime</returns>
        public FileStreamReadResult ReadDateTime(long offset)
        {
            return ReadCellHelper(offset, length =>
            {
                long ticks = BitConverter.ToInt64(buffer, 0);
                return new DateTime(ticks);
            });
        }

        /// <summary>
        /// Reads a DateTimeOffset from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the DateTimeOffset from</param>
        /// <returns>A DateTimeOffset</returns>
        public FileStreamReadResult ReadDateTimeOffset(long offset)
        {
            // DateTimeOffset is represented by DateTime.Ticks followed by TimeSpan.Ticks
            // both as Int64 values
            return ReadCellHelper(offset, length => {
                long dtTicks = BitConverter.ToInt64(buffer, 0);
                long dtOffset = BitConverter.ToInt64(buffer, 8);
                return new DateTimeOffset(new DateTime(dtTicks), new TimeSpan(dtOffset)); 
            });
        }

        /// <summary>
        /// Reads a TimeSpan from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the TimeSpan from</param>
        /// <returns>A TimeSpan</returns>
        public FileStreamReadResult ReadTimeSpan(long offset)
        {
            return ReadCellHelper(offset, length =>
            {
                long ticks = BitConverter.ToInt64(buffer, 0);
                return new TimeSpan(ticks);
            });
        }

        /// <summary>
        /// Reads a string from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the string from</param>
        /// <returns>A string</returns>
        public FileStreamReadResult ReadString(long offset)
        {
            return ReadCellHelper(offset, length =>
                length > 0
                    ? Encoding.Unicode.GetString(buffer, 0, length)
                    : string.Empty, totalLength => totalLength == 1);
        }

        /// <summary>
        /// Reads bytes from the file at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the bytes from</param>
        /// <returns>A byte array</returns>
        public FileStreamReadResult ReadBytes(long offset)
        {
            return ReadCellHelper(offset, length =>
            {
                byte[] output = new byte[length];
                Buffer.BlockCopy(buffer, 0, output, 0, length);
                return output;
            }, totalLength => totalLength == 1);
        }

        /// <summary>
        /// Reads the bytes that make up a GUID at the offset provided
        /// </summary>
        /// <param name="offset">Offset into the file to read the bytes from</param>
        /// <returns>A guid type object</returns>
        public FileStreamReadResult ReadGuid(long offset)
        {
            return ReadCellHelper(offset, length =>
            {
                byte[] output = new byte[length];
                Buffer.BlockCopy(buffer, 0, output, 0, length);
                return new SqlGuid(output);
            }, totalLength => totalLength == 1);
        }

        /// <summary>
        /// Reads a SqlMoney type from the offset provided
        /// into a 
        /// </summary>
        /// <param name="offset"></param>
        /// <returns>A sql money type object</returns>
        public FileStreamReadResult ReadMoney(long offset)
        {
            return ReadCellHelper(offset, length =>
            {
                int[] arrInt32 = new int[length / 4];
                Buffer.BlockCopy(buffer, 0, arrInt32, 0, length);
                return new SqlMoney(new decimal(arrInt32));
            });
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

        #region Private Helpers

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

        /// <summary>
        /// Reads the value of a cell from the file wrapper, checks to see if it null using
        /// <paramref name="isNullFunc"/>, and converts it to the proper output type using
        /// <paramref name="convertFunc"/>.
        /// </summary>
        /// <param name="offset">Offset into the file to read from</param>
        /// <param name="convertFunc">Function to use to convert the buffer to the target type</param>
        /// <param name="isNullFunc">
        /// If provided, this function will be used to determine if the value is null
        /// </param>
        /// <returns>The object, a display value, and the length of the value + its length</returns>
        private FileStreamReadResult ReadCellHelper(long offset, Func<int, object> convertFunc, Func<int, bool> isNullFunc = null)
        {
            LengthResult length = ReadLength(offset);
            DbCellValue result = new DbCellValue();

            if (isNullFunc == null ? length.ValueLength == 0 : isNullFunc(length.TotalLength))
            {
                result.RawObject = null;
                result.DisplayValue = null;
            }
            else
            {
                AssureBufferLength(length.ValueLength);
                fileStream.ReadData(buffer, length.ValueLength);
                result.RawObject = convertFunc(length.ValueLength);
                result.DisplayValue = result.RawObject.ToString();
            }

            return new FileStreamReadResult(result, length.TotalLength);
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
