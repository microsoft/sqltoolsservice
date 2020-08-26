// <copyright file="KustoQueryUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>
using System;
using System.Data;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public class DataReaderWrapper : IDataReader
    {
        private readonly IDataReader _inner ;
        public DataReaderWrapper(IDataReader inner)
        {
            _inner = inner;
        }

        public object this[int i] => _inner[i];

        public object this[string name] => _inner[name];

        public int Depth => _inner.Depth;

        public bool IsClosed => _inner.IsClosed;

        public int RecordsAffected => _inner.RecordsAffected;

        public int FieldCount => _inner.FieldCount;

        public void Close() => _inner.Close();
        public void Dispose() => _inner.Dispose();
        public bool GetBoolean(int i) => _inner.GetBoolean(i);
        public byte GetByte(int i) => _inner.GetByte(i);
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) => _inner.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        public char GetChar(int i) => _inner.GetChar(i);
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) => _inner.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        public IDataReader GetData(int i) => _inner.GetData(i);
        public string GetDataTypeName(int i) => _inner.GetDataTypeName(i);
        public DateTime GetDateTime(int i) => _inner.GetDateTime(i);
        public decimal GetDecimal(int i) => _inner.GetDecimal(i);
        public double GetDouble(int i) => _inner.GetDouble(i);
        public Type GetFieldType(int i) => _inner.GetFieldType(i);
        public float GetFloat(int i) => _inner.GetFloat(i);
        public Guid GetGuid(int i) => _inner.GetGuid(i);
        public short GetInt16(int i) => _inner.GetInt16(i);
        public int GetInt32(int i) => _inner.GetInt32(i);
        public long GetInt64(int i) => _inner.GetInt64(i);
        public string GetName(int i) => _inner.GetName(i);
        public int GetOrdinal(string name) => _inner.GetOrdinal(name);
        public DataTable GetSchemaTable() => _inner.GetSchemaTable();
        public string GetString(int i) => _inner.GetString(i);
        public object GetValue(int i) => _inner.GetValue(i);
        public int GetValues(object[] values) => _inner.GetValues(values);
        public bool IsDBNull(int i) => _inner.IsDBNull(i);
        public virtual bool NextResult() => _inner.NextResult();
        public bool Read() => _inner.Read();
    }
}
