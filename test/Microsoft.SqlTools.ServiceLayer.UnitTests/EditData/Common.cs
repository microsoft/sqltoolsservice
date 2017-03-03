//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.Utility;
using Moq;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    public class Common
    {
        public const string OwnerUri = "testFile";

        public static IEditTableMetadata GetStandardMetadata(DbColumn[] columns, bool allKeys = true, bool isMemoryOptimized = false)
        {
            // Create a Column Metadata Provider
            var columnMetas = columns.Select((c, i) =>
                new EditColumnWrapper
                {
                    DbColumn = new DbColumnWrapper(c),
                    EscapedName = c.ColumnName,
                    Ordinal = i,
                    IsKey = c.IsIdentity.HasTrue()
                }).ToArray();
            return GetMetadataProvider(columnMetas, allKeys, isMemoryOptimized);
        }

        public static DbColumn[] GetColumns(bool includeIdentity)
        {
            List<DbColumn> columns = new List<DbColumn>();

            if (includeIdentity)
            {
                columns.Add(new TestDbColumn("id", true));
            }

            for (int i = 0; i < 3; i++)
            {
                columns.Add(new TestDbColumn($"col{i}"));
            }
            return columns.ToArray();
        }

        public static ResultSet GetResultSet(DbColumn[] columns, bool includeIdentity, int rowCount = 1)
        {
            IEnumerable<object[]> rows = includeIdentity
                ? Enumerable.Repeat(new object[] { "id", "1", "2", "3" }, rowCount)
                : Enumerable.Repeat(new object[] { "1", "2", "3" }, rowCount);
            var testResultSet = new TestResultSet(columns, rows);
            var reader = new TestDbDataReader(new[] { testResultSet });
            var resultSet = new ResultSet(0, 0, MemoryFileSystem.GetFileStreamFactory());
            resultSet.ReadResultToEnd(reader, CancellationToken.None).Wait();
            return resultSet;
        }

        public static DbDataReader GetNewRowDataReader(DbColumn[] columns, bool includeIdentity)
        {
            object[][] rows = includeIdentity
                ? new[] {new object[] {"id", "q", "q", "q"}}
                : new[] {new object[] {"q", "q", "q"}};
            var testResultSet = new TestResultSet(columns, rows);
            return new TestDbDataReader(new [] {testResultSet});
        }

        public static void AddCells(RowEditBase rc, bool includeIdentity)
        {
            // Skip the first column since if identity, since identity columns can't be updated
            int start = includeIdentity ? 1 : 0;
            for (int i = start; i < rc.AssociatedResultSet.Columns.Length; i++)
            {
                rc.SetCell(i, "123");
            }
        }

        public static IEditTableMetadata GetMetadataProvider(EditColumnWrapper[] columnMetas, bool allKeys = false, bool isMemoryOptimized = false)
        {
            // Create a table metadata provider
            var tableMetaMock = new Mock<IEditTableMetadata>();
            if (allKeys)
            {
                // All columns should be returned as "keys"
                tableMetaMock.Setup(m => m.KeyColumns).Returns(columnMetas);
            }
            else
            {
                // All identity columns should be returned as keys
                tableMetaMock.Setup(m => m.KeyColumns).Returns(columnMetas.Where(c => c.DbColumn.IsIdentity.HasTrue()));
            }
            tableMetaMock.Setup(m => m.Columns).Returns(columnMetas);
            tableMetaMock.Setup(m => m.IsMemoryOptimized).Returns(isMemoryOptimized);
            tableMetaMock.Setup(m => m.EscapedMultipartName).Returns("tbl");

            return tableMetaMock.Object;
        }
    }
}
