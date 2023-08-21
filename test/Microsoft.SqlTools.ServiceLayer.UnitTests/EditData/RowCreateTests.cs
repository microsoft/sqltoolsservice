﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    public partial class RowCreateTests
    {
        [Test]
        public async Task RowCreateConstruction()
        {
            // Setup: Create the values to store
            const long rowId = 100;
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);
            ResultSet rs = await Common.GetResultSet(data.DbColumns, false);

            // If: I create a RowCreate instance
            var rc = new RowCreate(rowId, rs, data.TableMetadata);

            // Then: The values I provided should be available
            Assert.AreEqual(rowId, rc.RowId);
            Assert.AreEqual(rs, rc.AssociatedResultSet);
            Assert.AreEqual(data.TableMetadata, rc.AssociatedObjectMetadata);
        }

        #region GetScript Tests

        public static IEnumerable<object[]> GetScriptMissingCellsData
        {
            get
            {
                // NOTE: Test matrix is defined in TableTestMatrix.txt, test cases here are identified by test ID
                yield return new object[] {true, 0, 0, 2};    // 02
                yield return new object[] {true, 0, 0, 4};    // 03
                yield return new object[] {true, 0, 1, 4};    // 06
                yield return new object[] {true, 1, 0, 4};    // 12
                yield return new object[] {true, 1, 1, 4};    // 16
                yield return new object[] {false, 0, 0, 1};   // 21
                yield return new object[] {false, 0, 0, 3};   // 22
                yield return new object[] {false, 0, 1, 3};   // 25
                yield return new object[] {false, 1, 0, 3};   // 31
                yield return new object[] {false, 1, 1, 3};   // 35
            }
        }

        [Test]
        [TestCaseSource(nameof(GetScriptMissingCellsData))]
        public async Task GetScriptMissingCell(bool includeIdentity, int defaultCols, int nullableCols, int valuesToSkipSetting)
        {
            // Setup: Generate the parameters for the row create
            var data = new Common.TestDbColumnsWithTableMetadata(false, includeIdentity, defaultCols, nullableCols);
            var rs = await Common.GetResultSet(data.DbColumns, includeIdentity);
            var rc = new RowCreate(100, rs, data.TableMetadata);

            // If: I ask for a script to be generated without setting any values
            // Then: An exception should be thrown for missing cells
            Assert.Throws<InvalidOperationException>(() => rc.GetScript());
        }

        public static IEnumerable<object[]> GetScriptData
        {
            get
            {
                // NOTE: Test matrix is defined in TableTestMatrix.txt, test cases here are identified by test ID
                yield return new object[] {true, 0, 0, 1, new RegexExpectedOutput(3, 3, 0)};    // 01
                yield return new object[] {true, 0, 1, 1, new RegexExpectedOutput(3, 3, 0)};    // 04
                yield return new object[] {true, 0, 1, 2, new RegexExpectedOutput(2, 2, 0)};    // 05
                yield return new object[] {true, 0, 3, 1, new RegexExpectedOutput(3, 3, 0)};    // 07
                yield return new object[] {true, 0, 3, 2, new RegexExpectedOutput(2, 2, 0)};    // 08
                yield return new object[] {true, 0, 3, 4, null};                                // 09
                yield return new object[] {true, 1, 0, 1, new RegexExpectedOutput(3, 3, 0)};    // 10
                yield return new object[] {true, 1, 0, 2, new RegexExpectedOutput(2, 2, 0)};    // 11
                yield return new object[] {true, 1, 1, 1, new RegexExpectedOutput(3, 3, 0)};    // 13
                yield return new object[] {true, 1, 1, 2, new RegexExpectedOutput(2, 2, 0)};    // 14
                yield return new object[] {true, 1, 1, 3, new RegexExpectedOutput(1, 1, 0)};    // 15
                yield return new object[] {true, 3, 0, 1, new RegexExpectedOutput(3, 3, 0)};    // 17
                yield return new object[] {true, 3, 0, 2, new RegexExpectedOutput(2, 2, 0)};    // 18
                yield return new object[] {true, 3, 0, 4, null};                                // 19
                yield return new object[] {false, 0, 0, 0, new RegexExpectedOutput(3, 3, 0)};   // 20
                yield return new object[] {false, 0, 1, 0, new RegexExpectedOutput(3, 3, 0)};   // 23
                yield return new object[] {false, 0, 1, 1, new RegexExpectedOutput(2, 2, 0)};   // 24
                yield return new object[] {false, 0, 3, 0, new RegexExpectedOutput(3, 3, 0)};   // 26
                yield return new object[] {false, 0, 3, 1, new RegexExpectedOutput(2, 2, 0)};   // 27
                yield return new object[] {false, 0, 3, 3, null};                               // 28
                yield return new object[] {false, 1, 0, 0, new RegexExpectedOutput(3, 3, 0)};   // 29
                yield return new object[] {false, 1, 0, 1, new RegexExpectedOutput(2, 2, 0)};   // 30
                yield return new object[] {false, 1, 1, 0, new RegexExpectedOutput(3, 3, 0)};   // 32
                yield return new object[] {false, 1, 1, 1, new RegexExpectedOutput(2, 2, 0)};   // 33
                yield return new object[] {false, 1, 1, 2, new RegexExpectedOutput(1, 1, 0)};   // 34
                yield return new object[] {false, 3, 0, 0, new RegexExpectedOutput(3, 3, 0)};   // 36
                yield return new object[] {false, 3, 0, 1, new RegexExpectedOutput(2, 2, 0)};   // 37
                yield return new object[] {false, 3, 0, 3, null};                               // 38
            }
        }
        
        [Test]
        [TestCaseSource(nameof(GetScriptData))]
        public async Task GetScript(bool includeIdentity, int colsWithDefaultConstraints, int colsThatAllowNull, int valuesToSkipSetting, RegexExpectedOutput expectedOutput)
        {
            // Setup: 
            // ... Generate the parameters for the row create
            var data = new Common.TestDbColumnsWithTableMetadata(false, includeIdentity, colsWithDefaultConstraints, colsThatAllowNull);
            ResultSet rs = await Common.GetResultSet(data.DbColumns, includeIdentity);
            
            // ... Create a row create and set the appropriate number of cells 
            var rc = new RowCreate(100, rs, data.TableMetadata);
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
                Match m = GetInsert1Regex().Match(sql);
                Assert.True(m.Success);
                
                // Table name matches
                Assert.AreEqual(Common.TableName, m.Groups[1].Value);
            }
            else
            {
                // Do the whole validation
                Match m = GetInsert2Regex().Match(sql);
                Assert.True(m.Success);
                
                // Table name matches
                Assert.AreEqual(Common.TableName, m.Groups[1].Value);
                
                // In columns match
                string cols = m.Groups[2].Value;
                Assert.AreEqual(expectedOutput.ExpectedInColumns, cols.Split(',').Length);
                
                // In values match
                string vals = m.Groups[3].Value;               
                Assert.AreEqual(expectedOutput.ExpectedInValues, vals.Split(',').Length);
            }
        }
        
        #endregion

        [Test]
        public async Task ApplyChanges([Values]bool includeIdentity)
        {
            // Setup: 
            // ... Generate the parameters for the row create
            const long rowId = 100;
            var data = new Common.TestDbColumnsWithTableMetadata(false, includeIdentity, 0, 0);
            ResultSet rs = await Common.GetResultSet(data.DbColumns, includeIdentity);

            // ... Setup a db reader for the result of an insert
            var newRowReader = Common.GetNewRowDataReader(data.DbColumns, includeIdentity);

            // If: I ask for the change to be applied
            var rc = new RowCreate(rowId, rs, data.TableMetadata);
            rc.ApplyChanges(newRowReader);

            // Then: The result set should have an additional row in it
            Assert.AreEqual(2, rs.RowCount);
        }

        #region GetCommand Tests

        [Test]
        public async Task GetCommandNullConnection()
        {
            // Setup: Create a row create
            RowCreate rc = await GetStandardRowCreate();

            // If: I attempt to create a command with a null connection
            // Then: It should throw an exception
            Assert.Throws<ArgumentNullException>(() => rc.GetCommand(null));
        }

        public static IEnumerable<object[]> GetCommandMissingCellsData
        {
            get
            {
                // NOTE: Test matrix is defined in TableTestMatrix.txt, test cases here are identified by test ID
                yield return new object[] {true, 0, 0, 2};    // 02
                yield return new object[] {true, 0, 0, 4};    // 03
                yield return new object[] {true, 0, 1, 4};    // 06
                yield return new object[] {true, 1, 0, 4};    // 12
                yield return new object[] {true, 1, 1, 4};    // 16
                yield return new object[] {false, 0, 0, 1};   // 21
                yield return new object[] {false, 0, 0, 3};   // 22
                yield return new object[] {false, 0, 1, 3};   // 25
                yield return new object[] {false, 1, 0, 3};   // 31
                yield return new object[] {false, 1, 1, 3};   // 35
            }
        }

        [Test]
        [TestCaseSource(nameof(GetCommandMissingCellsData))]
        public async Task GetCommandMissingCellNoDefault(bool includeIdentity, int defaultCols, int nullableCols,
            int valuesToSkip)
        {
            // Setup: 
            // ... Generate the row create object
            var data = new Common.TestDbColumnsWithTableMetadata(false, includeIdentity, defaultCols, nullableCols);
            ResultSet rs = await Common.GetResultSet(data.DbColumns, includeIdentity);
            var rc = new RowCreate(100, rs, data.TableMetadata);
            
            // ... Create a mock db connection for building the command
            var mockConn = new TestSqlConnection();
            
            // If: I ask for a script to be generated without setting all the required values
            // Then: An exception should be thrown for the missing cells
            Assert.Throws<InvalidOperationException>(() => rc.GetCommand(mockConn));
        }

        public static IEnumerable<object[]> GetCommandData
        {
            get
            {
                // NOTE: Test matrix is defined in TableTestMatrix.txt, test cases here are identified by test ID
                yield return new object[] {true, 0, 0, 1, new RegexExpectedOutput(3, 3, 4)};    // 01
                yield return new object[] {true, 0, 1, 1, new RegexExpectedOutput(3, 3, 4)};    // 04
                yield return new object[] {true, 0, 1, 2, new RegexExpectedOutput(2, 2, 4)};    // 05
                yield return new object[] {true, 0, 3, 1, new RegexExpectedOutput(3, 3, 4)};    // 07
                yield return new object[] {true, 0, 3, 2, new RegexExpectedOutput(2, 2, 4)};    // 08
                yield return new object[] {true, 0, 3, 4, new RegexExpectedOutput(0, 0, 4)};    // 09
                yield return new object[] {true, 1, 0, 1, new RegexExpectedOutput(3, 3, 4)};    // 10
                yield return new object[] {true, 1, 0, 2, new RegexExpectedOutput(2, 2, 4)};    // 11
                yield return new object[] {true, 1, 1, 1, new RegexExpectedOutput(3, 3, 4)};    // 13
                yield return new object[] {true, 1, 1, 2, new RegexExpectedOutput(2, 2, 4)};    // 14
                yield return new object[] {true, 1, 1, 3, new RegexExpectedOutput(1, 1, 4)};    // 15
                yield return new object[] {true, 3, 0, 1, new RegexExpectedOutput(3, 3, 4)};    // 17
                yield return new object[] {true, 3, 0, 2, new RegexExpectedOutput(2, 2, 4)};    // 18
                yield return new object[] {true, 3, 0, 4, new RegexExpectedOutput(0, 0, 4)};    // 19
                yield return new object[] {false, 0, 0, 0, new RegexExpectedOutput(3, 3, 3)};   // 20
                yield return new object[] {false, 0, 1, 0, new RegexExpectedOutput(3, 3, 3)};   // 23
                yield return new object[] {false, 0, 1, 1, new RegexExpectedOutput(2, 2, 3)};   // 24
                yield return new object[] {false, 0, 3, 0, new RegexExpectedOutput(3, 3, 3)};   // 26
                yield return new object[] {false, 0, 3, 1, new RegexExpectedOutput(2, 2, 3)};   // 27
                yield return new object[] {false, 0, 3, 3, new RegexExpectedOutput(0, 0, 3)};   // 28
                yield return new object[] {false, 1, 0, 0, new RegexExpectedOutput(3, 3, 3)};   // 29
                yield return new object[] {false, 1, 0, 1, new RegexExpectedOutput(2, 2, 3)};   // 30
                yield return new object[] {false, 1, 1, 0, new RegexExpectedOutput(3, 3, 3)};   // 32
                yield return new object[] {false, 1, 1, 1, new RegexExpectedOutput(2, 2, 3)};   // 33
                yield return new object[] {false, 1, 1, 2, new RegexExpectedOutput(1, 1, 3)};   // 34
                yield return new object[] {false, 3, 0, 0, new RegexExpectedOutput(3, 3, 3)};   // 36
                yield return new object[] {false, 3, 0, 1, new RegexExpectedOutput(2, 2, 3)};   // 37
                yield return new object[] {false, 3, 0, 3, new RegexExpectedOutput(0, 0, 3)};   // 38
            }
        }
        
        [Test]
        [TestCaseSource(nameof(GetCommandData))]
        public async Task GetCommand(bool includeIdentity, int defaultCols, int nullableCols, int valuesToSkip, RegexExpectedOutput expectedOutput)
        {
            // Setup: 
            // ... Generate the parameters for the row create
            var data = new Common.TestDbColumnsWithTableMetadata(false, includeIdentity, defaultCols, nullableCols);
            ResultSet rs = await Common.GetResultSet(data.DbColumns, includeIdentity);
            
            // ... Mock db connection for building the command
            var mockConn = new TestSqlConnection(null);
            
            // ... Create a row create and set the appropriate number of cells 
            var rc = new RowCreate(100, rs, data.TableMetadata);
            Common.AddCells(rc, valuesToSkip);
            
            // If: I ask for the command for the row insert
            DbCommand cmd = rc.GetCommand(mockConn);
            
            // Then:
            // ... The command should not be null
            Assert.NotNull(cmd);
            
            // ... There should be parameters in it
            Assert.AreEqual(expectedOutput.ExpectedInValues, cmd.Parameters.Count);
            
            // ... The script should match the expected regex output
            ValidateCommandAgainstRegex(cmd.CommandText, expectedOutput);
        }

        private static void ValidateCommandAgainstRegex(string sql, RegexExpectedOutput expectedOutput)
        {
            // Break the query into parts
            string[] splitSql = sql.Split(Environment.NewLine);
            Assert.AreEqual(3, splitSql.Length);
            
            // Check the declare statement first
            Match declareMatch = GetDeclareRegex().Match(splitSql[0]);
            Assert.True(declareMatch.Success);
            
            // Declared table name matches
            Assert.True(declareMatch.Groups[1].Value.StartsWith("Insert"));
            Assert.True(declareMatch.Groups[1].Value.EndsWith("Output"));
            
            // Correct number of columns in declared table
            string[] declareCols = declareMatch.Groups[2].Value.Split(", ");
            Assert.AreEqual(expectedOutput.ExpectedOutColumns, declareCols.Length);
            
            // Check the insert statement in the middle 
            if (expectedOutput.ExpectedInColumns == 0 || expectedOutput.ExpectedInValues == 0)
            {
                // If expected output was null make sure we match the default values reges
                var insertRegex = GetInsertRegex();
                Match insertMatch = insertRegex.Match(splitSql[1]);
                Assert.True(insertMatch.Success);
                
                // Table name matches
                Assert.AreEqual(Common.TableName, insertMatch.Groups[1].Value);
                
                string[] outCols = insertMatch.Groups[2].Value.Split(", ");
                Assert.AreEqual(expectedOutput.ExpectedOutColumns, outCols.Length);
                Assert.That(outCols, Has.All.StartsWith("inserted."), "Output columns match");

                Assert.Multiple(() =>
                {
                    Assert.That(insertMatch.Groups[3].Value, Does.StartWith("Insert"), "Output table name matches");
                    Assert.That(insertMatch.Groups[3].Value, Does.EndWith("Output"));
                });
            }
            else
            {
                // Do the whole validation
                var insertRegex = GetInsertFullRegex();
                Match insertMatch = insertRegex.Match(splitSql[1]);
                Assert.True(insertMatch.Success);
                
                // Table name matches
                Assert.AreEqual(Common.TableName, insertMatch.Groups[1].Value);
                
                // 
                string[] outCols = insertMatch.Groups[3].Value.Split(", ");
                Assert.Multiple(() =>
                {
                    Assert.AreEqual(expectedOutput.ExpectedOutColumns, outCols.Length);
                    Assert.That(outCols, Has.All.StartsWith("inserted."), "Output columns match");
                });
                
                // In columns match
                string[] inCols = insertMatch.Groups[2].Value.Split(", ");
                Assert.AreEqual(expectedOutput.ExpectedInColumns, inCols.Length);

                // Output table name matches
                Assert.Multiple(() =>
                {
                    Assert.That(insertMatch.Groups[4].Value, Does.StartWith("Insert"));
                    Assert.That(insertMatch.Groups[4].Value, Does.EndWith("Output"));
                });

                // In values match
                string[] inVals = insertMatch.Groups[5].Value.Split(", ");
                Assert.AreEqual(expectedOutput.ExpectedInValues, inVals.Length);
                Assert.That(inVals, Has.All.Match(@"@.+\d+_\d+"));
            }
            
            // Check the select statement last
            var selectRegex = GetSelectRegex();
            Match selectMatch = selectRegex.Match(splitSql[2]);
            Assert.True(selectMatch.Success);
            
            // Correct number of columns in declared table
            string[] selectCols = selectMatch.Groups[1].Value.Split(", ");
            Assert.AreEqual(expectedOutput.ExpectedOutColumns, selectCols.Length);
            
            // Declared table name matches
            Assert.True(selectMatch.Groups[2].Value.StartsWith("Insert"));
            Assert.True(selectMatch.Groups[2].Value.EndsWith("Output"));
        }
        
        #endregion
        
        #region GetEditRow Tests

        [Test]
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
            Assert.AreEqual(EditRow.EditRowState.DirtyInsert, er.State);

            // ... The row should have a bunch of empty cells (equal to number of columns) and all are dirty
            Assert.AreEqual(rc.newCells.Length, er.Cells.Length);
            Assert.That(er.Cells.Select(c => c.DisplayValue), Has.All.Empty);
            Assert.That(er.Cells.Select(ec => ec.IsDirty), Has.All.True);
        }

        [Test]
        public async Task GetEditRowWithDefaultValue()
        {
            // Setup: Generate a row create with default values
            const long rowId = 100;
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, 3, 0);
            ResultSet rs = await Common.GetResultSet(data.DbColumns, false);
            var rc = new RowCreate(rowId, rs, data.TableMetadata);
            
            // If: I request an edit row from the row create
            EditRow er = rc.GetEditRow(null);
            
            // Then:
            // ... The row should not be null
            Assert.NotNull(er);
            
            // ... The row should not be clean
            Assert.True(er.IsDirty);
            Assert.AreEqual(EditRow.EditRowState.DirtyInsert, er.State);
            
            // ... The row sould have a bunch of default values (equal to number of columns) and all are dirty
            Assert.AreEqual(rc.newCells.Length, er.Cells.Length);
            Assert.That(er.Cells.Select(ec => ec.DisplayValue), Has.All.EqualTo(Common.DefaultValue));
            Assert.That(er.Cells.Select(ec => ec.IsDirty), Has.All.True);
        }

        [Test]
        public async Task GetEditRowWithCalculatedValue()
        {
            // Setup: Generate a row create with an identity column
            const long rowId = 100;
            var data = new Common.TestDbColumnsWithTableMetadata(false, true, 0, 0);
            ResultSet rs = await Common.GetResultSet(data.DbColumns, true);
            var rc = new RowCreate(rowId, rs, data.TableMetadata);
            
            // If: I request an edit row from the row created
            EditRow er = rc.GetEditRow(null);
            
            // Then:
            // ... The row should not be null
            Assert.NotNull(er);
            Assert.AreEqual(er.Id, rowId);
            
            // ... The row should not be clean
            Assert.True(er.IsDirty);
            Assert.AreEqual(EditRow.EditRowState.DirtyInsert, er.State);
            
            // ... The row should have a TBD for the identity column
            Assert.AreEqual(rc.newCells.Length, er.Cells.Length);
            Assert.AreEqual(SR.EditDataComputedColumnPlaceholder, er.Cells[0].DisplayValue);
            Assert.False(er.Cells[0].IsNull);
            Assert.True(er.Cells[0].IsDirty);

            // ... The rest of the cells should have empty display values
            Assert.That(er.Cells.Skip(1).Select(ec => new { ec.DisplayValue, ec.IsNull, ec.IsDirty }), Has.All.EqualTo(new { DisplayValue = string.Empty, IsNull = false, IsDirty = true }));
        }

        [Test]
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
            Assert.AreEqual(EditRow.EditRowState.DirtyInsert, er.State);

            // ... The row should not be clean
            Assert.True(er.IsDirty);
            Assert.AreEqual(EditRow.EditRowState.DirtyInsert, er.State);

            // ... The row should have a single non-empty cell at the beginning that is dirty
            Assert.AreEqual(setValue, er.Cells[0].DisplayValue);
            Assert.False(er.Cells[0].IsNull);
            Assert.True(er.Cells[0].IsDirty);

            // ... The rest of the cells should be blank, but dirty
            for (int i = 1; i < er.Cells.Length; i++)
            {
                EditCell ec = er.Cells[i];
                Assert.AreEqual(string.Empty, ec.DisplayValue);
                Assert.False(ec.IsNull);
                Assert.True(ec.IsDirty);
            }
        }

        #endregion

        #region SetCell Tests
        
        [Test]
        public async Task SetCellOutOfRange([Values(-1, 3, 100)]int columnId)
        {
            // Setup: Generate a row create
            RowCreate rc = await GetStandardRowCreate();

            // If: I attempt to set a cell on a column that is out of range, I should get an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => rc.SetCell(columnId, string.Empty));
        }

        [Test]
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
            Assert.AreEqual(updateValue, eucr.Cell.DisplayValue);
            Assert.False(eucr.Cell.IsNull);

            // ... The returned value should be dirty
            Assert.NotNull(eucr.Cell.IsDirty);

            // ... The row should still be dirty
            Assert.True(eucr.IsRowDirty);

            // ... There should be a cell update in the cell list
            Assert.NotNull(rc.newCells[0]);
        }

        [Test]
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
            var etm = Common.GetCustomEditTableMetadata(cols);

            // ... Create the row create
            var rc = new RowCreate(100, rs, etm);

            // If: I set a cell in the newly created row to something that will be corrected
            EditUpdateCellResult eucr = rc.SetCell(0, "1000");

            // Then:
            // ... The returned value should be equal to what we provided
            Assert.NotNull(eucr);
            Assert.NotNull(eucr.Cell);
            Assert.That(eucr.Cell.DisplayValue, Is.Not.EqualTo("1000"));
            Assert.False(eucr.Cell.IsNull);

            // ... The returned value should be dirty
            Assert.NotNull(eucr.Cell.IsDirty);

            // ... The row should still be dirty
            Assert.True(eucr.IsRowDirty);

            // ... There should be a cell update in the cell list
            Assert.NotNull(rc.newCells[0]);
        }

        [Test]
        public async Task SetCellNull()
        {
            // Setup: Generate a row create
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 3);
            var rs = await Common.GetResultSet(data.DbColumns, false);
            var rc = new RowCreate(100, rs, data.TableMetadata);

            // If: I set a cell in the newly created row to null
            const string nullValue = "NULL";
            EditUpdateCellResult eucr = rc.SetCell(0, nullValue);

            // Then:
            // ... The returned value should be equal to what we provided
            Assert.NotNull(eucr);
            Assert.NotNull(eucr.Cell);
            Assert.AreEqual(nullValue, eucr.Cell.DisplayValue);
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
        
        [Test]
        public async Task RevertCellOutOfRange([Values(-1,3,100)]int columnId)
        {
            // Setup: Generate the row create
            RowCreate rc = await GetStandardRowCreate();

            // If: I attempt to revert a cell that is out of range
            // Then: I should get an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => rc.RevertCell(columnId));
        }

        [Test]
        public async Task RevertCellNotSet([Values(0,1)]int defaultCols)
        {
            // Setup: 
            // ... Generate the parameters for the row create
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, defaultCols, 0);
            ResultSet rs = await Common.GetResultSet(data.DbColumns, false);
            var rc = new RowCreate(100, rs, data.TableMetadata);

            // If: I attempt to revert a cell that has not been set
            EditRevertCellResult result = rc.RevertCell(0);

            // Then:
            // ... We should get a result back
            Assert.NotNull(result);

            // ... We should get back an edit cell with a value based on the default value
            string expectedDisplayValue = defaultCols > 0 ? Common.DefaultValue : string.Empty; 
            Assert.NotNull(result.Cell);
            Assert.AreEqual(expectedDisplayValue, result.Cell.DisplayValue);
            Assert.False(result.Cell.IsNull);    // TODO: Modify to support null defaults

            // ... The row should be dirty
            Assert.True(result.IsRowDirty);

            // ... The cell should no longer be set
            Assert.Null(rc.newCells[0]);
        }

        [Test]
        public async Task RevertCellThatWasSet([Values(0, 1)] int defaultCols)
        {
            // Setup: 
            // ... Generate the parameters for the row create
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, defaultCols, 0);
            ResultSet rs = await Common.GetResultSet(data.DbColumns, false);
            var rc = new RowCreate(100, rs, data.TableMetadata);
            rc.SetCell(0, "1");

            // If: I attempt to revert a cell that was set
            EditRevertCellResult result = rc.RevertCell(0);

            // Then:
            // ... We should get a result back
            Assert.NotNull(result);

            // ... We should get back an edit cell with a value based on the default value
            string expectedDisplayValue = defaultCols > 0 ? Common.DefaultValue : string.Empty; 
            Assert.NotNull(result.Cell);
            Assert.AreEqual(expectedDisplayValue, result.Cell.DisplayValue);
            Assert.False(result.Cell.IsNull);    // TODO: Modify to support null defaults

            // ... The row should be dirty
            Assert.True(result.IsRowDirty);

            // ... The cell should no longer be set
            Assert.Null(rc.newCells[0]);
        }

        #endregion
        
        private static async Task<RowCreate> GetStandardRowCreate()
        {
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);
            var rs = await Common.GetResultSet(data.DbColumns, false);
            return new RowCreate(100, rs, data.TableMetadata);
        }

        public class RegexExpectedOutput
        {
            public RegexExpectedOutput(int expectedInColumns, int expectedInValues, int expectedOutColumns)
            {
                ExpectedInColumns = expectedInColumns;
                ExpectedInValues = expectedInValues;
                ExpectedOutColumns = expectedOutColumns;
            }
            
            public int ExpectedInColumns { get; }
            public int ExpectedInValues { get; }
            public int ExpectedOutColumns { get; }
        }

        #region Generated Regex
        [GeneratedRegex("INSERT INTO (.+) DEFAULT VALUES")]
        private static partial Regex GetInsert1Regex();

        [GeneratedRegex("INSERT INTO (.+)\\((.+)\\) VALUES \\((.+)\\)")]
        private static partial Regex GetInsert2Regex();

        [GeneratedRegex("^DECLARE @(.+) TABLE \\((.+)\\)$")]
        private static partial Regex GetDeclareRegex();

        [GeneratedRegex("^INSERT INTO (.+) OUTPUT (.+) INTO @(.+) DEFAULT VALUES$")]
        private static partial Regex GetInsertRegex();

        [GeneratedRegex("^INSERT INTO (.+)\\((.+)\\) OUTPUT (.+) INTO @(.+) VALUES \\((.+)\\)$")]
        private static partial Regex GetInsertFullRegex();

        [GeneratedRegex("^SELECT (.+) FROM @(.+)$")]
        private static partial Regex GetSelectRegex();
        #endregion
    }
}
