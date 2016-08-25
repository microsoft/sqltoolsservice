using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class StorageDataReader
    {

        public DbDataReader DbDataReader { get; private set; }
        private readonly SqlDataReader sqlDataReader;

        private bool SupportSqlXml
        {
            get { return sqlDataReader != null; }
        }

        public StorageDataReader(DbDataReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader), "Reader cannot be null.");
            }

            // Attempt to use this reader as a SqlDataReader
            sqlDataReader = reader as SqlDataReader;
            DbDataReader = reader;
        }

        public int FieldCount
        {
            get { return DbDataReader.VisibleFieldCount; }
        }

        public string GetName(int i)
        {
            return DbDataReader.GetName(i);
        }

        public string GetDataTypeName(int i)
        {
            return DbDataReader.GetDataTypeName(i);
        }

        public string GetProviderSpecificDataTypeName(int i)
        {
            Type t = GetFieldType(i);
            return t.ToString();
        }

        public Type GetFieldType(int i)
        {
            return DbDataReader.GetProviderSpecificFieldType(i);
        }

        public Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            return DbDataReader.ReadAsync(cancellationToken);
        }

        public object GetValue(int i)
        {
            return sqlDataReader == null ? DbDataReader.GetValue(i) : sqlDataReader.GetValue(i);
        }

        public void GetValues(object[] values)
        {
            if (sqlDataReader == null)
            {
                DbDataReader.GetValues(values);
            }
            else
            {
                sqlDataReader.GetValues(values);
            }
        }

        public bool IsDBNull(int i)
        {
            return DbDataReader.IsDBNull(i);
        }

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
            if (sqlDataReader == null)
            {
                // We need a Sql data reader in order to retrieve sql xml
                throw new InvalidOperationException("Cannot retrieve SqlXml without a SqlDataReader");
            }

            return sqlDataReader.GetSqlXml(i);
        }

        #endregion

        private class StringWriterWithMaxCapacity : StringWriter
        {
            bool stopWriting = false;


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

        public class StorageAbortedException : Exception
        {
            /// <summary>
            /// TODO
            /// </summary>
            public StorageAbortedException()
            {
            }

            /// <summary>
            /// TODO
            /// </summary>
            public StorageAbortedException(String message)
                : base(message)
            {
            }

            /// <summary>
            /// TODO
            /// </summary>
            public StorageAbortedException(String message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

    }
}
