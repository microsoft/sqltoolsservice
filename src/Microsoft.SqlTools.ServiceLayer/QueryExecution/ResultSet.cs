//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
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
    }
}
