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
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.EditData
{
    public class RowEditBaseTests
    {
        [Theory]
        [InlineData(-1)]        // Negative index
        [InlineData(100)]       // Index larger than number of columns
        public void ValidateUpdatableColumnOutOfRange(int columnId)
        {
            // Setup: Create a result set
            ResultSet rs = GetResultSet(
                new DbColumn[] { new TestDbColumn("id", true), new TestDbColumn("col1")}, 
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
                new DbColumn[] { new TestDbColumn("id", true), new TestDbColumn("col1") },
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
            IEditTableMetadata etm = Common.GetMetadata(cols);

            RowEditTester rt = new RowEditTester(rs, etm);
            rt.ValidateWhereClauseSingleKey(nullClause);
        }

        public static IEnumerable<object> GetWhereClauseIsNotNullData
        {
            get
            {
                yield return new object[] {new TestDbColumn("col"), DBNull.Value, "IS NULL"};
                yield return new object[] {new TestDbColumn("col", "VARBINARY", typeof(byte[])), new byte[5], "IS NOT NULL"};
                yield return new object[] {new TestDbColumn("col", "TEXT", typeof(string)), "abc", "IS NOT NULL"};
                yield return new object[] {new TestDbColumn("col", "NTEXT", typeof(string)), "abc", "IS NOT NULL"};
            }
        }

        [Fact]
        public void GetWhereClauseMultipleKeyColumns()
        {
            // Setup: Create a result set and metadata provider with multiple key columns
            DbColumn[] cols = {new TestDbColumn("col1"), new TestDbColumn("col2")};
            ResultSet rs = GetResultSet(cols, new object[] {"abc", "def"});
            IEditTableMetadata etm = Common.GetMetadata(cols);

            RowEditTester rt = new RowEditTester(rs, etm);
            rt.ValidateWhereClauseMultipleKeys();
        }

        [Fact]
        public void GetWhereClauseNoKeyColumns()
        {
            // Setup: Create a result set and metadata provider with no key columns
            DbColumn[] cols = {new TestDbColumn("col1"), new TestDbColumn("col2")};
            ResultSet rs = GetResultSet(cols, new object[] {"abc", "def"});
            IEditTableMetadata etm = Common.GetMetadata(new DbColumn[] {});

            RowEditTester rt = new RowEditTester(rs, etm);
            rt.ValidateWhereClauseNoKeys();
        }

        private static ResultSet GetResultSet(DbColumn[] columns, object[] row)
        {
            object[][] rows = {row};
            var testResultSet = new TestResultSet(columns, rows);
            var testReader = new TestDbDataReader(new [] {testResultSet});
            var resultSet = new ResultSet(testReader, 0,0, QueryExecution.Common.GetFileStreamFactory(new Dictionary<string, byte[]>()));
            resultSet.ReadResultToEnd(CancellationToken.None).Wait();
            return resultSet;
        }

        private class RowEditTester : RowEditBase
        {
            public RowEditTester(ResultSet rs, IEditTableMetadata meta) : base(0, rs, meta) { }

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

            public override EditUpdateCellResult SetCell(int columnId, string newValue)
            {
                throw new NotImplementedException();
            }
        }
    }
}
