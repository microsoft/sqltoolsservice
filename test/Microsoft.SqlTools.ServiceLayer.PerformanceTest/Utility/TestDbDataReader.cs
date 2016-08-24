using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using Moq;

namespace Microsoft.SqlTools.ServiceLayer.PerformanceTest.Utility
{
    public class TestDbDataReader : DbDataReader, IDbColumnSchemaGenerator
    {

        #region Test Specific Implementations

        private Dictionary<string, string>[][] Data { get; set; }

        public IEnumerator<Dictionary<string, string>[]> ResultSet { get; private set; }

        private IEnumerator<Dictionary<string, string>> Rows { get; set; }

        public TestDbDataReader(Dictionary<string, string>[][] data)
        {
            Data = data;
            if (Data != null)
            {
                ResultSet = ((IEnumerable<Dictionary<string, string>[]>)Data).GetEnumerator();
                ResultSet.MoveNext();
            }
        }

        #endregion

        public override bool HasRows
        {
            get { return ResultSet != null && ResultSet.Current.Length > 0; }
        }

        public override bool Read()
        {
            if (Rows == null)
            {
                Rows = ((IEnumerable<Dictionary<string, string>>)ResultSet.Current).GetEnumerator();
            }
            return Rows.MoveNext();
        }

        public override bool NextResult()
        {
            if (Data == null || !ResultSet.MoveNext())
            {
                return false;
            }
            Rows = ((IEnumerable<Dictionary<string, string>>)ResultSet.Current).GetEnumerator();
            return true;
        }

        public override object GetValue(int ordinal)
        {
            return this[ordinal];
        }

        public override object this[string name]
        {
            get { return Rows.Current[name]; }
        }

        public override object this[int ordinal]
        {
            get { return Rows.Current[Rows.Current.Keys.AsEnumerable().ToArray()[ordinal]]; }
        }

        public ReadOnlyCollection<DbColumn> GetColumnSchema()
        {
            if (ResultSet?.Current == null || ResultSet.Current.Length <= 0)
            {
                return new ReadOnlyCollection<DbColumn>(new List<DbColumn>());
            }

            List<DbColumn> columns = new List<DbColumn>();
            for (int i = 0; i < ResultSet.Current[0].Count; i++)
            {
                columns.Add(new Mock<DbColumn>().Object);
            }
            return new ReadOnlyCollection<DbColumn>(columns);
        }

        public override int FieldCount { get { return Rows?.Current.Count ?? 0; } }

        #region Not Implemented

        public override bool GetBoolean(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override byte GetByte(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override char GetChar(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override DateTime GetDateTime(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override decimal GetDecimal(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override double GetDouble(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public override string GetName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetInt64(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetInt32(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override short GetInt16(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override Guid GetGuid(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override float GetFloat(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override Type GetFieldType(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override string GetString(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public override bool IsDBNull(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override int Depth { get; }
        public override bool IsClosed { get; }
        public override int RecordsAffected { get; }

        #endregion        
    }
}
