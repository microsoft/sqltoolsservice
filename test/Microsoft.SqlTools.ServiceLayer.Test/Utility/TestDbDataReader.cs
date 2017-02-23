//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.Test.Utility
{
    public class TestDbDataReader : DbDataReader, IDbColumnSchemaGenerator
    {

        #region Test Specific Implementations

        private IEnumerable<TestResultSet> Data { get; }

        public IEnumerator<TestResultSet> ResultSetEnumerator { get; }

        private IEnumerator<object[]> RowEnumerator { get; set; }

        public TestDbDataReader(IEnumerable<TestResultSet> data)
        {
            Data = data;
            if (Data != null)
            {
                ResultSetEnumerator = Data.GetEnumerator();
                ResultSetEnumerator.MoveNext();
            }
        }

        #endregion

        #region Properties

        public override int FieldCount => ResultSetEnumerator?.Current.Columns.Count ?? 0;

        public override bool HasRows => ResultSetEnumerator?.Current.Rows.Count > 0;

        /// <summary>
        /// Mimicks the behavior of SqlDbDataReader
        /// </summary>
        public override int RecordsAffected => RowEnumerator != null ? -1 : 1;

        public override object this[int ordinal] => RowEnumerator.Current[ordinal];

        #endregion

        #region Implemented Methods

        /// <summary>
        /// If the row enumerator hasn't been initialized for the current result set, the
        /// enumerator for the current result set is defined. Increments the enumerator
        /// </summary>
        /// <returns>True if tere were more rows, false otherwise</returns>
        public override bool Read()
        {
            if (RowEnumerator == null)
            {
                RowEnumerator = ResultSetEnumerator.Current.GetEnumerator();
            }
            return RowEnumerator.MoveNext();
        }

        /// <summary>
        /// Increments the result set enumerator and initializes the row enumerator
        /// </summary>
        /// <returns></returns>
        public override bool NextResult()
        {
            if (Data == null || !ResultSetEnumerator.MoveNext())
            {
                return false;
            }
            RowEnumerator = ResultSetEnumerator.Current.GetEnumerator();
            return true;
        }

        /// <summary>
        /// Retrieves the value for the cell of the current row in the given column
        /// </summary>
        /// <param name="ordinal">Ordinal of the column</param>
        /// <returns>The object in the cell</returns>
        public override object GetValue(int ordinal)
        {
            return this[ordinal];
        }

        /// <summary>
        /// Stores the values of all cells in this row in the given object array
        /// </summary>
        /// <param name="values">Destination for all cell values</param>
        /// <returns>Number of cells in the current row</returns>
        public override int GetValues(object[] values)
        {
            for (int i = 0; i < RowEnumerator.Current.Count(); i++)
            {
                values[i] = this[i];
            }
            return RowEnumerator.Current.Count();
        }

        /// <summary>
        /// Whether or not a given cell in the current row is null
        /// </summary>
        /// <param name="ordinal">Ordinal of the column</param>
        /// <returns>True if the cell is null, false otherwise</returns>
        public override bool IsDBNull(int ordinal)
        {
            return this[ordinal] == null;
        }

        /// <returns>Collection of test columns in the current result set</returns>
        public ReadOnlyCollection<DbColumn> GetColumnSchema()
        {
            if (ResultSetEnumerator?.Current == null || ResultSetEnumerator.Current.Rows.Count <= 0)
            {
                return new ReadOnlyCollection<DbColumn>(new List<DbColumn>());
            }

            return new ReadOnlyCollection<DbColumn>(ResultSetEnumerator.Current.Columns);
        }

        #endregion

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
            char[] allChars = ((string) RowEnumerator.Current[ordinal]).ToCharArray();
            int outLength = allChars.Length;
            if (buffer != null)
            {
                Array.Copy(allChars, (int) dataOffset, buffer, bufferOffset, outLength);
            }
            return outLength;
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

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override object this[string name]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int Depth { get { throw new NotImplementedException(); } }

        public override bool IsClosed { get { throw new NotImplementedException(); } }

        #endregion        
    }
}
