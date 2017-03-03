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
    public class RowCreateTests
    {
        [Fact]
        public void RowCreateConstruction()
        {
            // Setup: Create the values to store
            const long rowId = 100;
            ResultSet rs = QueryExecution.Common.GetBasicExecutedBatch().ResultSets[0];
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);

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
            DbColumn[] columns = Common.GetColumns(includeIdentity);
            ResultSet rs = Common.GetResultSet(columns, includeIdentity);
            IEditTableMetadata etm = Common.GetMetadata(columns);

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
        public void GetScriptMissingCell()
        {
            // Setup: Generate the parameters for the row create
            const long rowId = 100;
            DbColumn[] columns = Common.GetColumns(false);
            ResultSet rs = Common.GetResultSet(columns, false);
            IEditTableMetadata etm = Common.GetMetadata(columns);

            // If: I ask for a script to be generated without setting any values
            // Then: An exception should be thrown for missing cells
            RowCreate rc = new RowCreate(rowId, rs, etm);
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
            ResultSet rs = Common.GetResultSet(columns, includeIdentity);
            IEditTableMetadata etm = Common.GetMetadata(columns);

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
        public void GetCommand(bool includeIdentity)
        {
            // Setup:
            // ... Create a row create with cell updates
            const long rowId = 100;
            var columns = Common.GetColumns(includeIdentity);
            var rs = Common.GetResultSet(columns, includeIdentity);
            var etm = Common.GetMetadata(columns);
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
        public void GetCommandNullConnection()
        {
            // Setup: Create a row create
            const long rowId = 100;
            var columns = Common.GetColumns(false);
            var rs = Common.GetResultSet(columns, false);
            var etm = Common.GetMetadata(columns);
            RowCreate rc = new RowCreate(rowId, rs, etm);

            // If: I attempt to create a command with a null connection
            // Then: It should throw an exception
            Assert.Throws<ArgumentNullException>(() => rc.GetCommand(null));
        }

        [Fact]
        public void GetCommandMissingCell()
        {
            // Setup: Generate the parameters for the row create
            const long rowId = 100;
            var columns = Common.GetColumns(false);
            var rs = Common.GetResultSet(columns, false);
            var etm = Common.GetMetadata(columns);
            var mockConn = new TestSqlConnection(null);

            // If: I ask for a script to be generated without setting any values
            // Then: An exception should be thrown for missing cells
            RowCreate rc = new RowCreate(rowId, rs, etm);
            Assert.Throws<InvalidOperationException>(() => rc.GetCommand(mockConn));
        }
    }
}
