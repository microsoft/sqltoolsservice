//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    public class ResultSet : IDisposable
    {
        #region Properties

        public DbColumnWrapper[] Columns { get; set; }

        public long RowCount { get; set; }

        private string bufferFileName;

        private StorageDataReader DataReader { get; set; }

        private IFileStreamWriter FileWriter { get; set; }

        public bool HasLongFields { get; private set; }

        private ArrayList64 FileOffsets { get; set; }

        private long currentFileOffset;

        public int MaxCharsToStore { get; set; }

        public int MaxXmlCharsToStore { get; set; }

        #endregion

        public ResultSet(DbDataReader reader, IFileStreamWriter fileWriter)
        {
            // Sanity check to make sure we got a reader
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader), "Reader cannot be null");
            }
            DataReader = new StorageDataReader(reader);

            // Initialize the storage
            bufferFileName = Path.GetTempFileName();
            if (bufferFileName.Length == 0)
            {
                throw new FileNotFoundException("Failed to get buffer file name");
            }
            FileOffsets = new ArrayList64();

            // Open a writer for the file
            FileWriter = fileWriter;
            FileWriter.Init(bufferFileName);
        }

        public async Task ReadResultToEnd(CancellationToken cancellationToken)
        {
            // If we can initialize the columns using the column schema, use that
            if (!DataReader.DbDataReader.CanGetColumnSchema())
            {
                throw new InvalidOperationException("Could not retrieve column schema for result set.");
            }
            Columns = DataReader.DbDataReader.GetColumnSchema().Select(column => new DbColumnWrapper(column)).ToArray();
            HasLongFields = Columns.Any(column => column.IsLong.HasValue && column.IsLong.Value);

            while (await DataReader.ReadAsync(cancellationToken))
            {
                RowCount++;
                FileOffsets.Add(currentFileOffset);

                object[] values = new object[Columns.Length];
                if (!HasLongFields)
                {
                    // get all record values in one shot if there are no extra long fields
                    DataReader.GetValues(values);
                }

                // Loop over all the columns and write the values to the temp file
                for (int i = 0; i < Columns.Length; i++)
                {
                    if (HasLongFields)
                    {
                        if (DataReader.IsDBNull(i))
                        {
                            // Need special case for DBNull because
                            // reader.GetValue doesn't return DBNull in case of SqlXml and CLR type
                            values[i] = DBNull.Value;
                        }
                        else
                        {
                            DbColumnWrapper ci = Columns[i];
                            if (ci.IsLongField)
                            {
                                // not a long field 
                                values[i] = DataReader.GetValue(i);
                            }
                            else 
                            {
                                // this is a long field
                                if (ci.IsBytes)
                                {
                                    values[i] = DataReader.GetBytesWithMaxCapacity(i, MaxCharsToStore);
                                }
                                else if (ci.IsChars)
                                {
                                    Debug.Assert(MaxCharsToStore > 0);
                                    values[i] = DataReader.GetCharsWithMaxCapacity(i,
                                        ci.IsXml ? MaxXmlCharsToStore : MaxCharsToStore);
                                }
                                else if (ci.IsXml)
                                {
                                    Debug.Assert(MaxXmlCharsToStore > 0);
                                    values[i] = DataReader.GetXmlWithMaxCapacity(i, MaxXmlCharsToStore);
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
                        currentFileOffset += await FileWriter.WriteNull();
                    }
                    else
                    {
                        if (Columns[i].IsSqlVariant)
                        {
                            // serialize type information as a string before the value
                            string val = tVal.ToString();
                            currentFileOffset += await FileWriter.WriteString(val);
                        }

                        if (tVal == typeof(string))
                        {
                            // String - most frequently used data type
                            string val = (string)values[i];
                            currentFileOffset += await FileWriter.WriteString(val);
                        }
                        else if (tVal == typeof(SqlString))
                        {
                            // SqlString
                            SqlString val = (SqlString)values[i];
                            if (val.IsNull)
                            {
                                currentFileOffset += await FileWriter.WriteNull();
                            }
                            else
                            {
                                currentFileOffset += await FileWriter.WriteString(val.Value);
                            }
                        }
                        else if (tVal == typeof(short))
                        {
                            // Int16
                            short val = (short)values[i];
                            currentFileOffset += await FileWriter.WriteInt16(val);
                        }
                        else if (tVal == typeof(SqlInt16)) 
                        {
                            // SqlInt16
                            SqlInt16 val = (SqlInt16)values[i];
                            if (val.IsNull)
                            {
                                currentFileOffset += await FileWriter.WriteNull();
                            }
                            else
                            {
                                currentFileOffset += await FileWriter.WriteInt16(val.Value);
                            }
                        }
                        else if (tVal == typeof(int)) 
                        {
                            // Int32
                            int val = (int)values[i];
                            currentFileOffset += await FileWriter.WriteInt32(val);
                        }
                        else if (tVal == typeof(SqlInt32)) 
                        {
                            // SqlInt32
                            SqlInt32 val = (SqlInt32)values[i];
                            if (val.IsNull)
                            {
                                currentFileOffset += await FileWriter.WriteNull();
                            }
                            else
                            {
                                currentFileOffset += await FileWriter.WriteInt32(val.Value);
                            }
                        }
                        else if (tVal == typeof(long))
                        {
                            // Int64
                            long val = (long)values[i];
                            currentFileOffset += await FileWriter.WriteInt64(val);
                        }
                        else if (tVal == typeof(SqlInt64)) 
                        {
                            // SqlInt64
                            SqlInt64 val = (SqlInt64)values[i];
                            if (val.IsNull)
                            {
                                currentFileOffset += await FileWriter.WriteNull();
                            }
                            else
                            {
                                currentFileOffset += await FileWriter.WriteInt64(val.Value);
                            }
                        }
                        else if (tVal == typeof(byte))
                        {
                            // Byte
                            byte val = (byte)values[i];
                            currentFileOffset += await FileWriter.WriteByte(val);
                        }
                        else if (tVal == typeof(SqlByte))
                        {
                            // SqlByte
                            SqlByte val = (SqlByte)values[i];
                            if (val.IsNull)
                            {
                                currentFileOffset += await FileWriter.WriteNull();
                            }
                            else
                            {
                                currentFileOffset += await FileWriter.WriteByte(val.Value);
                            }
                        }
                        else if (tVal == typeof(char))
                        {
                            // Char
                            char val = (char)values[i];
                            currentFileOffset += await FileWriter.WriteChar(val);
                        }
                        else if (tVal == typeof(bool))
                        {
                            // Boolean
                            bool val = (bool)values[i];
                            currentFileOffset += await FileWriter.WriteBoolean(val);
                        }
                        else if (tVal == typeof(SqlBoolean))
                        {
                            // SqlBoolean
                            SqlBoolean val = (SqlBoolean)values[i];
                            if (val.IsNull)
                            {
                                currentFileOffset += await FileWriter.WriteNull();
                            }
                            else
                            {
                                currentFileOffset += await FileWriter.WriteBoolean(val.Value);
                            }
                        }
                        else if (tVal == typeof(double)) 
                        {
                            // Double
                            double val = (double)values[i];
                            currentFileOffset += await FileWriter.WriteDouble(val);
                        }
                        else if (tVal == typeof(SqlDouble))
                        {
                            // SqlDouble
                            SqlDouble val = (SqlDouble)values[i];
                            if (val.IsNull)
                            {
                                currentFileOffset += await FileWriter.WriteNull();
                            }
                            else
                            {
                                currentFileOffset += await FileWriter.WriteDouble(val.Value);
                            }
                        }
                        else if (tVal == typeof(SqlSingle))
                        {
                            // SqlSingle
                            SqlSingle val = (SqlSingle)values[i];
                            if (val.IsNull)
                            {
                                currentFileOffset += await FileWriter.WriteNull();
                            }
                            else
                            {
                                currentFileOffset += await FileWriter.WriteSingle(val.Value);
                            }
                        }
                        else if (tVal == typeof(decimal))
                        {
                            // Decimal
                            decimal val = (decimal)values[i];
                            currentFileOffset += await FileWriter.WriteDecimal(val);
                        }
                        else if (tVal == typeof(SqlDecimal))
                        {
                            // SqlDecimal
                            SqlDecimal val = (SqlDecimal) values[i];
                            if (val.IsNull)
                            {
                                currentFileOffset += await FileWriter.WriteNull();
                            }
                            else
                            {
                                currentFileOffset += await FileWriter.WriteSqlDecimal(val);
                            }
                        }
                        else if (tVal == typeof(DateTime))
                        {
                            // DateTime
                            DateTime val = (DateTime)values[i];
                            currentFileOffset += await FileWriter.WriteDateTime(val);
                        }
                        else if (tVal == typeof(DateTimeOffset))
                        {
                            // DateTimeOffset
                            DateTimeOffset val = (DateTimeOffset)values[i];
                            currentFileOffset += await FileWriter.WriteDateTimeOffset(val);
                        }
                        else if (tVal == typeof(SqlDateTime))
                        {
                            // SqlDateTime
                            SqlDateTime val = (SqlDateTime)values[i];
                            if (val.IsNull)
                            {
                                currentFileOffset += await FileWriter.WriteNull();
                            }
                            else
                            {
                                currentFileOffset += await FileWriter.WriteDateTime(val.Value);
                            }
                        }
                        else if (tVal == typeof(TimeSpan)) 
                        {
                            // TimeSpan
                            TimeSpan val = (TimeSpan) values[i];
                            currentFileOffset += await FileWriter.WriteTimeSpan(val);
                        }
                        else if (tVal == typeof(byte[])) 
                        {
                            // Bytes
                            byte[] val = (byte[])values[i];
                            currentFileOffset += await FileWriter.WriteBytes(val, val.Length);
                        }
                        else if (tVal == typeof(SqlBytes)) 
                        {
                            // SqlBytes
                            SqlBytes val = (SqlBytes)values[i];
                            if (val.IsNull)
                            {
                                currentFileOffset += await FileWriter.WriteNull();
                            }
                            else
                            {
                                currentFileOffset += await FileWriter.WriteBytes(val.Value, val.Value.Length);
                            }
                        }
                        else if (tVal == typeof(SqlBinary))
                        {
                            // SqlBinary
                            SqlBinary val = (SqlBinary)values[i];
                            if (val.IsNull)
                            {
                                currentFileOffset += await FileWriter.WriteNull();
                            }
                            else
                            {
                                currentFileOffset += await FileWriter.WriteBytes(val.Value, val.Value.Length);
                            }
                        }
                        else if (tVal == typeof(SqlGuid))
                        {
                            // SqlGuid
                            SqlGuid val = (SqlGuid)values[i];
                            if (val.IsNull)
                            {
                                currentFileOffset += await FileWriter.WriteNull();
                            }
                            else
                            {
                                byte[] bytesVal = val.ToByteArray();
                                currentFileOffset += await FileWriter.WriteBytes(bytesVal, bytesVal.Length);
                            }
                        }
                        else if (tVal == typeof(SqlMoney))
                        {
                            // SqlMoney
                            SqlMoney val = (SqlMoney)values[i];
                            if (val.IsNull)
                            {
                                currentFileOffset += await FileWriter.WriteNull();
                            }
                            else
                            {
                                currentFileOffset += await FileWriter.WriteDecimal(val.Value);
                            }
                        }
                        else 
                        {
                            // treat everything else as string
                            string val = values[i].ToString();
                            currentFileOffset += await FileWriter.WriteString(val);
                        }
                    }
                }

                // Flush the buffer after every row
                await FileWriter.FlushBuffer();
            }
        }

        /// <summary>
        /// Add a row of data to the result set using a <see cref="DbDataReader"/> that has already
        /// read in a row.
        /// </summary>
        /// <param name="reader">A <see cref="DbDataReader"/> that has already had a read performed</param>
        //public void AddRow(DbDataReader reader)
        //{
        //    List<object> row = new List<object>();
        //    for (int i = 0; i < reader.FieldCount; ++i)
        //    {
        //        row.Add(reader.GetValue(i));
        //    }
        //    Rows.Add(row.ToArray());
        //}

        /// <summary>
        /// Generates a subset of the rows from the result set
        /// </summary>
        /// <param name="startRow">The starting row of the results</param>
        /// <param name="rowCount">How many rows to retrieve</param>
        /// <returns>A subset of results</returns>
        public ResultSetSubset GetSubset(int startRow, int rowCount)
        {
            // Sanity check to make sure that the row and the row count are within bounds
            if (startRow < 0 || startRow >= RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(startRow), "Start row cannot be less than 0 " +
                                                                        "or greater than the number of rows in the resultset");
            }
            if (rowCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowCount), "Row count must be a positive integer");
            }

            // Retrieve the subset of the results as per the request
            //object[][] rows = Rows.Skip(startRow).Take(rowCount).ToArray();
            //return new ResultSetSubset
            //{
            //    Rows = rows,
            //    RowCount = rows.Length
            //};
            return null;
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
                // Cleanup the file that we opened to buffer results
            }

            disposed = true;
        }

        ~ResultSet()
        {
            Dispose(false);
        }

        #endregion
    }
}
