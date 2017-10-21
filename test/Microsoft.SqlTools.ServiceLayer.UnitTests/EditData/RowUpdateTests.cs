//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
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
    public class RowUpdateTests
    {
        [Fact]
        public void RowUpdateConstruction()
        {
            // Setup: Create the values to store
            const long rowId = 0;
            ResultSet rs = QueryExecution.Common.GetBasicExecutedBatch().ResultSets[0];
            EditTableMetadata etm = Common.GetStandardMetadata(rs.Columns);

            // If: I create a RowUpdate instance
            RowUpdate rc = new RowUpdate(rowId, rs, etm);

            // Then: The values I provided should be available
            Assert.Equal(rowId, rc.RowId);
            Assert.Equal(rs, rc.AssociatedResultSet);
            Assert.Equal(etm, rc.AssociatedObjectMetadata);
        }

        [Fact]
        public async Task SetCell()
        {
            // Setup: Create a row update
            RowUpdate ru = await GetStandardRowUpdate();

            // If: I set a cell that can be updated
            EditUpdateCellResult eucr = ru.SetCell(0, "col1");

            // Then:
            // ... A edit cell was returned
            Assert.NotNull(eucr);
            Assert.NotNull(eucr.Cell);

            // ... The new value we provided should be returned
            Assert.Equal("col1", eucr.Cell.DisplayValue);
            Assert.False(eucr.Cell.IsNull);

            // ... The row is still dirty
            Assert.True(eucr.IsRowDirty);

            // ... The cell should be dirty
            Assert.True(eucr.Cell.IsDirty);

            // ... There should be a cell update in the cell list
            Assert.Contains(0, ru.cellUpdates.Keys);
            Assert.NotNull(ru.cellUpdates[0]);
        }

        [Fact]
        public void SetCellHasCorrections()
        {
            // Setup: 
            // ... Generate a result set with a single binary column
            DbColumn[] cols =
            {
                new TestDbColumn
                {
                    DataType = typeof(byte[]),
                    DataTypeName = "binary"
                }
            };
            object[][] rows = { new object[]{new byte[] {0x00}}};
            var testResultSet = new TestResultSet(cols, rows);
            var testReader = new TestDbDataReader(new[] { testResultSet }, false);
            var rs = new ResultSet(0, 0, MemoryFileSystem.GetFileStreamFactory());
            rs.ReadResultToEnd(testReader, CancellationToken.None).Wait();

            // ... Generate the metadata
            var etm = Common.GetStandardMetadata(cols);

            // ... Create the row update
            RowUpdate ru = new RowUpdate(0, rs, etm);

            // If: I set a cell in the newly created row to something that will be corrected
            EditUpdateCellResult eucr = ru.SetCell(0, "1000");

            // Then:
            // ... A edit cell was returned
            Assert.NotNull(eucr);
            Assert.NotNull(eucr.Cell);

            // ... The value we used won't be returned
            Assert.NotEmpty(eucr.Cell.DisplayValue);
            Assert.NotEqual("1000", eucr.Cell.DisplayValue);
            Assert.False(eucr.Cell.IsNull);

            // ... The cell should be dirty
            Assert.True(eucr.Cell.IsDirty);

            // ... The row is still dirty
            Assert.True(eucr.IsRowDirty);

            // ... There should be a cell update in the cell list
            Assert.Contains(0, ru.cellUpdates.Keys);
            Assert.NotNull(ru.cellUpdates[0]);
        }

        [Fact]
        public async Task SetCellImplicitRevertTest()
        {
            // Setup: Create a fake table to update
            DbColumn[] columns = Common.GetColumns(true);
            ResultSet rs = await Common.GetResultSet(columns, true);
            EditTableMetadata etm = Common.GetStandardMetadata(columns);

            // If: 
            // ... I add updates to all the cells in the row
            RowUpdate ru = new RowUpdate(0, rs, etm);
            Common.AddCells(ru, true);

            // ... Then I update a cell back to it's old value
            var eucr = ru.SetCell(1, (string) rs.GetRow(0)[1].RawObject);

            // Then:
            // ... A edit cell was returned
            Assert.NotNull(eucr);
            Assert.NotNull(eucr.Cell);

            // ... The new value we provided should be returned
            Assert.Equal(rs.GetRow(0)[1].DisplayValue, eucr.Cell.DisplayValue);
            Assert.False(eucr.Cell.IsNull);

            // ... The cell should be clean
            Assert.False(eucr.Cell.IsDirty);

            // ... The row is still dirty
            Assert.True(eucr.IsRowDirty);

            // ... It should be formatted as an update script
            Regex r = new Regex(@"UPDATE .+ SET (.*) WHERE");
            var m = r.Match(ru.GetScript());

            // ... It should have 2 updates
            string updates = m.Groups[1].Value;
            string[] updateSplit = updates.Split(',');
            Assert.Equal(2, updateSplit.Length);
            Assert.All(updateSplit, s => Assert.Equal(2, s.Split('=').Length));
        }

        public async Task SetCellImplicitRowRevertTests()
        {
            // Setup: Create a fake column to update
            DbColumn[] columns = Common.GetColumns(true);
            ResultSet rs = await Common.GetResultSet(columns, true);
            EditTableMetadata etm = Common.GetStandardMetadata(columns);

            // If:
            // ... I add updates to one cell in the row
            RowUpdate ru = new RowUpdate(0, rs, etm);
            ru.SetCell(1, "qqq");

            // ... Then I update the cell to its original value
            var eucr = ru.SetCell(1, (string) rs.GetRow(0)[1].RawObject);

            // Then:
            // ... An edit cell should have been returned
            Assert.NotNull(eucr);
            Assert.NotNull(eucr.Cell);

            // ... The old value should be returned
            Assert.Equal(rs.GetRow(0)[1].DisplayValue, eucr.Cell.DisplayValue);
            Assert.False(eucr.Cell.IsNull);

            // ... The cell should be clean
            Assert.False(eucr.Cell.IsDirty);

            // ... The row should be clean
            Assert.False(eucr.IsRowDirty);

            // TODO: Make sure that the script and command things will return null
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetScriptTest(bool isMemoryOptimized)
        {
            // Setup: Create a fake table to update
            DbColumn[] columns = Common.GetColumns(true);
            ResultSet rs = await Common.GetResultSet(columns, true);
            EditTableMetadata etm = Common.GetStandardMetadata(columns, isMemoryOptimized);

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
        public async Task GetCommand(bool includeIdentity, bool isMemoryOptimized)
        {
            // Setup: 
            // ... Create a row update with cell updates
            var columns = Common.GetColumns(includeIdentity);
            var rs = await Common.GetResultSet(columns, includeIdentity);
            var etm = Common.GetStandardMetadata(columns, isMemoryOptimized);
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
        public async Task GetCommandNullConnection()
        {
            // Setup: Create a row update
            RowUpdate ru = await GetStandardRowUpdate();

            // If: I attempt to create a command with a null connection
            // Then: It should throw an exception
            Assert.Throws<ArgumentNullException>(() => ru.GetCommand(null));
        }

        [Fact]
        public async Task GetEditRow()
        {
            // Setup: Create a row update with a cell set
            var columns = Common.GetColumns(false);
            var rs = await Common.GetResultSet(columns, false);
            var etm = Common.GetStandardMetadata(columns);
            RowUpdate ru = new RowUpdate(0, rs, etm);
            ru.SetCell(0, "foo");

            // If: I attempt to get an edit row
            DbCellValue[] cells = rs.GetRow(0).ToArray();
            EditRow er = ru.GetEditRow(cells);

            // Then:
            // ... The state should be dirty
            Assert.True(er.IsDirty);
            Assert.Equal(EditRow.EditRowState.DirtyUpdate, er.State);

            // ... The ID should be the same as the one provided
            Assert.Equal(0, er.Id);

            // ... The row should match the cells that were given, except for the updated cell
            Assert.Equal(cells.Length, er.Cells.Length);
            for (int i = 1; i < cells.Length; i++)
            {
                DbCellValue originalCell = cells[i];
                DbCellValue outputCell = er.Cells[i];

                Assert.Equal(originalCell.DisplayValue, outputCell.DisplayValue);
                Assert.Equal(originalCell.IsNull, outputCell.IsNull);
                // Note: No real need to check the RawObject property
            }

            // ... The updated cell should match what it was set to and be dirty
            EditCell newCell = er.Cells[0];
            Assert.Equal("foo", newCell.DisplayValue);
            Assert.False(newCell.IsNull);
            Assert.True(newCell.IsDirty);
        }

        [Fact]
        public async Task GetEditNullRow()
        {
            // Setup: Create a row update
            RowUpdate ru = await GetStandardRowUpdate();

            // If: I attempt to get an edit row with a null cached row
            // Then: I should get an exception
            Assert.Throws<ArgumentNullException>(() => ru.GetEditRow(null));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ApplyChanges(bool includeIdentity)
        {
            // Setup: 
            // ... Create a row update (no cell updates needed)
            var columns = Common.GetColumns(includeIdentity);
            var rs = await Common.GetResultSet(columns, includeIdentity);
            var etm = Common.GetStandardMetadata(columns);
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
            var rs = await Common.GetResultSet(columns, true);
            var etm = Common.GetStandardMetadata(columns);
            RowUpdate ru = new RowUpdate(0, rs, etm);

            // If: I  ask for the changes to be applied with a null db reader
            // Then: I should get an exception
            await Assert.ThrowsAsync<ArgumentNullException>(() => ru.ApplyChanges(null));
        }

        [Theory]
        [InlineData(-1)]        // Negative
        [InlineData(3)]         // At edge of acceptable values
        [InlineData(100)]       // Way too large value
        public async Task RevertCellOutOfRange(int columnId)
        {
            // Setup: 
            // ... Create a row update (no cell updates needed)
            var columns = Common.GetColumns(false);
            var rs = await Common.GetResultSet(columns, false);
            var etm = Common.GetStandardMetadata(columns);
            RowUpdate ru = new RowUpdate(0, rs, etm);

            // If: I attempt to revert a cell that is out of range
            // Then: I should get an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => ru.RevertCell(columnId));
        }

        [Fact]
        public async Task RevertCellNotSet()
        {
            // Setup: 
            // ... Create a row update (no cell updates needed)
            var columns = Common.GetColumns(true);
            var rs = await Common.GetResultSet(columns, true);
            var etm = Common.GetStandardMetadata(columns);
            RowUpdate ru = new RowUpdate(0, rs, etm);

            // If: I attempt to revert a cell that has not been set
            EditRevertCellResult result = ru.RevertCell(0);

            // Then:
            // ... We should get a result back
            Assert.NotNull(result);

            // ... We should get the original value back
            // @TODO: Check for a default value when we support it
            Assert.NotNull(result.Cell);
            Assert.Equal(rs.GetRow(0)[0].DisplayValue, result.Cell.DisplayValue);

            // ... The row should be clean
            Assert.False(result.IsRowDirty);

            // ... The cell should no longer be set
            Assert.DoesNotContain(0, ru.cellUpdates.Keys);
        }

        [Fact]
        public async Task RevertCellThatWasSet()
        {
            // Setup: 
            // ... Create a row update
            var columns = Common.GetColumns(false);
            var rs = await Common.GetResultSet(columns, false);
            var etm = Common.GetStandardMetadata(columns);
            RowUpdate ru = new RowUpdate(0, rs, etm);
            ru.SetCell(0, "qqq");
            ru.SetCell(1, "qqq");

            // If: I attempt to revert a cell that was set
            EditRevertCellResult result = ru.RevertCell(0);

            // Then:
            // ... We should get a result back
            Assert.NotNull(result);

            // ... We should get the original value back
            // @TODO: Check for a default value when we support it
            Assert.NotNull(result.Cell);
            Assert.Equal(rs.GetRow(0)[0].DisplayValue, result.Cell.DisplayValue);

            // ... The row should be dirty still
            Assert.True(result.IsRowDirty);

            // ... The cell should no longer be set
            Assert.DoesNotContain(0, ru.cellUpdates.Keys);
        }

        [Fact]
        public async Task RevertCellRevertsRow()
        {
            // Setup:
            // ... Create a row update
            var columns = Common.GetColumns(false);
            var rs = await Common.GetResultSet(columns, false);
            var etm = Common.GetStandardMetadata(columns);
            RowUpdate ru = new RowUpdate(0, rs, etm);
            ru.SetCell(0, "qqq");

            // If: I attempt to revert a cell that was set
            EditRevertCellResult result = ru.RevertCell(0);

            // Then:
            // ... We should get a result back
            Assert.NotNull(result);

            // ... We should get the original value back
            // @TODO: Check for a default value when we support it
            Assert.NotNull(result.Cell);
            Assert.Equal(rs.GetRow(0)[0].DisplayValue, result.Cell.DisplayValue);

            // ... The row should now be reverted
            Assert.False(result.IsRowDirty);

            // ... The cell should no longer be set
            Assert.DoesNotContain(0, ru.cellUpdates.Keys);
        }

        private async Task<RowUpdate> GetStandardRowUpdate()
        {
            var columns = Common.GetColumns(false);
            var rs = await Common.GetResultSet(columns, false);
            var etm = Common.GetStandardMetadata(columns);
            return new RowUpdate(0, rs, etm);
        }
    }
}
