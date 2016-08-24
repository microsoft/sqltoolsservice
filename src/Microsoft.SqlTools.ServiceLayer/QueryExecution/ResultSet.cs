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

        public List<object[]> Rows { get; private set; }

        public long RowCount { get; set; }

        private string bufferFileName;

        private DbDataReader DataReader { get; set; }

        public bool HasLongFields { get; private set; }

        private ArrayList64 FileOffsets { get; set; }

        private long currentFileOffset;

        public int MaxCharsToStore { get; set; }

        #endregion

        public ResultSet(DbDataReader reader, IFileStreamWriter fileWriter)
        {
            // Sanity check to make sure we got a reader
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader), "Reader cannot be null");
            }
            DataReader = reader;

            // Initialize the storage
            bufferFileName = Path.GetTempFileName();
            if (bufferFileName.Length == 0)
            {
                throw new FileNotFoundException("Failed to get buffer file name");
            }

            // Open a writer for the file
            IFileStreamWriter writer = fileWriter;
            writer.Init(bufferFileName);
        }

        public async Task ReadResultToEnd()
        {
            // If we can initialize the columns using the column schema, use that
            if (!DataReader.CanGetColumnSchema())
            {
                throw new InvalidOperationException("Could not retrieve column schema for result set.");
            }
            Columns = DataReader.GetColumnSchema().Select(column => new DbColumnWrapper(column)).ToArray();
            HasLongFields = Columns.Any(column => column.IsLong.HasValue && column.IsLong.Value);

            while (await DataReader.ReadAsync())
            {
                RowCount++;
                FileOffsets.Add(currentFileOffset);

                object[] values = new object[Columns.Length];
                if (!HasLongFields)
                {
                    // get all record values in one shot if there are no extra long fields
                    DataReader.GetValues(values);
                }

                for (int i = 0; i < Columns.Length; i++)
                {
                    if (HasLongFields)
                    {
                        if (DataReader.IsDBNull(i))
                        {
                            // Need special case for DBNull because
                            // reader.GetValue doesn't return DBNull in case of SqlXml and CLR type
                            values[i] = System.DBNull.Value;
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
                                    values[i] = reader.GetCharsWithMaxCapacity(i,
                                        ci.IsXml ? MaxXmlCharsToStore : MaxCharsToStore);
                                }
                                else if (ci.IsXml)
                                {
                                    Debug.Assert(MaxXmlCharsToStore > 0);
                                    // GetXmlWithMaxCapacity uses an anonymous delegate that allows the underlying
                                    // StreamWriter to periodically check the value of m_bKeepStoringData
                                    values[i] = reader.GetXmlWithMaxCapacity(i, this.MaxXmlCharsToStore, delegate () { return m_bKeepStoringData; });
                                }
                                else // we should never get here
                                {
                                    Debug.Assert(false);
                                    values[i] = reader.GetValue(i); // read anyway using standard retrieval mechanism
                                }
                            }
                        }
                    }

                    tVal = values[i].GetType(); // get true type of the object
                    STrace.Trace(DataStorageConstants.ComponentName, DataStorageConstants.NormalTrace, "Column " + i + " Type " + tVal.ToString());

                    if (Variables.dbNullType == tVal)
                    {
                        m_i64CurrentOffset += m_fsw.WriteNull();
                    }
                    else
                    {
                        if (((IColumnInfo)m_arrColumns[i]).IsSqlVariant())
                        {
                            // serialize type information as a string before the value
                            Variables.strVal = tVal.ToString();
                            m_i64CurrentOffset += m_fsw.WriteString(Variables.strVal);
                            Variables.strVal = null;
                        }

                        if (Variables.strType == tVal)        // String - most frequently used data type
                        {
                            Variables.strVal = (string)values[i];
                            m_i64CurrentOffset += m_fsw.WriteString(Variables.strVal);
                            Variables.strVal = null;
                        }
                        else if (Variables.sqlStringType == tVal)  // SqlString
                        {
                            Variables.sqlStringVal = (SqlString)values[i];
                            if (Variables.sqlStringVal.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                m_i64CurrentOffset += m_fsw.WriteString(Variables.sqlStringVal.Value);
                            }
                            Variables.sqlStringVal = null;
                        }
                        else if (Variables.i16Type == tVal)    // Int16
                        {
                            Variables.i16Val = (Int16)values[i];
                            m_i64CurrentOffset += m_fsw.WriteInt16(Variables.i16Val);
                        }
                        else if (Variables.sqlI16Type == tVal) // SqlInt16
                        {
                            Variables.sqlI16Val = (SqlInt16)values[i];
                            if (Variables.sqlI16Val.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                m_i64CurrentOffset += m_fsw.WriteInt16(Variables.sqlI16Val.Value);
                            }
                        }
                        else if (Variables.i32Type == tVal)     // Int32
                        {
                            Variables.i32Val = (Int32)values[i];
                            m_i64CurrentOffset += m_fsw.WriteInt32(Variables.i32Val);
                        }
                        else if (Variables.sqlI32Type == tVal) // SqlInt32
                        {
                            Variables.sqlI32Val = (SqlInt32)values[i];
                            if (Variables.sqlI32Val.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                m_i64CurrentOffset += m_fsw.WriteInt32(Variables.sqlI32Val.Value);
                            }
                        }
                        else if (Variables.i64Type == tVal)     // Int64
                        {
                            Variables.i64Val = (Int64)values[i];
                            m_i64CurrentOffset += m_fsw.WriteInt64(Variables.i64Val);
                        }
                        else if (Variables.sqlI64Type == tVal) // SqlInt64
                        {
                            Variables.sqlI64Val = (SqlInt64)values[i];
                            if (Variables.sqlI64Val.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                m_i64CurrentOffset += m_fsw.WriteInt64(Variables.sqlI64Val.Value);
                            }
                        }
                        else if (Variables.byteType == tVal)    // Byte
                        {
                            Variables.byteVal = (byte)values[i];
                            m_i64CurrentOffset += m_fsw.WriteByte(Variables.byteVal);
                        }
                        else if (Variables.sqlByteType == tVal)  // SqlByte
                        {
                            Variables.sqlByteVal = (SqlByte)values[i];
                            if (Variables.sqlByteVal.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                m_i64CurrentOffset += m_fsw.WriteByte(Variables.sqlByteVal.Value);
                            }
                        }
                        else if (Variables.chType == tVal)       // Char
                        {
                            Variables.chVal = (char)values[i];
                            m_i64CurrentOffset += m_fsw.WriteChar(Variables.chVal);
                        }
                        else if (Variables.boolType == tVal)       // Boolean
                        {
                            Variables.boolVal = (bool)values[i];
                            m_i64CurrentOffset += m_fsw.WriteBoolean(Variables.boolVal);
                        }
                        else if (Variables.sqlBoolType == tVal)       // SqlBoolean
                        {
                            Variables.sqlBoolVal = (SqlBoolean)values[i];
                            if (Variables.sqlBoolVal.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                m_i64CurrentOffset += m_fsw.WriteBoolean(Variables.sqlBoolVal.Value);
                            }
                        }
                        else if (Variables.dblType == tVal)       // Double
                        {
                            Variables.dblVal = (double)values[i];
                            m_i64CurrentOffset += m_fsw.WriteDouble(Variables.dblVal);
                        }
                        else if (Variables.sqlDblType == tVal)       // SqlDouble
                        {
                            Variables.sqlDblVal = (SqlDouble)values[i];
                            if (Variables.sqlDblVal.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                m_i64CurrentOffset += m_fsw.WriteDouble(Variables.sqlDblVal.Value);
                            }
                        }
                        else if (Variables.sqlSingleType == tVal)       // SqlSingle
                        {
                            Variables.sqlSingleVal = (SqlSingle)values[i];
                            if (Variables.sqlSingleVal.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                m_i64CurrentOffset += m_fsw.WriteSingle(Variables.sqlSingleVal.Value);
                            }
                        }
                        else if (Variables.decimalType == tVal)     // Decimal
                        {
                            Variables.decimalVal = (Decimal)values[i];
                            m_i64CurrentOffset += m_fsw.WriteDecimal(Variables.decimalVal);
                        }
                        else if (Variables.sqlDecimalType == tVal) // SqlDecimal
                        {
                            Variables.sqlDecimalVal = (SqlDecimal)values[i];
                            if (Variables.sqlDecimalVal.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                m_i64CurrentOffset += m_fsw.WriteSqlDecimal(Variables.sqlDecimalVal);
                            }
                        }
                        else if (Variables.dateTimeType == tVal)      // DateTime
                        {
                            Variables.dateTimeVal = (DateTime)values[i];
                            m_i64CurrentOffset += m_fsw.WriteDateTime(Variables.dateTimeVal);
                        }
                        else if (Variables.dateTimeOffsetType == tVal)      // DateTimeOffset
                        {
                            Variables.dateTimeOffsetVal = (DateTimeOffset)values[i];
                            m_i64CurrentOffset += m_fsw.WriteDateTimeOffset(Variables.dateTimeOffsetVal);
                        }
                        else if (Variables.sqlDateTimeType == tVal)      // SqlDateTime
                        {
                            Variables.sqlDateTimeVal = (SqlDateTime)values[i];
                            if (Variables.sqlDateTimeVal.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                m_i64CurrentOffset += m_fsw.WriteDateTime(Variables.sqlDateTimeVal.Value);
                            }
                        }
                        else if (Variables.timeSpanType == tVal) // System.TimeSpan
                        {
                            Variables.timeSpan = (TimeSpan)values[i];
                            m_i64CurrentOffset += m_fsw.WriteTimeSpan(Variables.timeSpan);
                        }
                        else if (tVal == Variables.bytesType) // Bytes
                        {
                            Variables.bytesVal = (Byte[])values[i];
                            m_i64CurrentOffset += m_fsw.WriteBytes(Variables.bytesVal, Variables.bytesVal.Length);

                            Variables.bytesVal = null;
                        }
                        else if (tVal == Variables.sqlBytesType) // SqlBytes
                        {
                            Variables.sqlBytesVal = (SqlBytes)values[i];
                            if (Variables.sqlBytesVal.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                Variables.bytesVal = Variables.sqlBytesVal.Value;
                                m_i64CurrentOffset += m_fsw.WriteBytes(Variables.bytesVal, Variables.bytesVal.Length);
                            }

                            Variables.sqlBytesVal = null;
                            Variables.bytesVal = null;
                        }
                        else if (Variables.sqlBinaryType == tVal)    // SqlBinary
                        {
                            Variables.sqlBinaryVal = (SqlBinary)values[i];
                            if (Variables.sqlBinaryVal.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                Variables.bytesVal = Variables.sqlBinaryVal.Value;
                                m_i64CurrentOffset += m_fsw.WriteBytes(Variables.bytesVal, Variables.bytesVal.Length);
                            }

                            Variables.sqlBinaryVal = null;
                            Variables.bytesVal = null;
                        }
                        else if (Variables.sqlGuidType == tVal)    // SqlGuid
                        {
                            Variables.sqlGuidVal = (SqlGuid)values[i];
                            if (Variables.sqlGuidVal.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                Variables.bytesVal = Variables.sqlGuidVal.ToByteArray();
                                m_i64CurrentOffset += m_fsw.WriteBytes(Variables.bytesVal, Variables.bytesVal.Length);
                            }
                        }
                        else if (Variables.sqlMoneyType == tVal)    // SqlMoney
                        {
                            Variables.sqlMoneyVal = (SqlMoney)values[i];
                            if (Variables.sqlMoneyVal.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                Variables.decimalVal = Variables.sqlMoneyVal.Value;
                                m_i64CurrentOffset += m_fsw.WriteDecimal(Variables.decimalVal);
                            }
                        }
                        /*
                        * There are bugs in web data provider that don't allow us to find out
                        * true object type
                        *
                        else if (Variables.sqlDateType == tVal)    // SqlDate
                        {
                            Variables.sqlDateVal = (SqlDate)values[i];
                            if (Variables.sqlDateVal.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                Variables.sqlI64Val = Variables.sqlDateVal.Ticks;
                                m_i64CurrentOffset += m_fsw.WriteInt64(Variables.sqlI64Val.Value);
                            }
                        }
                        else if (Variables.sqlTimeType == tVal)    // SqlTime
                        {
                            Variables.sqlTimeVal = (SqlTime)values[i];
                            if (Variables.sqlTimeVal.IsNull)
                            {
                                m_i64CurrentOffset += m_fsw.WriteNull();
                            }
                            else
                            {
                                Variables.sqlI64Val = Variables.sqlTimeVal.Ticks;
                                m_i64CurrentOffset += m_fsw.WriteInt64(Variables.sqlI64Val.Value);
                            }
                        }
                        */
                        else // treat everything else as string
                        {
                            Variables.strVal = values[i].ToString();
                            m_i64CurrentOffset += m_fsw.WriteString(Variables.strVal);
                        }
                    }
                }
                m_fsw.FlushBuffer();

                // Loop over the columns and read the data from the column
                for (int i = 0; i < Columns.Length; ++i)
                {
                    
                }
            }
        }

        /// <summary>
        /// Add a row of data to the result set using a <see cref="DbDataReader"/> that has already
        /// read in a row.
        /// </summary>
        /// <param name="reader">A <see cref="DbDataReader"/> that has already had a read performed</param>
        public void AddRow(DbDataReader reader)
        {
            List<object> row = new List<object>();
            for (int i = 0; i < reader.FieldCount; ++i)
            {
                row.Add(reader.GetValue(i));
            }
            Rows.Add(row.ToArray());
        }

        /// <summary>
        /// Generates a subset of the rows from the result set
        /// </summary>
        /// <param name="startRow">The starting row of the results</param>
        /// <param name="rowCount">How many rows to retrieve</param>
        /// <returns>A subset of results</returns>
        public ResultSetSubset GetSubset(int startRow, int rowCount)
        {
            // Sanity check to make sure that the row and the row count are within bounds
            if (startRow < 0 || startRow >= Rows.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(startRow), "Start row cannot be less than 0 " +
                                                                        "or greater than the number of rows in the resultset");
            }
            if (rowCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowCount), "Row count must be a positive integer");
            }

            // Retrieve the subset of the results as per the request
            object[][] rows = Rows.Skip(startRow).Take(rowCount).ToArray();
            return new ResultSetSubset
            {
                Rows = rows,
                RowCount = rows.Length
            };
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
