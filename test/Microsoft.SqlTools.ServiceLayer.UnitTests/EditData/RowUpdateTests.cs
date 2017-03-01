//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
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
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);

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
            IEditTableMetadata etm = Common.GetMetadata(columns);

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
        public void GetScriptTest(bool isHekaton)
        {
            // Setup: Create a fake table to update
            DbColumn[] columns = Common.GetColumns(true);
            ResultSet rs = Common.GetResultSet(columns, true);
            IEditTableMetadata etm = Common.GetMetadata(columns, false, isHekaton);

            // If: I ask for a script to be generated for update
            RowUpdate ru = new RowUpdate(0, rs, etm);
            Common.AddCells(ru, true);
            string script = ru.GetScript();

            // Then:
            // ... The script should not be null
            Assert.NotNull(script);

            // ... It should be formatted as an update script
            string regexString = isHekaton
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
    }
}
