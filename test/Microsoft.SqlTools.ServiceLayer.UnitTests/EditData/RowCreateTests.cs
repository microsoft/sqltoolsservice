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

        #region GetScript Tests

        [Fact]
        public async Task GetScriptMissingCell()
        {
            // Setup: Generate the parameters for the row create
            RowCreate rc = await GetStandardRowCreate();

            // If: I ask for a script to be generated without setting any values
            // Then: An exception should be thrown for missing cells
            Assert.Throws<InvalidOperationException>(() => rc.GetScript());
        }

        public static IEnumerable<object[]> GetScriptData
        {
            get
            {
                yield return new object[] {true, 0, 1, new RegexExpectedOutput(3, 3, 0)};    // Has identity, no defaults, all values set
                yield return new object[] {true, 2, 1, new RegexExpectedOutput(3, 3, 0)};    // Has identity, some defaults, all values set
                yield return new object[] {true, 2, 2, new RegexExpectedOutput(2, 2, 0)};    // Has identity, some defaults, defaults not set
                yield return new object[] {true, 4, 1, new RegexExpectedOutput(3, 3, 0)};    // Has identity, all defaults, all values set
                yield return new object[] {true, 4, 4, null};                                // Has identity, all defaults, defaults not set
                yield return new object[] {false, 0, 0, new RegexExpectedOutput(3, 3, 0)};   // No identity, no defaults, all values set
                yield return new object[] {false, 1, 0, new RegexExpectedOutput(3, 3, 0)};   // No identity, some defaults, all values set
                yield return new object[] {false, 1, 1, new RegexExpectedOutput(2, 2, 0)};   // No identity, some defaults, defaults not set
                yield return new object[] {false, 3, 0, new RegexExpectedOutput(3, 3, 0)};   // No identity, all defaults, all values set
                yield return new object[] {false, 3, 3, null};                               // No identity, all defaults, defaults not set
            }
        }
        
        [Theory]
        [MemberData(nameof(GetScriptData))]
        public async Task GetScript(bool includeIdentity, int defaultCols, int valuesToSkipSetting, RegexExpectedOutput expectedOutput)
        {
            // Setup: 
            // ... Generate the parameters for the row create
            DbColumn[] columns = Common.GetColumns(includeIdentity);
            ResultSet rs = await Common.GetResultSet(columns, includeIdentity);
            EditTableMetadata etm = Common.GetStandardMetadata(columns, includeIdentity, defaultCols);
            
            // ... Create a row create and set the appropriate number of cells 
            RowCreate rc = new RowCreate(100, rs, etm);
            Common.AddCells(rc, valuesToSkipSetting);
            
            // If: I ask for the script for the row insert
            string script = rc.GetScript();
            
            // Then:
            // ... The script should not be null
            Assert.NotNull(script);
            
            // ... The script should match the expected regex output
            ValidateScriptAgainstRegex(script, expectedOutput);
        }

        private static void ValidateScriptAgainstRegex(string sql, RegexExpectedOutput expectedOutput)
        {
            if (expectedOutput == null)
            {
                // If expected output was null make sure we match the default values reges
                Regex r = new Regex(@"INSERT INTO (.+) DEFAULT VALUES");
                Match m = r.Match(sql);
                Assert.True(m.Success);
                
                // Table name matches
                Assert.Equal(Common.TableName, m.Groups[1].Value);
            }
            else
            {
                // Do the whole validation
                Regex r = new Regex(@"INSERT INTO (.+)\((.+)\) VALUES \((.+)\)");
                Match m = r.Match(sql);
                Assert.True(m.Success);
                
                // Table name matches
                Assert.Equal(Common.TableName, m.Groups[1].Value);
                
                // In columns match
                string cols = m.Groups[2].Value;
                Assert.Equal(expectedOutput.ExpectedInColumns, cols.Split(',').Length);
                
                // In values match
                string vals = m.Groups[3].Value;               
                Assert.Equal(expectedOutput.ExpectedInValues, vals.Split(',').Length);
            }
        }
        
        #endregion

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
            EditTableMetadata etm = Common.GetStandardMetadata(columns, includeIdentity);

            // ... Setup a db reader for the result of an insert
            var newRowReader = Common.GetNewRowDataReader(columns, includeIdentity);

            // If: I ask for the change to be applied
            RowCreate rc = new RowCreate(rowId, rs, etm);
            await rc.ApplyChanges(newRowReader);

            // Then: The result set should have an additional row in it
            Assert.Equal(2, rs.RowCount);
        }

        #region GetCommand Tests

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
        public async Task GetCommandMissingCellNoDefault()
        {
            // Setup: Generate the parameters for the row create
            RowCreate rc = await GetStandardRowCreate();
            var mockConn = new TestSqlConnection(null);

            // If: I ask for a script to be generated without setting any values
            // Then: An exception should be thrown for missing cells
            Assert.Throws<InvalidOperationException>(() => rc.GetCommand(mockConn));
        }

        public static IEnumerable<object[]> GetCommandData
        {
            get
            {
                yield return new object[] {true, 0, 1, new RegexExpectedOutput(3, 3, 4)};    // Has identity, no defaults, all values set
                yield return new object[] {true, 2, 1, new RegexExpectedOutput(3, 3, 4)};    // Has identity, some defaults, all values set
                yield return new object[] {true, 2, 2, new RegexExpectedOutput(2, 2, 4)};    // Has identity, some defaults, defaults not set
                yield return new object[] {true, 4, 1, new RegexExpectedOutput(3, 3, 4)};    // Has identity, all defaults, all values set
                yield return new object[] {true, 4, 4, new RegexExpectedOutput(0, 0, 4)};    // Has identity, all defaults, defaults not set
                yield return new object[] {false, 0, 0, new RegexExpectedOutput(3, 3, 3)};   // No identity, no defaults, all values set
                yield return new object[] {false, 1, 0, new RegexExpectedOutput(3, 3, 3)};   // No identity, some defaults, all values set
                yield return new object[] {false, 1, 1, new RegexExpectedOutput(2, 2, 3)};   // No identity, some defaults, defaults not set
                yield return new object[] {false, 3, 0, new RegexExpectedOutput(3, 3, 3)};   // No identity, all defaults, all values set
                yield return new object[] {false, 3, 3, new RegexExpectedOutput(0, 0, 3)};   // No identity, all defaults, defaults not set
            }
        }
        
        [Theory]
        [MemberData(nameof(GetCommandData))]
        public async Task GetCommand(bool includeIdentity, int defaultCols, int valuesToSkipSetting, RegexExpectedOutput expectedOutput)
        {
            // Setup: 
            // ... Generate the parameters for the row create
            DbColumn[] columns = Common.GetColumns(includeIdentity);
            ResultSet rs = await Common.GetResultSet(columns, includeIdentity);
            EditTableMetadata etm = Common.GetStandardMetadata(columns, includeIdentity, defaultCols);
            
            // ... Mock db connection for building the command
            var mockConn = new TestSqlConnection(null);
            
            // ... Create a row create and set the appropriate number of cells 
            RowCreate rc = new RowCreate(100, rs, etm);
            Common.AddCells(rc, valuesToSkipSetting);
            
            // If: I ask for the command for the row insert
            DbCommand cmd = rc.GetCommand(mockConn);
            
            // Then:
            // ... The command should not be null
            Assert.NotNull(cmd);
            
            // ... There should be parameters in it
            Assert.Equal(expectedOutput.ExpectedInValues, cmd.Parameters.Count);
            
            // ... The script should match the expected regex output
            ValidateCommandAgainstRegex(cmd.CommandText, expectedOutput);
        }

        private static void ValidateCommandAgainstRegex(string sql, RegexExpectedOutput expectedOutput)
        {
            if (expectedOutput.ExpectedInColumns == 0 || expectedOutput.ExpectedInValues == 0)
            {
                // If expected output was null make sure we match the default values reges
                Regex r = new Regex(@"INSERT INTO (.+) OUTPUT (.+) DEFAULT VALUES");
                Match m = r.Match(sql);
                Assert.True(m.Success);
                
                // Table name matches
                Assert.Equal(Common.TableName, m.Groups[1].Value);
                
                // Output columns match
                string[] outCols = m.Groups[2].Value.Split(", ");
                Assert.Equal(expectedOutput.ExpectedOutColumns, outCols.Length);
                Assert.All(outCols, col => Assert.StartsWith("inserted.", col));
            }
            else
            {
                // Do the whole validation
                Regex r = new Regex(@"INSERT INTO (.+)\((.+)\) OUTPUT (.+) VALUES \((.+)\)");
                Match m = r.Match(sql);
                Assert.True(m.Success);
                
                // Table name matches
                Assert.Equal(Common.TableName, m.Groups[1].Value);
                
                // Output columns match
                string[] outCols = m.Groups[3].Value.Split(", ");
                Assert.Equal(expectedOutput.ExpectedOutColumns, outCols.Length);
                Assert.All(outCols, col => Assert.StartsWith("inserted.", col));
                
                // In columns match
                string[] inCols = m.Groups[2].Value.Split(", ");
                Assert.Equal(expectedOutput.ExpectedInColumns, inCols.Length);
                
                // In values match
                string[] inVals = m.Groups[4].Value.Split(", ");
                Assert.Equal(expectedOutput.ExpectedInValues, inVals.Length);
                Assert.All(inVals, val => Assert.Matches(@"@.+\d+", val));
            }
        }
        
        #endregion
        
        #region GetEditRow Tests

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

            // ... The row should have a bunch of empty cells (equal to number of columns) and all are dirty
            Assert.Equal(rc.newCells.Length, er.Cells.Length);
            Assert.All(er.Cells, ec =>
            {
                Assert.Equal(string.Empty, ec.DisplayValue);
                Assert.False(ec.IsNull);
                Assert.True(ec.IsDirty);
            });
        }

        [Fact]
        public async Task GetEditRowWithDefaultValue()
        {
            // Setup: Generate a row create with default values
            const long rowId = 100;
            DbColumn[] columns = Common.GetColumns(false);
            ResultSet rs = await Common.GetResultSet(columns, false);
            EditTableMetadata etm = Common.GetStandardMetadata(columns, false, columns.Length);
            RowCreate rc = new RowCreate(rowId, rs, etm);
            
            // If: I request an edit row from the row create
            EditRow er = rc.GetEditRow(null);
            
            // Then:
            // ... The row should not be null
            Assert.NotNull(er);
            
            // ... The row should not be clean
            Assert.True(er.IsDirty);
            Assert.Equal(EditRow.EditRowState.DirtyInsert, er.State);
            
            // ... The row sould have a bunch of default values (equal to number of columns) and all are dirty
            Assert.Equal(rc.newCells.Length, er.Cells.Length);
            Assert.All(er.Cells, ec =>
            {
                Assert.Equal(Common.DefaultValue, ec.DisplayValue);
                Assert.False(ec.IsNull);    // TODO: Update when we support null default values better
                Assert.True(ec.IsDirty);
            });
        }

        [Fact]
        public async Task GetEditRowWithCalculatedValue()
        {
            // Setup: Generate a row create with an identity column
            const long rowId = 100;
            DbColumn[] columns = Common.GetColumns(true);
            ResultSet rs = await Common.GetResultSet(columns, true);
            EditTableMetadata etm = Common.GetStandardMetadata(columns, true);
            RowCreate rc = new RowCreate(rowId, rs, etm);
            
            // If: I request an edit row from the row created
            EditRow er = rc.GetEditRow(null);
            
            // Then:
            // ... The row should not be null
            Assert.NotNull(er);
            Assert.Equal(er.Id, rowId);
            
            // ... The row should not be clean
            Assert.True(er.IsDirty);
            Assert.Equal(EditRow.EditRowState.DirtyInsert, er.State);
            
            // ... The row should have a TBD for the identity column
            Assert.Equal(rc.newCells.Length, er.Cells.Length);
            Assert.Equal(SR.EditDataComputedColumnPlaceholder, er.Cells[0].DisplayValue);
            Assert.False(er.Cells[0].IsNull);
            Assert.True(er.Cells[0].IsDirty);
                
            // ... The rest of the cells should have empty display values
            Assert.All(er.Cells.Skip(1), ec =>
            {
                Assert.Equal(string.Empty, ec.DisplayValue);
                Assert.False(ec.IsNull);
                Assert.True(ec.IsDirty);
            });
        }

        [Fact]
        public async Task GetEditRowWithAdditions()
        {
            // Setp: Generate a row create with a cell added to it
            RowCreate rc = await GetStandardRowCreate();
            const string setValue = "foo";
            rc.SetCell(0, setValue);

            // If: I request an edit row from the row create
            EditRow er = rc.GetEditRow(null);

            // Then:
            // ... The row should not be null and contain the same number of cells as columns
            Assert.NotNull(er);
            Assert.Equal(EditRow.EditRowState.DirtyInsert, er.State);

            // ... The row should not be clean
            Assert.True(er.IsDirty);
            Assert.Equal(EditRow.EditRowState.DirtyInsert, er.State);

            // ... The row should have a single non-empty cell at the beginning that is dirty
            Assert.Equal(setValue, er.Cells[0].DisplayValue);
            Assert.False(er.Cells[0].IsNull);
            Assert.True(er.Cells[0].IsDirty);

            // ... The rest of the cells should be blank, but dirty
            for (int i = 1; i < er.Cells.Length; i++)
            {
                EditCell ec = er.Cells[i];
                Assert.Equal(string.Empty, ec.DisplayValue);
                Assert.False(ec.IsNull);
                Assert.True(ec.IsDirty);
            }
        }

        #endregion

        #region SetCell Tests
        
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
            const string updateValue = "1";
            EditUpdateCellResult eucr = rc.SetCell(0, updateValue);

            // Then:
            // ... The returned value should be equal to what we provided
            Assert.NotNull(eucr);
            Assert.NotNull(eucr.Cell);
            Assert.Equal(updateValue, eucr.Cell.DisplayValue);
            Assert.False(eucr.Cell.IsNull);

            // ... The returned value should be dirty
            Assert.NotNull(eucr.Cell.IsDirty);

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
            var testReader = new TestDbDataReader(new[] {testResultSet}, false);
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
            Assert.NotNull(eucr.Cell);
            Assert.NotEqual("1000", eucr.Cell.DisplayValue);
            Assert.False(eucr.Cell.IsNull);

            // ... The returned value should be dirty
            Assert.NotNull(eucr.Cell.IsDirty);

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
            const string nullValue = "NULL";
            EditUpdateCellResult eucr = rc.SetCell(0, nullValue);

            // Then:
            // ... The returned value should be equal to what we provided
            Assert.NotNull(eucr);
            Assert.NotNull(eucr.Cell);
            Assert.Equal(nullValue, eucr.Cell.DisplayValue);
            Assert.True(eucr.Cell.IsNull);

            // ... The returned value should be dirty
            Assert.NotNull(eucr.Cell.IsDirty);

            // ... The row should still be dirty
            Assert.True(eucr.IsRowDirty);

            // ... There should be a cell update in the cell list
            Assert.NotNull(rc.newCells[0]);
        }

        #endregion
        
        #region RevertCell Tests
        
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RevertCellNotSet(bool hasDefaultValues)
        {
            // Setup: 
            // ... Generate the parameters for the row create
            DbColumn[] columns = Common.GetColumns(false);
            ResultSet rs = await Common.GetResultSet(columns, false);
            EditTableMetadata etm = Common.GetStandardMetadata(columns, false, hasDefaultValues ? 1 : 0);
            RowCreate rc = new RowCreate(100, rs, etm);

            // If: I attempt to revert a cell that has not been set
            EditRevertCellResult result = rc.RevertCell(0);

            // Then:
            // ... We should get a result back
            Assert.NotNull(result);

            // ... We should get back an edit cell with a value based on the default value
            string expectedDisplayValue = hasDefaultValues ? Common.DefaultValue : string.Empty; 
            Assert.NotNull(result.Cell);
            Assert.Equal(expectedDisplayValue, result.Cell.DisplayValue);
            Assert.False(result.Cell.IsNull);    // TODO: Modify to support null defaults

            // ... The row should be dirty
            Assert.True(result.IsRowDirty);

            // ... The cell should no longer be set
            Assert.Null(rc.newCells[0]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RevertCellThatWasSet(bool hasDefaultValues)
        {
            // Setup: 
            // ... Generate the parameters for the row create
            DbColumn[] columns = Common.GetColumns(false);
            ResultSet rs = await Common.GetResultSet(columns, false);
            EditTableMetadata etm = Common.GetStandardMetadata(columns, false, hasDefaultValues ? 1 : 0);
            RowCreate rc = new RowCreate(100, rs, etm);
            rc.SetCell(0, "1");

            // If: I attempt to revert a cell that was set
            EditRevertCellResult result = rc.RevertCell(0);

            // Then:
            // ... We should get a result back
            Assert.NotNull(result);

            // ... We should get back an edit cell with a value based on the default value
            string expectedDisplayValue = hasDefaultValues ? Common.DefaultValue : string.Empty; 
            Assert.NotNull(result.Cell);
            Assert.Equal(expectedDisplayValue, result.Cell.DisplayValue);
            Assert.False(result.Cell.IsNull);    // TODO: Modify to support null defaults

            // ... The row should be dirty
            Assert.True(result.IsRowDirty);

            // ... The cell should no longer be set
            Assert.Null(rc.newCells[0]);
        }

        #endregion
        
        private static async Task<RowCreate> GetStandardRowCreate()
        {
            var cols = Common.GetColumns(false);
            var rs = await Common.GetResultSet(cols, false);
            var etm = Common.GetStandardMetadata(cols);
            return new RowCreate(100, rs, etm);
        }

        public class RegexExpectedOutput
        {
            public RegexExpectedOutput(int expectedInColumns, int expectedInValues, int expectedOutColumns)
            {
                ExpectedInColumns = expectedInColumns;
                ExpectedInValues = expectedInValues;
                ExpectedOutColumns = expectedOutColumns;
            }
            
            public int ExpectedInColumns { get; set; }
            public int ExpectedInValues { get; set; }
            public int ExpectedOutColumns { get; set; }
        }
    }
}
