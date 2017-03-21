//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    public class RowEditBaseTests
    {
        [Theory]
        [InlineData(-1)]        // Negative index
        [InlineData(2)]         // Equal to count of columns
        [InlineData(100)]       // Index larger than number of columns
        public void ValidateUpdatableColumnOutOfRange(int columnId)
        {
            // Setup: Create a result set
            ResultSet rs = GetResultSet(
                new DbColumn[] {
                    new TestDbColumn("id") {IsKey = true, IsAutoIncrement = true, IsIdentity = true},
                    new TestDbColumn("col1")
                },
                new object[] { "id", "1" });

            // If: I validate a column ID that is out of range
            // Then: It should throw
            RowEditTester tester = new RowEditTester(rs, null);
            Assert.Throws<ArgumentOutOfRangeException>(() => tester.ValidateColumn(columnId));
        }

        [Fact]
        public void ValidateUpdatableColumnNotUpdatable()
        {
            // Setup: Create a result set with an identity column
            ResultSet rs = GetResultSet(
                new DbColumn[] {
                    new TestDbColumn("id") {IsKey = true, IsAutoIncrement = true, IsIdentity = true},
                    new TestDbColumn("col1")
                },
                new object[] { "id", "1" });

            // If: I validate a column ID that is not updatable
            // Then: It should throw
            RowEditTester tester = new RowEditTester(rs, null);
            Assert.Throws<InvalidOperationException>(() => tester.ValidateColumn(0));
        }

        [Theory]
        [MemberData(nameof(GetWhereClauseIsNotNullData))]
        public void GetWhereClauseSimple(DbColumn col, object val, string nullClause)
        {
            // Setup: Create a result set and metadata provider with a single column
            var cols = new[] {col};
            ResultSet rs = GetResultSet(cols, new[] {val});
            EditTableMetadata etm = Common.GetStandardMetadata(cols);

            RowEditTester rt = new RowEditTester(rs, etm);
            rt.ValidateWhereClauseSingleKey(nullClause);
        }

        public static IEnumerable<object> GetWhereClauseIsNotNullData
        {
            get
            {
                yield return new object[] {new TestDbColumn("col"), DBNull.Value, "IS NULL"};
                yield return new object[] {
                    new TestDbColumn
                    {
                        DataTypeName = "VARBINARY",
                        DataType = typeof(byte[])
                    },
                    new byte[5],
                    "IS NOT NULL"
                };
                yield return new object[]
                {
                    new TestDbColumn
                    {
                        DataType = typeof(string),
                        DataTypeName = "TEXT"
                    },
                    "abc",
                    "IS NOT NULL"
                };
                yield return new object[]
                {
                    new TestDbColumn
                    {
                        DataType = typeof(string),
                        DataTypeName = "NTEXT",

                    },
                    "abc",
                    "IS NOT NULL"
                };
            }
        }

        [Fact]
        public void GetWhereClauseMultipleKeyColumns()
        {
            // Setup: Create a result set and metadata provider with multiple key columns
            DbColumn[] cols = {new TestDbColumn("col1"), new TestDbColumn("col2")};
            ResultSet rs = GetResultSet(cols, new object[] {"abc", "def"});
            EditTableMetadata etm = Common.GetStandardMetadata(cols);

            RowEditTester rt = new RowEditTester(rs, etm);
            rt.ValidateWhereClauseMultipleKeys();
        }

        [Fact]
        public void GetWhereClauseNoKeyColumns()
        {
            // Setup: Create a result set and metadata provider with no key columns
            DbColumn[] cols = {new TestDbColumn("col1"), new TestDbColumn("col2")};
            ResultSet rs = GetResultSet(cols, new object[] {"abc", "def"});
            EditTableMetadata etm = Common.GetStandardMetadata(new DbColumn[] {});

            RowEditTester rt = new RowEditTester(rs, etm);
            rt.ValidateWhereClauseNoKeys();
        }

        [Fact]
        public void SortingByTypeTest()
        {
            // Setup: Create a result set and metadata we can reuse
            var cols = Common.GetColumns(false);
            var rs = Common.GetResultSet(cols, false);
            var etm = Common.GetStandardMetadata(cols);

            // If: I request to sort a list of the three different edit operations
            List<RowEditBase> rowEdits = new List<RowEditBase>
            {
                new RowDelete(0, rs, etm),
                new RowUpdate(0, rs, etm),
                new RowCreate(0, rs, etm)
            };
            rowEdits.Sort();

            // Then: Delete should be the last operation to execute
            //       (we don't care about the order of the other two)
            Assert.IsType<RowDelete>(rowEdits.Last());
        }

        [Fact]
        public void SortingUpdatesByRowIdTest()
        {
            // Setup: Create a result set and metadata we can reuse
            var cols = Common.GetColumns(false);
            var rs = Common.GetResultSet(cols, false, 4);
            var etm = Common.GetStandardMetadata(cols);

            // If: I sort 3 edit operations of the same type
            List<RowEditBase> rowEdits = new List<RowEditBase>
            {
                new RowUpdate(3, rs, etm),
                new RowUpdate(1, rs, etm),
                new RowUpdate(2, rs, etm)
            };
            rowEdits.Sort();

            // Then: They should be in order by row ID ASCENDING
            Assert.Equal(1, rowEdits[0].RowId);
            Assert.Equal(2, rowEdits[1].RowId);
            Assert.Equal(3, rowEdits[2].RowId);
        }

        [Fact]
        public void SortingCreatesByRowIdTest()
        {
            // Setup: Create a result set and metadata we can reuse
            var cols = Common.GetColumns(false);
            var rs = Common.GetResultSet(cols, false);
            var etm = Common.GetStandardMetadata(cols);

            // If: I sort 3 edit operations of the same type
            List<RowEditBase> rowEdits = new List<RowEditBase>
            {
                new RowCreate(3, rs, etm),
                new RowCreate(1, rs, etm),
                new RowCreate(2, rs, etm)
            };
            rowEdits.Sort();

            // Then: They should be in order by row ID ASCENDING
            Assert.Equal(1, rowEdits[0].RowId);
            Assert.Equal(2, rowEdits[1].RowId);
            Assert.Equal(3, rowEdits[2].RowId);
        }

        [Fact]
        public void SortingDeletesByRowIdTest()
        {
            // Setup: Create a result set and metadata we can reuse
            var cols = Common.GetColumns(false);
            var rs = Common.GetResultSet(cols, false);
            var etm = Common.GetStandardMetadata(cols);

            // If: I sort 3 delete operations of the same type
            List<RowEditBase> rowEdits = new List<RowEditBase>
            {
                new RowDelete(1, rs, etm),
                new RowDelete(3, rs, etm),
                new RowDelete(2, rs, etm)
            };
            rowEdits.Sort();

            // Then: They should be in order by row ID DESCENDING
            Assert.Equal(3, rowEdits[0].RowId);
            Assert.Equal(2, rowEdits[1].RowId);
            Assert.Equal(1, rowEdits[2].RowId);
        }

        private static ResultSet GetResultSet(DbColumn[] columns, object[] row)
        {
            object[][] rows = {row};
            var testResultSet = new TestResultSet(columns, rows);
            var testReader = new TestDbDataReader(new [] {testResultSet});
            var resultSet = new ResultSet(0,0, MemoryFileSystem.GetFileStreamFactory());
            resultSet.ReadResultToEnd(testReader, CancellationToken.None).Wait();
            return resultSet;
        }

        private class RowEditTester : RowEditBase
        {
            public RowEditTester(ResultSet rs, EditTableMetadata meta) : base(0, rs, meta) { }

            public void ValidateColumn(int columnId)
            {
                ValidateColumnIsUpdatable(columnId);
            }

            // ReSharper disable once UnusedParameter.Local
            public void ValidateWhereClauseSingleKey(string nullValue)
            {
                // If: I generate a where clause with one is null column value
                WhereClause wc = GetWhereClause(false);
                
                // Then:
                // ... There should only be one component
                Assert.Equal(1, wc.ClauseComponents.Count);
                
                // ... Parameterization should be empty
                Assert.Empty(wc.Parameters);

                // ... The component should contain the name of the column and be null
                Assert.Equal(
                    $"({AssociatedObjectMetadata.Columns.First().EscapedName} {nullValue})",
                    wc.ClauseComponents[0]);

                // ... The complete clause should contain a single WHERE
                Assert.Equal($"WHERE {wc.ClauseComponents[0]}", wc.CommandText);
            }

            public void ValidateWhereClauseMultipleKeys()
            {
                // If: I generate a where clause with multiple key columns
                WhereClause wc = GetWhereClause(false);

                // Then:
                // ... There should two components
                var keys = AssociatedObjectMetadata.KeyColumns.ToArray();
                Assert.Equal(keys.Length, wc.ClauseComponents.Count);

                // ... Parameterization should be empty
                Assert.Empty(wc.Parameters);

                // ... The components should contain the name of the column and the value
                Regex r = new Regex(@"\([0-9a-z]+ = .+\)");
                Assert.All(wc.ClauseComponents, s => Assert.True(r.IsMatch(s)));

                // ... The complete clause should contain multiple cause components joined
                //     with and
                Assert.True(wc.CommandText.StartsWith("WHERE "));
                Assert.True(wc.CommandText.EndsWith(string.Join(" AND ", wc.ClauseComponents)));
            }

            public void ValidateWhereClauseNoKeys()
            {
                // If: I generate a where clause from metadata that doesn't have keys
                // Then: An exception should be thrown
                Assert.Throws<InvalidOperationException>(() => GetWhereClause(false));
            }

            public override string GetScript()
            {
                throw new NotImplementedException();
            }

            public override EditRow GetEditRow(DbCellValue[] cells)
            {
                throw new NotImplementedException();
            }

            public override string RevertCell(int columnId)
            {
                throw new NotImplementedException();
            }

            public override EditUpdateCellResult SetCell(int columnId, string newValue)
            {
                throw new NotImplementedException();
            }

            public override Task ApplyChanges(DbDataReader reader)
            {
                throw new NotImplementedException();
            }

            public override DbCommand GetCommand(DbConnection conn)
            {
                throw new NotImplementedException();
            }

            protected override int SortId => 0;
        }
    }
}
