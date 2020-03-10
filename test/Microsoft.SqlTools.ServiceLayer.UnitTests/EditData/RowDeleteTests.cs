//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    public class RowDeleteTests
    {
        [Fact]
        public async Task RowDeleteConstruction()
        {
            // Setup: Create the values to store
            Common.TestDbColumnsWithTableMetadata data = new Common.TestDbColumnsWithTableMetadata(false, true, 0, 0);
            ResultSet rs = await Common.GetResultSet(data.DbColumns, true);

            // If: I create a RowCreate instance
            RowDelete rc = new RowDelete(100, rs, data.TableMetadata);

            // Then: The values I provided should be available
            Assert.Equal(100, rc.RowId);
            Assert.Equal(rs, rc.AssociatedResultSet);
            Assert.Equal(data.TableMetadata, rc.AssociatedObjectMetadata);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetScriptTest(bool isMemoryOptimized)
        {
            Common.TestDbColumnsWithTableMetadata data = new Common.TestDbColumnsWithTableMetadata(isMemoryOptimized, true, 0, 0);
            ResultSet rs = await Common.GetResultSet(data.DbColumns, true);

            // If: I ask for a script to be generated for delete
            RowDelete rd = new RowDelete(0, rs, data.TableMetadata);
            string script = rd.GetScript();

            // Then:
            // ... The script should not be null
            Assert.NotNull(script);

            // ... It should be formatted as a delete script
            string scriptStart = $"DELETE FROM {data.TableMetadata.EscapedMultipartName}";
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
            Common.TestDbColumnsWithTableMetadata data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, false);

            // If: I ask for the change to be applied
            RowDelete rd = new RowDelete(0, rs, data.TableMetadata);
            await rd.ApplyChanges(null);      // Reader not used, can be null

            // Then : The result set should have one less row in it
            Assert.Equal(0, rs.RowCount);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public async Task GetCommand(bool includeIdentity, bool isMemoryOptimized)
        {
            // Setup:
            // ... Create a row delete
            Common.TestDbColumnsWithTableMetadata data = new Common.TestDbColumnsWithTableMetadata(isMemoryOptimized, includeIdentity, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, includeIdentity);
            RowDelete rd = new RowDelete(0, rs, data.TableMetadata);

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
            Assert.Equal(data.TableMetadata.EscapedMultipartName, tbl);

            // ... There should be as many where components as there are keys
            string[] whereComponents = m.Groups[2].Value.Split(new[] {"AND"}, StringSplitOptions.None);
            Assert.Equal(expectedKeys, whereComponents.Length);

            // ... Each component should have be equal to a parameter
            Assert.All(whereComponents, c => Assert.True(Regex.IsMatch(c.Trim(), @"\(.+ = @.+\)")));
        }

        [Fact]
        public async Task GetCommandNullConnection()
        {
            // Setup: Create a row delete
            RowDelete rd = await GetStandardRowDelete();

            // If: I attempt to create a command with a null connection
            // Then: It should throw an exception
            Assert.Throws<ArgumentNullException>(() => rd.GetCommand(null));
        }

        [Fact]
        public async Task GetEditRow()
        {
            // Setup: Create a row delete
            Common.TestDbColumnsWithTableMetadata data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, false);
            RowDelete rd = new RowDelete(0, rs, data.TableMetadata);

            // If: I attempt to get an edit row
            DbCellValue[] cells = rs.GetRow(0).ToArray();
            EditRow er = rd.GetEditRow(cells);

            // Then:
            // ... The state should be dirty
            Assert.True(er.IsDirty);
            Assert.Equal(EditRow.EditRowState.DirtyDelete, er.State);

            // ... The ID should be the same as the one provided
            Assert.Equal(0, er.Id);

            // ... The row should match the cells that were given and should be dirty
            Assert.Equal(cells.Length, er.Cells.Length);
            for (int i = 0; i < cells.Length; i++)
            {
                DbCellValue originalCell = cells[i];
                EditCell outputCell = er.Cells[i];

                Assert.Equal(originalCell.DisplayValue, outputCell.DisplayValue);
                Assert.Equal(originalCell.IsNull, outputCell.IsNull);
                Assert.True(outputCell.IsDirty);
                // Note: No real need to check the RawObject property
            }
        }

        [Fact]
        public async Task GetEditNullRow()
        {
            // Setup: Create a row delete
            RowDelete rd = await GetStandardRowDelete();

            // If: I attempt to get an edit row with a null cached row
            // Then: I should get an exception
            Assert.Throws<ArgumentNullException>(() => rd.GetEditRow(null));
        }

        [Fact]
        public async Task SetCell()
        {
            // Setup: Create a row delete
            RowDelete rd = await GetStandardRowDelete();

            // If: I set a cell on a delete row edit
            // Then: It should throw as invalid operation
            Assert.Throws<InvalidOperationException>(() => rd.SetCell(0, null));
        }

        [Fact]
        public async Task RevertCell()
        {
            // Setup: Create a row delete
            RowDelete rd = await GetStandardRowDelete();

            // If: I revert a cell on a delete row edit
            // Then: It should throw
            Assert.Throws<InvalidOperationException>(() => rd.RevertCell(0));
        }

        [Fact]
        public async Task GetVerifyQuery()
        {
            // Setup: Create a row update and set the first row cell to have values
            // ... other than "1" for testing purposes (simulated select query result).
            Common.TestDbColumnsWithTableMetadata data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, false);
            RowUpdate ru = new RowUpdate(0, rs, data.TableMetadata);
            object[][] rows =
            {
                new object[] {"2", "0", "0"},
            };
            var testResultSet = new TestResultSet(data.DbColumns, rows);
            var newRowReader = new TestDbDataReader(new[] { testResultSet }, false);
            await ru.ApplyChanges(newRowReader);

            // ... Create a row delete.
            RowDelete rd = new RowDelete(0, rs, data.TableMetadata);
            int expectedKeys = 3;

            // If: I generate a verify command
            String verifyCommand = rd.GetVerifyScript();

            // Then:
            // ... The command should not be null
            Assert.NotNull(verifyCommand);

            // ... It should be formatted into an where script
            string regexTest = @"SELECT COUNT \(\*\) FROM (.+) WHERE (.+)";
            Regex r = new Regex(regexTest);
            var m = r.Match(verifyCommand);
            Assert.True(m.Success);

            // ... There should be a table
            string tbl = m.Groups[1].Value;
            Assert.Equal(data.TableMetadata.EscapedMultipartName, tbl);

            // ... There should be as many where components as there are keys
            string[] whereComponents = m.Groups[2].Value.Split(new[] { "AND" }, StringSplitOptions.None);
            Assert.Equal(expectedKeys, whereComponents.Length);

            // ... Mock db connection for building the command
            var mockConn = new TestSqlConnection(new[] { testResultSet });

            // If: I attempt to get a command for a simulated delete of a row with duplicates.
            // Then: The Command will throw an exception as it detects there are
            // ... 2 or more rows with the same value in the simulated query results data.
            Assert.Throws<EditDataDeleteException>(() => rd.GetCommand(mockConn));
        }

        private async Task<RowDelete> GetStandardRowDelete()
        {
            Common.TestDbColumnsWithTableMetadata data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, false);
            return new RowDelete(0, rs, data.TableMetadata);
        }
    }
}
