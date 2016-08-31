//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Wrapper around a DbData reader to perform some special operations more simply
    /// </summary>
    /// <remarks>
    /// This code is based on code from Microsoft.SqlServer.Management.UI.Grid, SSMS DataStorage, 
    /// StorageDataReader
    /// </remarks>
    public class StorageDataReader
    {
        #region Properties

        public DbColumnWrapper[] Columns { get; private set; }

        /// <summary>
        /// The <see cref="DbDataReader"/> that will be read from
        /// </summary>
        public DbDataReader DbDataReader { get; private set; }

        /// <summary>
        /// If the DbDataReader is a SqlDataReader, it will be set here
        /// </summary>
        private SqlDataReader SqlDataReader { get; set; }

        /// <summary>
        /// Whether or not the data reader supports SqlXml types
        /// </summary>
        private bool SupportSqlXml
        {
            get { return SqlDataReader != null; }
        }

        #endregion

        /// <summary>
        /// Constructs a new wrapper around the provided reader
        /// </summary>
        /// <param name="reader">The reader to wrap around</param>
        public StorageDataReader(DbDataReader reader)
        {
            // Sanity check to make sure there is a data reader
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader), "Reader cannot be null.");
            }

            // Attempt to use this reader as a SqlDataReader
            SqlDataReader = reader as SqlDataReader;
            DbDataReader = reader;

            // Read the columns into a set of wrappers
            Columns = DbDataReader.GetColumnSchema().Select(column => new DbColumnWrapper(column)).ToArray();
        }

        #region DbDataReader Methods

        /// <summary>
        /// Pass-through to DbDataReader.ReadAsync()
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to use for cancelling a query</param>
        /// <returns></returns>
        public Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            return DbDataReader.ReadAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves a value
        /// </summary>
        /// <param name="i">Column ordinal</param>
        /// <returns>The value of the given column</returns>
        public object GetValue(int i)
        {
            return SqlDataReader == null ? DbDataReader.GetValue(i) : SqlDataReader.GetValue(i);
        }

        /// <summary>
        /// Stores all values of the current row into the provided object array
        /// </summary>
        /// <param name="values">Where to store the values from this row</param>
        public void GetValues(object[] values)
        {
            if (SqlDataReader == null)
            {
                DbDataReader.GetValues(values);
            }
            else
            {
                SqlDataReader.GetValues(values);
            }
        }

        /// <summary>
        /// Whether or not the cell of the given column at the current row is a DBNull
        /// </summary>
        /// <param name="i">Column ordinal</param>
        /// <returns>True if the cell is DBNull, false otherwise</returns>
        public bool IsDBNull(int i)
        {
            return DbDataReader.IsDBNull(i);
        }

        #endregion

        #region Custom Implementation

        /// <summary>
        /// Retrieves bytes with a maximum number of bytes to return
        /// </summary>
        /// <param name="iCol">Column ordinal</param>
        /// <param name="maxNumBytesToReturn">Number of bytes to return at maximum</param>
        /// <returns>Byte array</returns>
        public byte[] GetBytesWithMaxCapacity(int iCol, int maxNumBytesToReturn)
        {
            if (maxNumBytesToReturn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxNumBytesToReturn), "Maximum number of bytes to return must be greater than zero.");
            }

            //first, ask provider how much data it has and calculate the final # of bytes
            //NOTE: -1 means that it doesn't know how much data it has
            long neededLength;
            long origLength = neededLength = GetBytes(iCol, 0, null, 0, 0);
            if (neededLength == -1 || neededLength > maxNumBytesToReturn)
            {
                neededLength = maxNumBytesToReturn;
            }

            //get the data up to the maxNumBytesToReturn
            byte[] bytesBuffer = new byte[neededLength];
            GetBytes(iCol, 0, bytesBuffer, 0, (int)neededLength);

            //see if server sent back more data than we should return
            if (origLength == -1 || origLength > neededLength)
            {
                //pump the rest of data from the reader and discard it right away
                long dataIndex = neededLength;
                const int tmpBufSize = 100000;
                byte[] tmpBuf = new byte[tmpBufSize];
                while (GetBytes(iCol, dataIndex, tmpBuf, 0, tmpBufSize) == tmpBufSize)
                {
                    dataIndex += tmpBufSize;
                }
            }

            return bytesBuffer;
        }

        /// <summary>
        /// Retrieves characters with a maximum number of charss to return
        /// </summary>
        /// <param name="iCol">Column ordinal</param>
        /// <param name="maxCharsToReturn">Number of chars to return at maximum</param>
        /// <returns>String</returns>
        public string GetCharsWithMaxCapacity(int iCol, int maxCharsToReturn)
        {
            if (maxCharsToReturn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCharsToReturn), "Maximum number of bytes to return must be greater than zero");
            }

            //first, ask provider how much data it has and calculate the final # of chars
            //NOTE: -1 means that it doesn't know how much data it has
            long neededLength;
            long origLength = neededLength = GetChars(iCol, 0, null, 0, 0);
            if (neededLength == -1 || neededLength > maxCharsToReturn)
            {
                neededLength = maxCharsToReturn;
            }
            Debug.Assert(neededLength < int.MaxValue);

            //get the data up to maxCharsToReturn
            char[] buffer = new char[neededLength];
            if (neededLength > 0)
            {
                GetChars(iCol, 0, buffer, 0, (int)neededLength);
            }

            //see if server sent back more data than we should return
            if (origLength == -1 || origLength > neededLength)
            {
                //pump the rest of data from the reader and discard it right away
                long dataIndex = neededLength;
                const int tmpBufSize = 100000;
                char[] tmpBuf = new char[tmpBufSize];
                while (GetChars(iCol, dataIndex, tmpBuf, 0, tmpBufSize) == tmpBufSize)
                {
                    dataIndex += tmpBufSize;
                }
            }
            string res = new string(buffer);
            return res;
        }

        /// <summary>
        /// Retrieves xml with a maximum number of bytes to return
        /// </summary>
        /// <param name="iCol">Column ordinal</param>
        /// <param name="maxCharsToReturn">Number of chars to return at maximum</param>
        /// <returns>String</returns>
        public string GetXmlWithMaxCapacity(int iCol, int maxCharsToReturn)
        {
            if (SupportSqlXml)
            {
                SqlXml sm = GetSqlXml(iCol);
                if (sm == null)
                {
                    return null;
                }

                //this code is mostly copied from SqlClient implementation of returning value for XML data type
                StringWriterWithMaxCapacity sw = new StringWriterWithMaxCapacity(null, maxCharsToReturn);
                XmlWriterSettings writerSettings = new XmlWriterSettings
                {
                    CloseOutput = false,
                    ConformanceLevel = ConformanceLevel.Fragment
                };
                // don't close the memory stream
                XmlWriter ww = XmlWriter.Create(sw, writerSettings);

                XmlReader reader = sm.CreateReader();
                reader.Read();

                while (!reader.EOF)
                {
                    ww.WriteNode(reader, true);
                }
                ww.Flush();
                return sw.ToString();
            }

            object o = GetValue(iCol);
            return o?.ToString();
        }

        #endregion

        #region Private Helpers

        private long GetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length)
        {
            return DbDataReader.GetBytes(i, dataIndex, buffer, bufferIndex, length);
        }

        private long GetChars(int i, long dataIndex, char[] buffer, int bufferIndex, int length)
        {
            return DbDataReader.GetChars(i, dataIndex, buffer, bufferIndex, length);
        }

        private SqlXml GetSqlXml(int i)
        {
            if (SqlDataReader == null)
            {
                // We need a Sql data reader in order to retrieve sql xml
                throw new InvalidOperationException("Cannot retrieve SqlXml without a SqlDataReader");
            }

            return SqlDataReader.GetSqlXml(i);
        }

        #endregion

        /// <summary>
        /// Internal class for writing strings with a maximum capacity
        /// </summary>
        /// <remarks>
        /// This code is take almost verbatim from Microsoft.SqlServer.Management.UI.Grid, SSMS 
        /// DataStorage, StorageDataReader class.
        /// </remarks>
        private class StringWriterWithMaxCapacity : StringWriter
        {
            bool stopWriting;


            private int CurrentLength
            {
                get { return GetStringBuilder().Length; }
            }

            public StringWriterWithMaxCapacity(IFormatProvider formatProvider, int capacity) : base(formatProvider)
            {
                MaximumCapacity = capacity;
            }

            public int MaximumCapacity { get; set; }

            public override void Write(char value)
            {
                if (!stopWriting)
                {
                    if (CurrentLength < MaximumCapacity)
                    {
                        base.Write(value);
                    }
                    else
                    {
                        stopWriting = true;
                    }
                }
            }

            public override void Write(char[] buffer, int index, int count)
            {
                if (!stopWriting)
                {
                    int curLen = CurrentLength;
                    if (curLen + (count - index) > MaximumCapacity)
                    {
                        stopWriting = true;

                        count = MaximumCapacity - curLen + index;
                        if (count < 0)
                        {
                            count = 0;
                        }
                        base.Write(buffer, index, count);
                    }
                }
            }

            public override void Write(string value)
            {
                if (!stopWriting)
                {
                    int curLen = CurrentLength;
                    if (value.Length + curLen > MaximumCapacity)
                    {
                        stopWriting = true;
                        base.Write(value.Substring(0, MaximumCapacity - curLen));
                    }
                    else
                    {
                        base.Write(value);
                    }
                }
            }
        }

    }
}
