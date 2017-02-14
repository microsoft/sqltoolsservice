//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.EditData
{
    public class RowCreateTests
    {
        [Fact]
        public void RowCreateConstruction()
        {
            // Setup: Create the values to store
            const long rowId = 100;
            ResultSet rs = QueryExecution.Common.GetBasicExecutedBatch().ResultSets[0];
            IEditTableMetadata etm = GetMetadata();

            // If: I create a RowCreate instance
            RowCreate rc = new RowCreate(rowId, rs, etm);

            // Then: The values I provided should be available
            Assert.Equal(rowId, rc.RowId);
            Assert.Equal(rs, rc.AssociatedResultSet);
            Assert.Equal(etm, rc.AssociatedObjectMetadata);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetScript(bool includeIdentity)
        {
            // Setup: Generate the parameters for the row create
            const long rowId = 100;
            ResultSet rs = GetResultSet(GetColumns(includeIdentity), includeIdentity);
            IEditTableMetadata etm = GetMetadata();

            // If: I ask for a script to be generated without an identity column
            RowCreate rc = new RowCreate(rowId, rs, etm);
            AddCells(rc, includeIdentity);
            string script = rc.GetScript();

            // Then:
            // ... The script should not be null,
            Assert.NotNull(script);

            // ... It should be formatted as an insert script
            Regex r = new Regex(@"INSERT INTO .*\((.*)\) VALUES \((.*)\)");
            var m = r.Match(script);
            Assert.True(m.Success);

            // ... It should have 3 columns and 3 values (regardless of the presence of an identity col)
            string cols = m.Groups[1].Value;
            string vals = m.Groups[2].Value;
            Assert.Equal(3, cols.Split(',').Length);
            Assert.Equal(3, vals.Split(',').Length);
        }

        [Fact]
        public void GetScriptMissingCell()
        {
            // Setup: Generate the parameters for the row create
            const long rowId = 100;
            ResultSet rs = GetResultSet(GetColumns(false), false);
            IEditTableMetadata etm = GetMetadata();

            // If: I ask for a script to be generated without setting any values
            // Then: An exception should be thrown for missing cells
            RowCreate rc = new RowCreate(rowId, rs, etm);
            Assert.Throws<InvalidOperationException>(() => rc.GetScript());
        }

        private static DbColumn[] GetColumns(bool includeIdentity)
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

        private static IEditTableMetadata GetMetadata()
        {
            // NOTE: The RowCreate is special in that it doesn't utilize the IEditTableMetadata
            // therefore, we don't need to properly mock it out
            Mock<IEditTableMetadata> mock = new Mock<IEditTableMetadata>();
            return mock.Object;
        }

        private static ResultSet GetResultSet(DbColumn[] columns, bool includeIdentity)
        {
            object[][] rows = includeIdentity
                ? new[] {new object[] {"id", "1", "2", "3"}}
                : new[] {new object[] {"1", "2", "3"}};
            var testResultSet = new TestResultSet(columns, rows);
            var reader = new TestDbDataReader(new[] {testResultSet});
            var resultSet = new ResultSet(reader, 0, 0, QueryExecution.Common.GetFileStreamFactory(new Dictionary<string, byte[]>()));
            resultSet.ReadResultToEnd(CancellationToken.None).Wait();
            return resultSet;
        }

        private static void AddCells(RowCreate rc, bool includeIdentity)
        {
            // Skip the first column since if identity, since identity columns can't be updated
            int start = includeIdentity ? 1 : 0;
            for(int i = start; i < rc.AssociatedResultSet.Columns.Length; i++)
            {
                rc.SetCell(i, "123");
            }
        }
    }
}
