//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
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
    public class RowCreateTests
    {
        [Fact]
        public async Task RowCreateConstruction()
        {
            // Setup: Create the values to store
            const long rowId = 100;
            DbColumn[] columns = Common.GetColumns(false);
            ResultSet rs = await Common.GetResultSet(columns, false);
            EditTableMetadata etm = Common.GetStandardMetadata(columns);

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
        public async Task GetScript(bool includeIdentity)
        {
            // Setup: Generate the parameters for the row create
            const long rowId = 100;
            DbColumn[] columns = Common.GetColumns(includeIdentity);
            ResultSet rs = await Common.GetResultSet(columns, includeIdentity);
            EditTableMetadata etm = Common.GetStandardMetadata(columns);

            // If: I ask for a script to be generated without an identity column
            RowCreate rc = new RowCreate(rowId, rs, etm);
            Common.AddCells(rc, includeIdentity);
            string script = rc.GetScript();

            // Then:
            // ... The script should not be null,
            Assert.NotNull(script);

            // ... It should be formatted as an insert script
            Regex r = new Regex(@"INSERT INTO (.+)\((.*)\) VALUES \((.*)\)");
            var m = r.Match(script);
            Assert.True(m.Success);

            // ... It should have 3 columns and 3 values (regardless of the presence of an identity col)
            string tbl = m.Groups[1].Value;
            string cols = m.Groups[2].Value;
            string vals = m.Groups[3].Value;
            Assert.Equal(etm.EscapedMultipartName, tbl);
            Assert.Equal(3, cols.Split(',').Length);
            Assert.Equal(3, vals.Split(',').Length);
        }

        [Fact]
        public async Task GetScriptMissingCell()
        {
            // Setup: Generate the parameters for the row create
            RowCreate rc = await GetStandardRowCreate();

            // If: I ask for a script to be generated without setting any values
            // Then: An exception should be thrown for missing cells
            Assert.Throws<InvalidOperationException>(() => rc.GetScript());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ApplyChanges(bool includeIdentity)
        {
            // Setup: 
            // ... Generate the parameters for the row create
            const long rowId = 100;
            DbColumn[] columns = Common.GetColumns(includeIdentity);
            ResultSet rs = await Common.GetResultSet(columns, includeIdentity);
            EditTableMetadata etm = Common.GetStandardMetadata(columns);

            // ... Setup a db reader for the result of an insert
            var newRowReader = Common.GetNewRowDataReader(columns, includeIdentity);

            // If: I ask for the change to be applied
            RowCreate rc = new RowCreate(rowId, rs, etm);
            await rc.ApplyChanges(newRowReader);

            // Then: The result set should have an additional row in it
            Assert.Equal(2, rs.RowCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetCommand(bool includeIdentity)
        {
            // Setup:
            // ... Create a row create with cell updates
            const long rowId = 100;
            var columns = Common.GetColumns(includeIdentity);
            var rs = await Common.GetResultSet(columns, includeIdentity);
            var etm = Common.GetStandardMetadata(columns);
            RowCreate rc = new RowCreate(rowId, rs, etm);
            Common.AddCells(rc, includeIdentity);

            // ... Mock db connection for building the command
            var mockConn = new TestSqlConnection(null);

            // If: I attempt to get a command for the edit
            DbCommand cmd = rc.GetCommand(mockConn);

            // Then:
            // ... The command should not be null
            Assert.NotNull(cmd);

            // ... There should be parameters in it
            Assert.Equal(3, cmd.Parameters.Count);

            // ... It should be formatted into an insert script with output
            Regex r = new Regex(@"INSERT INTO (.+)\((.+)\) OUTPUT (.+) VALUES \((.+)\)");
            var m = r.Match(cmd.CommandText);
            Assert.True(m.Success);

            // ... There should be a table
            string tbl = m.Groups[1].Value;
            Assert.Equal(etm.EscapedMultipartName, tbl);

            // ... There should be 3 columns for input
            string inCols = m.Groups[2].Value;
            Assert.Equal(3, inCols.Split(',').Length);

            // ... There should be 3 OR 4 columns for output that are inserted.
            string[] outCols = m.Groups[3].Value.Split(',');
            Assert.Equal(includeIdentity ? 4 : 3, outCols.Length);
            Assert.All(outCols, s => Assert.StartsWith("inserted.", s.Trim()));

            // ... There should be 3 parameters
            string[] param = m.Groups[4].Value.Split(',');
            Assert.Equal(3, param.Length);
            Assert.All(param, s => Assert.StartsWith("@Value", s.Trim()));
        }

        [Fact]
        public async Task GetCommandNullConnection()
        {
            // Setup: Create a row create
            RowCreate rc = await GetStandardRowCreate();


            // If: I attempt to create a command with a null connection
            // Then: It should throw an exception
            Assert.Throws<ArgumentNullException>(() => rc.GetCommand(null));
        }

        [Fact]
        public async Task GetCommandMissingCell()
        {
            // Setup: Generate the parameters for the row create
            RowCreate rc = await GetStandardRowCreate();
            var mockConn = new TestSqlConnection(null);

            // If: I ask for a script to be generated without setting any values
            // Then: An exception should be thrown for missing cells
            Assert.Throws<InvalidOperationException>(() => rc.GetCommand(mockConn));
        }

        [Fact]
        public async Task GetEditRowNoAdditions()
        {
            // Setup: Generate a standard row create
            RowCreate rc = await GetStandardRowCreate();

            // If: I request an edit row from the row create
            EditRow er = rc.GetEditRow(null);

            // Then:
            // ... The row should not be null
            Assert.NotNull(er);

            // ... The row should not be clean
            Assert.True(er.IsDirty);
            Assert.Equal(EditRow.EditRowState.DirtyInsert, er.State);

            // ... The row should have a bunch of empty cells (equal to number of columns)
            Assert.Equal(rc.newCells.Length, er.Cells.Length);
            Assert.All(er.Cells, dbc =>
            {
                Assert.Equal(string.Empty, dbc.DisplayValue);
                Assert.False(dbc.IsNull);
            });
        }

        [Fact]
        public async Task GetEditRowWithAdditions()
        {
            // Setp: Generate a row create with a cell added to it
            RowCreate rc = await GetStandardRowCreate();
            rc.SetCell(0, "foo");

            // If: I request an edit row from the row create
            EditRow er = rc.GetEditRow(null);

            // Then:
            // ... The row should not be null and contain the same number of cells as columns
            Assert.NotNull(er);
            Assert.Equal(EditRow.EditRowState.DirtyInsert, er.State);

            // ... The row should not be clean
            Assert.True(er.IsDirty);
            Assert.Equal(EditRow.EditRowState.DirtyInsert, er.State);

            // ... The row should have a single non-empty cell at the beginning
            Assert.Equal("foo", er.Cells[0].DisplayValue);
            Assert.False(er.Cells[0].IsNull);

            // ... The rest of the cells should be blank
            for (int i = 1; i < er.Cells.Length; i++)
            {
                DbCellValue dbc = er.Cells[i];
                Assert.Equal(string.Empty, dbc.DisplayValue);
                Assert.False(dbc.IsNull);
            }
        }

        [Theory]
        [InlineData(-1)]        // Negative
        [InlineData(3)]         // At edge of acceptable values
        [InlineData(100)]       // Way too large value
        public async Task SetCellOutOfRange(int columnId)
        {
            // Setup: Generate a row create
            RowCreate rc = await GetStandardRowCreate();

            // If: I attempt to set a cell on a column that is out of range, I should get an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => rc.SetCell(columnId, string.Empty));
        }

        [Fact]
        public async Task SetCellNoChange()
        {
            // Setup: Generate a row create
            RowCreate rc = await GetStandardRowCreate();

            // If: I set a cell in the newly created row to something that doesn't need changing
            EditUpdateCellResult eucr = rc.SetCell(0, "1");

            // Then:
            // ... The returned value should be equal to what we provided
            Assert.NotNull(eucr);
            Assert.NotNull(eucr.UpdatedCell);
            Assert.Equal("1", eucr.UpdatedCell.DisplayValue);
            Assert.False(eucr.UpdatedCell.IsNull);

            // ... The returned value should be dirty
            Assert.NotNull(eucr.UpdatedCell.IsDirty);

            // ... The row should still be dirty
            Assert.True(eucr.IsRowDirty);

            // ... There should be a cell update in the cell list
            Assert.NotNull(rc.newCells[0]);
        }

        [Fact]
        public async Task SetCellHasCorrections()
        {
            // Setup: 
            // ... Generate a result set with a single binary column
            DbColumn[] cols = {new TestDbColumn
            {
                DataType = typeof(byte[]),
                DataTypeName = "binary"
            }};
            object[][] rows = {};
            var testResultSet = new TestResultSet(cols, rows);
            var testReader = new TestDbDataReader(new[] {testResultSet});
            var rs = new ResultSet(0, 0, MemoryFileSystem.GetFileStreamFactory());
            await rs.ReadResultToEnd(testReader, CancellationToken.None);

            // ... Generate the metadata
            var etm = Common.GetStandardMetadata(cols);

            // ... Create the row create
            RowCreate rc = new RowCreate(100, rs, etm);

            // If: I set a cell in the newly created row to something that will be corrected
            EditUpdateCellResult eucr = rc.SetCell(0, "1000");

            // Then:
            // ... The returned value should be equal to what we provided
            Assert.NotNull(eucr);
            Assert.NotNull(eucr.UpdatedCell);
            Assert.NotEqual("1000", eucr.UpdatedCell.DisplayValue);
            Assert.False(eucr.UpdatedCell.IsNull);

            // ... The returned value should be dirty
            Assert.NotNull(eucr.UpdatedCell.IsDirty);

            // ... The row should still be dirty
            Assert.True(eucr.IsRowDirty);

            // ... There should be a cell update in the cell list
            Assert.NotNull(rc.newCells[0]);
        }

        [Fact]
        public async Task SetCellNull()
        {
            // Setup: Generate a row create
            RowCreate rc = await GetStandardRowCreate();

            // If: I set a cell in the newly created row to null
            EditUpdateCellResult eucr = rc.SetCell(0, "NULL");

            // Then:
            // ... The returned value should be equal to what we provided
            Assert.NotNull(eucr);
            Assert.NotNull(eucr.UpdatedCell);
            Assert.NotEmpty(eucr.UpdatedCell.DisplayValue);
            Assert.True(eucr.UpdatedCell.IsNull);

            // ... The returned value should be dirty
            Assert.NotNull(eucr.UpdatedCell.IsDirty);

            // ... The row should still be dirty
            Assert.True(eucr.IsRowDirty);

            // ... There should be a cell update in the cell list
            Assert.NotNull(rc.newCells[0]);
        }

        [Theory]
        [InlineData(-1)]        // Negative
        [InlineData(3)]         // At edge of acceptable values
        [InlineData(100)]       // Way too large value
        public async Task RevertCellOutOfRange(int columnId)
        {
            // Setup: Generate the row create
            RowCreate rc = await GetStandardRowCreate();

            // If: I attempt to revert a cell that is out of range
            // Then: I should get an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => rc.RevertCell(columnId));
        }

        [Fact]
        public async Task RevertCellNotSet()
        {
            // Setup: Generate the row create
            RowCreate rc = await GetStandardRowCreate();

            // If: I attempt to revert a cell that has not been set
            EditRevertCellResult result = rc.RevertCell(0);

            // Then:
            // ... We should get a result back
            Assert.NotNull(result);

            // ... We should get a null cell back
            // @TODO: Check for a default value when we support it
            Assert.Null(result.RevertedCell);

            // ... The row should be dirty
            Assert.True(result.IsRowDirty);

            // ... The cell should no longer be set
            Assert.Null(rc.newCells[0]);
        }

        [Fact]
        public async Task RevertCellThatWasSet()
        {
            // Setup: Generate the row create
            RowCreate rc = await GetStandardRowCreate();
            rc.SetCell(0, "1");

            // If: I attempt to revert a cell that was set
            EditRevertCellResult result = rc.RevertCell(0);

            // Then:
            // ... We should get a result back
            Assert.NotNull(result);

            // ... We should get a null cell back
            // @TODO: Check for a default value when we support it
            Assert.Null(result.RevertedCell);

            // ... The row should be dirty
            Assert.True(result.IsRowDirty);

            // ... The cell should no longer be set
            Assert.Null(rc.newCells[0]);
        }

        private static async Task<RowCreate> GetStandardRowCreate()
        {
            var cols = Common.GetColumns(false);
            var rs = await Common.GetResultSet(cols, false);
            var etm = Common.GetStandardMetadata(cols);
            return new RowCreate(100, rs, etm);
        }
    }
}
