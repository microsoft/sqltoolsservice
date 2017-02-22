//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    public class TestResultSet : IEnumerable<object[]>
    {
        public List<DbColumn> Columns;
        public List<object[]> Rows;

        public TestResultSet(int columns, int rows)
        {
            Columns = Enumerable.Range(0, columns).Select(i => new TestDbColumn($"Col{i}")).Cast<DbColumn>().ToList();
            Rows = new List<object[]>(rows);
            for (int i = 0; i < rows; i++)
            {
                var row = Enumerable.Range(0, columns).Select(j => $"Cell{i}.{j}").Cast<object>().ToArray();
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
