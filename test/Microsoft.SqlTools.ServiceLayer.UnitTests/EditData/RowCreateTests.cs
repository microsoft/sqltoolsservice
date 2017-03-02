//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
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
    }
}
