using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecutionServices.Contracts
{
    public class ResultSet
    {
        public DbColumn[] Columns { get; set; }

        public List<object[]> Rows { get; private set; }

        public ResultSet()
        {
            Rows = new List<object[]>();
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
    }
}
