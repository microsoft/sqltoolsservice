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
    public class RowUpdateTests
    {
        [Fact]
        public void RowUpdateConstruction()
        {
            // Setup: Create the values to store
            const long rowId = 0;
            ResultSet rs = QueryExecution.Common.GetBasicExecutedBatch().ResultSets[0];
            IEditTableMetadata etm = Common.GetStandardMetadata(rs.Columns);

            // If: I create a RowUpdate instance
            RowUpdate rc = new RowUpdate(rowId, rs, etm);

            // Then: The values I provided should be available
            Assert.Equal(rowId, rc.RowId);
            Assert.Equal(rs, rc.AssociatedResultSet);
            Assert.Equal(etm, rc.AssociatedObjectMetadata);
        }

        [Fact]
        public void ImplicitRevertTest()
        {
            // Setup: Create a fake table to update
            DbColumn[] columns = Common.GetColumns(true);
            ResultSet rs = Common.GetResultSet(columns, true);
            IEditTableMetadata etm = Common.GetStandardMetadata(columns);

            // If: 
            // ... I add updates to all the cells in the row
            RowUpdate ru = new RowUpdate(0, rs, etm);
            Common.AddCells(ru, true);

            // ... Then I update a cell back to it's old value
            var output = ru.SetCell(1, (string) rs.GetRow(0)[1].RawObject);

            // Then:
            // ... The output should indicate a revert
            Assert.NotNull(output);
            Assert.True(output.IsRevert);
            Assert.False(output.HasCorrections);
            Assert.False(output.IsNull);
            Assert.Equal(rs.GetRow(0)[1].DisplayValue, output.NewValue);

            // ... It should be formatted as an update script
            Regex r = new Regex(@"UPDATE .+ SET (.*) WHERE");
            var m = r.Match(ru.GetScript());

            // ... It should have 2 updates
            string updates = m.Groups[1].Value;
            string[] updateSplit = updates.Split(',');
            Assert.Equal(2, updateSplit.Length);
            Assert.All(updateSplit, s => Assert.Equal(2, s.Split('=').Length));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetScriptTest(bool isMemoryOptimized)
        {
            // Setup: Create a fake table to update
            DbColumn[] columns = Common.GetColumns(true);
            ResultSet rs = Common.GetResultSet(columns, true);
            IEditTableMetadata etm = Common.GetStandardMetadata(columns, false, isMemoryOptimized);

            // If: I ask for a script to be generated for update
            RowUpdate ru = new RowUpdate(0, rs, etm);
            Common.AddCells(ru, true);
            string script = ru.GetScript();

            // Then:
            // ... The script should not be null
            Assert.NotNull(script);

            // ... It should be formatted as an update script
            string regexString = isMemoryOptimized
                ? @"UPDATE (.+) WITH \(SNAPSHOT\) SET (.*) WHERE .+"
                : @"UPDATE (.+) SET (.*) WHERE .+";
            Regex r = new Regex(regexString);
            var m = r.Match(script);
            Assert.True(m.Success);

            // ... It should have 3 updates
            string tbl = m.Groups[1].Value;
            string updates = m.Groups[2].Value;
            string[] updateSplit = updates.Split(',');
            Assert.Equal(etm.EscapedMultipartName, tbl);
            Assert.Equal(3, updateSplit.Length);
            Assert.All(updateSplit, s => Assert.Equal(2, s.Split('=').Length));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void GetCommand(bool includeIdentity, bool isMemoryOptimized)
        {
            // Setup: 
            // ... Create a row update with cell updates
            var columns = Common.GetColumns(includeIdentity);
            var rs = Common.GetResultSet(columns, includeIdentity);
            var etm = Common.GetStandardMetadata(columns, !includeIdentity, isMemoryOptimized);
            RowUpdate ru = new RowUpdate(0, rs, etm);
            Common.AddCells(ru, includeIdentity);

            // ... Mock db connection for building the command
            var mockConn = new TestSqlConnection(null);

            // If: I ask for a command to be generated for update
            DbCommand cmd = ru.GetCommand(mockConn);

            // Then:
            // ... The command should not be null
            Assert.NotNull(cmd);

            // ... There should be an appropriate number of parameters in it
            //     (1 or 3 keys, 3 value parameters)
            int expectedKeys = includeIdentity ? 1 : 3;
            Assert.Equal(expectedKeys + 3, cmd.Parameters.Count);

            // ... It should be formatted into an update script with output
            string regexFormat = isMemoryOptimized
                ? @"UPDATE (.+) WITH \(SNAPSHOT\) SET (.+) OUTPUT (.+) WHERE (.+)"
                : @"UPDATE (.+) SET (.+) OUTPUT(.+) WHERE (.+)";
            Regex r = new Regex(regexFormat);
            var m = r.Match(cmd.CommandText);
            Assert.True(m.Success);

            // ... There should be a table
            string tbl = m.Groups[1].Value;
            Assert.Equal(etm.EscapedMultipartName, tbl);

            // ... There should be 3 parameters for input
            string[] inCols = m.Groups[2].Value.Split(',');
            Assert.Equal(3, inCols.Length);
            Assert.All(inCols, s => Assert.Matches(@"\[.+\] = @Value\d+", s));

            // ... There should be 3 OR 4 columns for output
            string[] outCols = m.Groups[3].Value.Split(',');
            Assert.Equal(includeIdentity ? 4 : 3, outCols.Length);
            Assert.All(outCols, s => Assert.StartsWith("inserted.", s.Trim()));

            // ... There should be 1 OR 3 columns for where components
            string[] whereComponents = m.Groups[4].Value.Split(new[] {"AND"}, StringSplitOptions.None);
            Assert.Equal(expectedKeys, whereComponents.Length);
            Assert.All(whereComponents, s => Assert.Matches(@"\(.+ = @Param\d+\)", s));
        }

        [Fact]
        public void GetCommandNullConnection()
        {
            // Setup: Create a row create
            var columns = Common.GetColumns(false);
            var rs = Common.GetResultSet(columns, false);
            var etm = Common.GetStandardMetadata(columns);
            RowUpdate rc = new RowUpdate(0, rs, etm);

            // If: I attempt to create a command with a null connection
            // Then: It should throw an exception
            Assert.Throws<ArgumentNullException>(() => rc.GetCommand(null));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ApplyChanges(bool includeIdentity)
        {
            // Setup: 
            // ... Create a row update (no cell updates needed)
            var columns = Common.GetColumns(includeIdentity);
            var rs = Common.GetResultSet(columns, includeIdentity);
            var etm = Common.GetStandardMetadata(columns, !includeIdentity);
            RowUpdate ru = new RowUpdate(0, rs, etm);
            long oldBytesWritten = rs.totalBytesWritten;

            // ... Setup a db reader for the result of an update
            var newRowReader = Common.GetNewRowDataReader(columns, includeIdentity);

            // If: I ask for the change to be applied
            await ru.ApplyChanges(newRowReader);

            // Then: 
            // ... The result set should have the same number of rows as before
            Assert.Equal(1, rs.RowCount);
            Assert.True(oldBytesWritten < rs.totalBytesWritten);
        }

        [Fact]
        public async Task ApplyChangesNullReader()
        {
            // Setup: 
            // ... Create a row update (no cell updates needed)
            var columns = Common.GetColumns(true);
            var rs = Common.GetResultSet(columns, true);
            var etm = Common.GetStandardMetadata(columns, false);
            RowUpdate ru = new RowUpdate(0, rs, etm);

            // If: I  ask for the changes to be applied with a null db reader
            // Then: I should get an exception
            await Assert.ThrowsAsync<ArgumentNullException>(() => ru.ApplyChanges(null));
        }
    }
}
