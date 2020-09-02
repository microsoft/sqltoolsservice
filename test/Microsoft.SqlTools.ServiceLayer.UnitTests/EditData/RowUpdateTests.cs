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
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    public class RowUpdateTests
    {
        [Test]
        public async Task RowUpdateConstruction()
        {
            // Setup: Create the values to store
            const long rowId = 0;
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, false);

            // If: I create a RowUpdate instance
            RowUpdate rc = new RowUpdate(rowId, rs, data.TableMetadata);

            // Then: The values I provided should be available
            Assert.AreEqual(rowId, rc.RowId);
            Assert.AreEqual(rs, rc.AssociatedResultSet);
            Assert.AreEqual(data.TableMetadata, rc.AssociatedObjectMetadata);
        }

        #region SetCell Tests
        
        [Test]
        public async Task SetCellOutOfRange([Values(-1,3,100)]int columnId)
        {
            // Setup: Generate a row create
            RowUpdate ru = await GetStandardRowUpdate();

            // If: I attempt to set a cell on a column that is out of range, I should get an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => ru.SetCell(columnId, string.Empty));
        }
        
        [Test]
        public async Task SetCellImplicitRevertTest()
        {
            // Setup: Create a fake table to update
            var data = new Common.TestDbColumnsWithTableMetadata(false, true, 0, 0);
            ResultSet rs = await Common.GetResultSet(data.DbColumns, true);

            // If: 
            // ... I add updates to all the cells in the row
            RowUpdate ru = new RowUpdate(0, rs, data.TableMetadata);
            Common.AddCells(ru, 1);

            // ... Then I update a cell back to it's old value
            var eucr = ru.SetCell(1, (string) rs.GetRow(0)[1].RawObject);

            // Then:
            // ... A edit cell was returned
            Assert.NotNull(eucr);
            Assert.NotNull(eucr.Cell);

            // ... The new value we provided should be returned
            Assert.AreEqual(rs.GetRow(0)[1].DisplayValue, eucr.Cell.DisplayValue);
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
            Assert.AreEqual(2, updateSplit.Length);
            Assert.That(updateSplit.Select(s => s.Split('=').Length), Has.All.EqualTo(2));
        }

        [Test]
        public async Task SetCellImplicitRowRevertTests()
        {
            // Setup: Create a fake column to update
            var data = new Common.TestDbColumnsWithTableMetadata(false, true, 0, 0);
            ResultSet rs = await Common.GetResultSet(data.DbColumns, true);

            // If:
            // ... I add updates to one cell in the row
            RowUpdate ru = new RowUpdate(0, rs, data.TableMetadata);
            ru.SetCell(1, "qqq");

            // ... Then I update the cell to its original value
            var eucr = ru.SetCell(1, (string) rs.GetRow(0)[1].RawObject);

            // Then:
            // ... An edit cell should have been returned
            Assert.NotNull(eucr);
            Assert.NotNull(eucr.Cell);

            // ... The old value should be returned
            Assert.AreEqual(rs.GetRow(0)[1].DisplayValue, eucr.Cell.DisplayValue);
            Assert.False(eucr.Cell.IsNull);

            // ... The cell should be clean
            Assert.False(eucr.Cell.IsDirty);

            // ... The row should be clean
            Assert.False(eucr.IsRowDirty);

            // TODO: Make sure that the script and command things will return null
        }
        
        [Test]
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
            var etm = Common.GetCustomEditTableMetadata(cols);

            // ... Create the row update
            RowUpdate ru = new RowUpdate(0, rs, etm);

            // If: I set a cell in the newly created row to something that will be corrected
            EditUpdateCellResult eucr = ru.SetCell(0, "1000");

            // Then:
            // ... A edit cell was returned
            Assert.NotNull(eucr);
            Assert.NotNull(eucr.Cell);

            // ... The value we used won't be returned
            Assert.That(eucr.Cell.DisplayValue, Is.Not.Empty);
            Assert.That(eucr.Cell.DisplayValue, Is.Not.EqualTo("1000"));
            Assert.False(eucr.Cell.IsNull);

            // ... The cell should be dirty
            Assert.True(eucr.Cell.IsDirty);

            // ... The row is still dirty
            Assert.True(eucr.IsRowDirty);

            // ... There should be a cell update in the cell list
            Assert.That(ru.cellUpdates.Keys, Has.Member(0));
            Assert.NotNull(ru.cellUpdates[0]);
        }
        
        [Test]
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
            Assert.AreEqual("col1", eucr.Cell.DisplayValue);
            Assert.False(eucr.Cell.IsNull);

            // ... The row is still dirty
            Assert.True(eucr.IsRowDirty);

            // ... The cell should be dirty
            Assert.True(eucr.Cell.IsDirty);

            // ... There should be a cell update in the cell list
            Assert.That(ru.cellUpdates.Keys, Has.Member(0));
            Assert.NotNull(ru.cellUpdates[0]);
        }
        
        #endregion

        [Test]
        public async Task GetScriptTest([Values]bool isMemoryOptimized)
        {
            // Setup: Create a fake table to update
            var data = new Common.TestDbColumnsWithTableMetadata(isMemoryOptimized, true, 0, 0);
            ResultSet rs = await Common.GetResultSet(data.DbColumns, true);

            // If: I ask for a script to be generated for update
            RowUpdate ru = new RowUpdate(0, rs, data.TableMetadata);
            Common.AddCells(ru, 1);
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
            Assert.AreEqual(data.TableMetadata.EscapedMultipartName, tbl);
            Assert.AreEqual(3, updateSplit.Length);
            Assert.That(updateSplit.Select(s => s.Split('=').Length), Has.All.EqualTo(2));
        }
        
        #region GetCommand Tests

        [Test]
        public async Task GetCommand([Values] bool includeIdentity, [Values] bool isMemoryOptimized)
        {
            // Setup: 
            // ... Create a row update with cell updates
            var data = new Common.TestDbColumnsWithTableMetadata(isMemoryOptimized, includeIdentity, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, includeIdentity);
            RowUpdate ru = new RowUpdate(0, rs, data.TableMetadata);
            Common.AddCells(ru, includeIdentity ? 1 : 0);

            // ... Mock db connection for building the command
            var mockConn = new TestSqlConnection(null);

            // If: I ask for a command to be generated for update
            DbCommand cmd = ru.GetCommand(mockConn);

            // Then:
            // ... The command should not be null
            Assert.NotNull(cmd);

            // ... Validate the command's makeup
            // Break the query into parts
            string[] splitSql = cmd.CommandText.Split(Environment.NewLine);
            Assert.True(splitSql.Length >= 3);
            
            // Check the declare statement first
            Regex declareRegex = new Regex(@"^DECLARE @(.+) TABLE \((.+)\)$");
            Match declareMatch = declareRegex.Match(splitSql[0]);
            Assert.True(declareMatch.Success);
            
            // Declared table name matches
            Assert.True(declareMatch.Groups[1].Value.StartsWith("Update"));
            Assert.True(declareMatch.Groups[1].Value.EndsWith("Output"));
            
            // Correct number of columns in declared table
            string[] declareCols = declareMatch.Groups[2].Value.Split(", ");
            Assert.AreEqual(rs.Columns.Length, declareCols.Length);
            
            // Check the update statement in the middle
            string regex = isMemoryOptimized
                ? @"^UPDATE (.+) WITH \(SNAPSHOT\) SET (.+) OUTPUT (.+) INTO @(.+) WHERE .+$"
                : @"^UPDATE (.+) SET (.+) OUTPUT (.+) INTO @(.+) WHERE .+$";
            Regex updateRegex = new Regex(regex);
            Match updateMatch = updateRegex.Match(splitSql[10]);
            Assert.True(updateMatch.Success);
            
            // Table name matches
            Assert.AreEqual(Common.TableName, updateMatch.Groups[1].Value);
            
            // Output columns match
            string[] outCols = updateMatch.Groups[3].Value.Split(", ");
            Assert.AreEqual(rs.Columns.Length, outCols.Length);
            Assert.That(outCols, Has.All.StartsWith("inserted."));
            
            string[] setCols = updateMatch.Groups[2].Value.Split(", ");
            Assert.AreEqual(3, setCols.Length);
            Assert.That(setCols, Has.All.Match(@".+ = @Value\d+_\d+"), "Set columns match");

            // Output table name matches
            Assert.That(updateMatch.Groups[4].Value, Does.StartWith("Update"));
            Assert.That(updateMatch.Groups[4].Value, Does.EndWith("Output"));
            
            // Check the select statement last
            Regex selectRegex = new Regex(@"^SELECT (.+) FROM @(.+)$");
            Match selectMatch = selectRegex.Match(splitSql[11]);
            Assert.True(selectMatch.Success);
            
            // Correct number of columns in select statement
            string[] selectCols = selectMatch.Groups[1].Value.Split(", ");
            Assert.AreEqual(rs.Columns.Length, selectCols.Length);
            
            // Select table name matches
            Assert.That(selectMatch.Groups[2].Value, Does.StartWith("Update"));
            Assert.That(selectMatch.Groups[2].Value, Does.EndWith("Output"));
            
            // ... There should be an appropriate number of parameters in it
            //     (1 or 3 keys, 3 value parameters)
            int expectedKeys = includeIdentity ? 1 : 3;
            Assert.AreEqual(expectedKeys + 3, cmd.Parameters.Count);
        }

        [Test]
        public async Task GetCommandNullConnection()
        {
            // Setup: Create a row update
            RowUpdate ru = await GetStandardRowUpdate();

            // If: I attempt to create a command with a null connection
            // Then: It should throw an exception
            Assert.Throws<ArgumentNullException>(() => ru.GetCommand(null));
        }
        
        #endregion

        #region GetEditRow Tests
        
        [Test]
        public async Task GetEditRow()
        {
            // Setup: Create a row update with a cell set
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, false);
            RowUpdate ru = new RowUpdate(0, rs, data.TableMetadata);
            ru.SetCell(0, "foo");

            // If: I attempt to get an edit row
            DbCellValue[] cells = rs.GetRow(0).ToArray();
            EditRow er = ru.GetEditRow(cells);

            // Then:
            // ... The state should be dirty
            Assert.True(er.IsDirty);
            Assert.AreEqual(EditRow.EditRowState.DirtyUpdate, er.State);

            // ... The ID should be the same as the one provided
            Assert.AreEqual(0, er.Id);

            // ... The row should match the cells that were given, except for the updated cell
            Assert.AreEqual(cells.Length, er.Cells.Length);
            for (int i = 1; i < cells.Length; i++)
            {
                DbCellValue originalCell = cells[i];
                DbCellValue outputCell = er.Cells[i];

                Assert.AreEqual(originalCell.DisplayValue, outputCell.DisplayValue);
                Assert.AreEqual(originalCell.IsNull, outputCell.IsNull);
                // Note: No real need to check the RawObject property
            }

            // ... The updated cell should match what it was set to and be dirty
            EditCell newCell = er.Cells[0];
            Assert.AreEqual("foo", newCell.DisplayValue);
            Assert.False(newCell.IsNull);
            Assert.True(newCell.IsDirty);
        }

        [Test]
        public async Task GetEditNullRow()
        {
            // Setup: Create a row update
            RowUpdate ru = await GetStandardRowUpdate();

            // If: I attempt to get an edit row with a null cached row
            // Then: I should get an exception
            Assert.Throws<ArgumentNullException>(() => ru.GetEditRow(null));
        }

        #endregion
        
        #region ApplyChanges Tests
        
        [Test]
        public async Task ApplyChanges([Values] bool includeIdentity)
        {
            // Setup: 
            // ... Create a row update (no cell updates needed)
            var data = new Common.TestDbColumnsWithTableMetadata(false, includeIdentity, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, includeIdentity);
            RowUpdate ru = new RowUpdate(0, rs, data.TableMetadata);
            long oldBytesWritten = rs.totalBytesWritten;

            // ... Setup a db reader for the result of an update
            var newRowReader = Common.GetNewRowDataReader(data.DbColumns, includeIdentity);

            // If: I ask for the change to be applied
            await ru.ApplyChanges(newRowReader);

            // Then: 
            // ... The result set should have the same number of rows as before
            Assert.AreEqual(1, rs.RowCount);
            Assert.True(oldBytesWritten < rs.totalBytesWritten);
        }

        [Test]
        public async Task ApplyChangesNullReader()
        {
            // Setup: 
            // ... Create a row update (no cell updates needed)
            var data = new Common.TestDbColumnsWithTableMetadata(false, true, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, true);
            RowUpdate ru = new RowUpdate(0, rs, data.TableMetadata);

            // If: I  ask for the changes to be applied with a null db reader
            // Then: I should get an exception
            Assert.ThrowsAsync<ArgumentNullException>(() => ru.ApplyChanges(null));
        }

        #endregion
        
        #region RevertCell Tests
        
        [Test]
        public async Task RevertCellOutOfRange([Values(-1, 3, 100)]  int columnId)
        {
            // Setup: 
            // ... Create a row update (no cell updates needed)
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, false);
            RowUpdate ru = new RowUpdate(0, rs, data.TableMetadata);

            // If: I attempt to revert a cell that is out of range
            // Then: I should get an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => ru.RevertCell(columnId));
        }

        [Test]
        public async Task RevertCellNotSet()
        {
            // Setup: 
            // ... Create a row update (no cell updates needed)
            var data = new Common.TestDbColumnsWithTableMetadata(false, true, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, true);
            RowUpdate ru = new RowUpdate(0, rs, data.TableMetadata);

            // If: I attempt to revert a cell that has not been set
            EditRevertCellResult result = ru.RevertCell(0);

            // Then:
            // ... We should get a result back
            Assert.NotNull(result);

            // ... We should get the original value back
            // @TODO: Check for a default value when we support it
            Assert.NotNull(result.Cell);
            Assert.AreEqual(rs.GetRow(0)[0].DisplayValue, result.Cell.DisplayValue);

            // ... The row should be clean
            Assert.False(result.IsRowDirty);

            Assert.That(ru.cellUpdates.Keys, Has.None.Zero, "The cell should no longer be set");
        }

        [Test]
        public async Task RevertCellThatWasSet()
        {
            // Setup: 
            // ... Create a row update
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, false);
            RowUpdate ru = new RowUpdate(0, rs, data.TableMetadata);
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
            Assert.AreEqual(rs.GetRow(0)[0].DisplayValue, result.Cell.DisplayValue);

            // ... The row should be dirty still
            Assert.True(result.IsRowDirty);

            Assert.That(ru.cellUpdates.Keys, Has.None.Zero, "The cell should no longer be set");
        }

        [Test]
        public async Task RevertCellRevertsRow()
        {
            // Setup:
            // ... Create a row update
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, false);
            RowUpdate ru = new RowUpdate(0, rs, data.TableMetadata);
            ru.SetCell(0, "qqq");

            // If: I attempt to revert a cell that was set
            EditRevertCellResult result = ru.RevertCell(0);

            // Then:
            // ... We should get a result back
            Assert.NotNull(result);

            // ... We should get the original value back
            // @TODO: Check for a default value when we support it
            Assert.NotNull(result.Cell);
            Assert.AreEqual(rs.GetRow(0)[0].DisplayValue, result.Cell.DisplayValue);

            // ... The row should now be reverted
            Assert.False(result.IsRowDirty);

            Assert.That(ru.cellUpdates.Keys, Has.None.Zero, "The cell should no longer be set");
        }
        
        #endregion

        private async Task<RowUpdate> GetStandardRowUpdate()
        {
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, false);
            return new RowUpdate(0, rs, data.TableMetadata);
        }
    }
}
