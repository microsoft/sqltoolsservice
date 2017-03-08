//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    public class RowDeleteTests
    {
        [Fact]
        public void RowDeleteConstruction()
        {
            // Setup: Create the values to store
            const long rowId = 100;
            ResultSet rs = QueryExecution.Common.GetBasicExecutedBatch().ResultSets[0];
            IEditTableMetadata etm = Common.GetStandardMetadata(rs.Columns);

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
        public void GetScriptTest(bool isMemoryOptimized)
        {
            DbColumn[] columns = Common.GetColumns(true);
            ResultSet rs = Common.GetResultSet(columns, true);
            IEditTableMetadata etm = Common.GetStandardMetadata(columns, false, isMemoryOptimized);

            // If: I ask for a script to be generated for delete
            RowDelete rd = new RowDelete(0, rs, etm);
            string script = rd.GetScript();

            // Then:
            // ... The script should not be null
            Assert.NotNull(script);

            // ... It should be formatted as a delete script
            string scriptStart = $"DELETE FROM {etm.EscapedMultipartName}";
            if (isMemoryOptimized)
            {
                scriptStart += " WITH(SNAPSHOT)";
            }
            Assert.StartsWith(scriptStart, script);
        }

        [Fact]
        public async Task ApplyChanges()
        {
            // Setup: Generate the parameters for the row delete object
            //        We don't care about the values besides the row ID
            const long rowId = 0;
            var columns = Common.GetColumns(false);
            var rs = Common.GetResultSet(columns, false);
            var etm = Common.GetStandardMetadata(columns);

            // If: I ask for the change to be applied
            RowDelete rd = new RowDelete(rowId, rs, etm);
            await rd.ApplyChanges(null);      // Reader not used, can be null

            // Then : The result set should have one less row in it
            Assert.Equal(0, rs.RowCount);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public void GetCommand(bool includeIdentity, bool isMemoryOptimized)
        {
            // Setup:
            // ... Create a row delete
            const long rowId = 0;
            var columns = Common.GetColumns(includeIdentity);
            var rs = Common.GetResultSet(columns, includeIdentity);
            var etm = Common.GetStandardMetadata(columns, !includeIdentity, isMemoryOptimized);
            RowDelete rd = new RowDelete(rowId, rs, etm);

            // ... Mock db connection for building the command
            var mockConn = new TestSqlConnection(null);

            // If: I attempt to get a command for the edit
            DbCommand cmd = rd.GetCommand(mockConn);

            // Then:
            // ... The command should not be null
            Assert.NotNull(cmd);

            // ... Only the keys should be used for parameters
            int expectedKeys = includeIdentity ? 1 : 3;
            Assert.Equal(expectedKeys, cmd.Parameters.Count);

            // ... It should be formatted into an delete script
            string regexTest = isMemoryOptimized
                ? @"DELETE FROM (.+) WITH\(SNAPSHOT\) WHERE (.+)"
                : @"DELETE FROM (.+) WHERE (.+)";
            Regex r = new Regex(regexTest);
            var m = r.Match(cmd.CommandText);
            Assert.True(m.Success);

            // ... There should be a table
            string tbl = m.Groups[1].Value;
            Assert.Equal(etm.EscapedMultipartName, tbl);

            // ... There should be as many where components as there are keys
            string[] whereComponents = m.Groups[2].Value.Split(new[] {"AND"}, StringSplitOptions.None);
            Assert.Equal(expectedKeys, whereComponents.Length);

            // ... Each component should have be equal to a parameter
            Assert.All(whereComponents, c => Assert.True(Regex.IsMatch(c.Trim(), @"\(.+ = @.+\)")));
        }

        [Fact]
        public void GetCommandNullConnection()
        {
            // Setup: Create a row delete
            var columns = Common.GetColumns(false);
            var rs = Common.GetResultSet(columns, false);
            var etm = Common.GetStandardMetadata(columns);
            RowDelete rd = new RowDelete(0, rs, etm);

            // If: I attempt to create a command with a null connection
            // Then: It should throw an exception
            Assert.Throws<ArgumentNullException>(() => rd.GetCommand(null));
        }

        [Fact]
        public void SetCell()
        {
            DbColumn[] columns = Common.GetColumns(true);
            ResultSet rs = Common.GetResultSet(columns, true);
            IEditTableMetadata etm = Common.GetStandardMetadata(columns, false);

            // If: I set a cell on a delete row edit
            // Then: It should throw as invalid operation
            RowDelete rd = new RowDelete(0, rs, etm);
            Assert.Throws<InvalidOperationException>(() => rd.SetCell(0, null));
        }

        [Fact]
        public void RevertCell()
        {
            // Setup: Create a row delete
            DbColumn[] cols = Common.GetColumns(false);
            ResultSet rs = Common.GetResultSet(cols, false);
            IEditTableMetadata etm = Common.GetStandardMetadata(cols);
            RowDelete rd = new RowDelete(0, rs, etm);

            // If: I revert a cell on a delete row edit
            // Then: It should throw
            Assert.Throws<InvalidOperationException>(() => rd.RevertCell(0));
        }
    }
}
