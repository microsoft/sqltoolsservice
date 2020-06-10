//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Utility
{
    public class TestResultSet : IEnumerable<object[]>
    {
        public List<DbColumn> Columns;
        public List<object[]> Rows;

        public static List<DbColumn> GetStandardColumns(int columnCount)
        {
            return Enumerable.Range(0, columnCount).Select(i => new TestDbColumn($"Col{i}")).Cast<DbColumn>().ToList();
        }

        /// <summary>
        /// This creates a test result set object with specified number of columns and rows.
        /// The implementation is done in parallel in multiple tasks if the number of rows is large so this method scales even to create millions of rows.
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="rows"></param>
        public TestResultSet(int columns, int rows)
        {
            Columns = GetStandardColumns(columns);
            Rows = new List<object[]>(rows);
            if (rows > 100)
            {
                var partitioner = Partitioner.Create(0, rows);
                Parallel.ForEach(partitioner, (range, loopState) => { AddRange(range); });
            }
            else if (rows > 0)
            {
               AddRange(new Tuple<int, int>(0, rows));  
            }
        }

        private void AddRange(Tuple<int, int> range)
        {
            for (int i = range.Item1; i < range.Item2; i++)
            {
                var rowIdx = i;
                var row = Enumerable.Range(0, Columns.Count).Select(j => $"Cell{rowIdx}.{j}").Cast<object>()
                    .ToArray();
                Rows.Add(row);
            }
        }

        public TestResultSet(IEnumerable<DbColumn> columns, IEnumerable<object[]> rows)
        {
            Columns = new List<DbColumn>(columns);
            Rows = new List<object[]>(rows);
        }

        #region IEnumerable<object[]> Impementation

        public IEnumerator<object[]> GetEnumerator()
        {
            return (IEnumerator<object[]>) Rows.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
