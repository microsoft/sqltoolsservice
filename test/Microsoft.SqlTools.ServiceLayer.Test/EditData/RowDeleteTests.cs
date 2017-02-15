
using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.EditData
{
    public class RowDeleteTests
    {
        [Fact]
        public void RowDeleteConstruction()
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
        public void GetScriptTest(bool isHekaton)
        {
            DbColumn[] columns = Common.GetColumns(true);
            ResultSet rs = Common.GetResultSet(columns, true);
            IEditTableMetadata etm = Common.GetMetadata(columns, false, isHekaton);

            // If: I ask for a script to be generated for delete
            RowDelete rd = new RowDelete(0, rs, etm);
            string script = rd.GetScript();

            // Then:
            // ... The script should not be null
            Assert.NotNull(script);

            // ... It should be formatted as a delete script
            string scriptStart = $"DELETE FROM {etm.EscapedMultipartName}";
            if (isHekaton)
            {
                scriptStart += " WITH(SNAPSHOT)";
            }
            Assert.StartsWith(scriptStart, script);
        }
    }
}
