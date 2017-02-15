using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.EditData
{
    public class SessionTests
    {
        #region Construction Tests

        [Fact]
        public void SessionConstructionNullQuery()
        {
            // If: I create a session object without a null query
            // Then: It should throw an exception
            Assert.Throws<ArgumentNullException>(() => new Session(null, Common.GetMetadata(new DbColumn[] {})));
        }

        [Fact]
        public void SessionConstructionNullMetadataProvider()
        {
            // If: I create a session object without a null metadata provider
            // Then: It should throw an exception
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            Assert.Throws<ArgumentNullException>(() => new Session(q, null));
        }

        [Fact]
        public void SessionConstructionUnfinishedQuery()
        {
            // If: I create a session object with a query that hasn't finished execution
            // Then: It should throw an exception
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            q.HasExecuted = false;
            IEditTableMetadata etm = Common.GetMetadata(q.Batches[0].ResultSets[0].Columns);
            Assert.Throws<InvalidOperationException>(() => new Session(q, etm));
        }

        [Fact]
        public void SessionConstructionIncorrectResultSet()
        {
            // Setup: Create a query that yields >1 result sets
            TestResultSet[] results =
            {
                QueryExecution.Common.StandardTestResultSet,
                QueryExecution.Common.StandardTestResultSet
            };

            // @TODO: Fix when the connection service is fixed
            ConnectionInfo ci = QueryExecution.Common.CreateConnectedConnectionInfo(results, false);
            ConnectionService.Instance.OwnerToConnectionMap[ci.OwnerUri] = ci;

            var fsf = QueryExecution.Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Query query = new Query(QueryExecution.Common.StandardQuery, ci, new QueryExecutionSettings(), fsf);
            query.Execute();
            query.ExecutionTask.Wait();
            IEditTableMetadata etm = Common.GetMetadata(query.Batches[0].ResultSets[0].Columns);

            // If: I create a session object with a query that has !=1 result sets
            // Then: It should throw an exception
            Assert.Throws<InvalidOperationException>(() => new Session(query, etm));
        }

        [Fact]
        public void SessionConstructionValid()
        {
            // If: I create a session object with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            IEditTableMetadata etm = Common.GetMetadata(q.Batches[0].ResultSets[0].Columns);
            Session s = new Session(q, etm);

            // Then:
            // ... The edit cache should exist and be empty
            Assert.NotNull(s.EditCache);
            Assert.Empty(s.EditCache);

            // ... The next row ID should be equivalent to the number of rows in the result set
            Assert.Equal(q.Batches[0].ResultSets[0].RowCount, s.NextRowId);
        }

        #endregion

        #region Create Row Tests

        [Fact]
        public void CreateRowAddFailure()
        {
            // NOTE: This scenario should theoretically never occur, but is tested for completeness
            // Setup:
            // ... Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);
            Session s = new Session(q, etm);

            // ... Add a mock edit to the edit cache to cause the .TryAdd to fail
            var mockEdit = new Mock<RowEditBase>().Object;
            s.EditCache[rs.RowCount] = mockEdit;

            // If: I create a row in the session
            // Then: 
            // ... An exception should be thrown
            Assert.Throws<InvalidOperationException>(() => s.CreateRow());

            // ... The mock edit should still exist
            Assert.Equal(mockEdit, s.EditCache[rs.RowCount]);

            // ... The next row ID should not have changes
            Assert.Equal(rs.RowCount, s.NextRowId);
        }

        [Fact]
        public void CreateRowSuccess()
        {
            // Setup: Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);
            Session s = new Session(q, etm);

            // If: I add a row to the session
            long newId = s.CreateRow();

            // Then:
            // ... The new ID should be equal to the row count
            Assert.Equal(rs.RowCount, newId);

            // ... The next row ID should have been incremented
            Assert.Equal(rs.RowCount + 1, s.NextRowId);

            // ... There should be a new row create object in the cache
            Assert.Contains(newId, s.EditCache.Keys);
            Assert.IsType<RowCreate>(s.EditCache[newId]);
        }

        #endregion

        [Theory]
        [MemberData(nameof(RowIdOutOfRangeData))]
        public void RowIdOutOfRange(long rowId, Action<Session, long> testAction)
        {
            // Setup: Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);
            Session s = new Session(q, etm);

            // If: I delete a row that is out of range for the result set
            // Then: I should get an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => testAction(s, rowId));
        }

        public static IEnumerable<object> RowIdOutOfRangeData
        {
            get
            {
                // Delete Row
                Action<Session, long> delAction = (s, l) => s.DeleteRow(l);
                yield return new object[] { -1L, delAction };
                yield return new object[] { 100L, delAction };

                // Update Cell
                Action<Session, long> upAction = (s, l) => s.UpdateCell(l, 0, null);
                yield return new object[] { -1L, upAction };
                yield return new object[] { 100L, upAction };
            }
        }

        #region Delete Row Tests

        [Fact]
        public void DeleteRowAddFailure()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);
            Session s = new Session(q, etm);

            // ... Add a mock edit to the edit cache to cause the .TryAdd to fail
            var mockEdit = new Mock<RowEditBase>().Object;
            s.EditCache[0] = mockEdit;

            // If: I delete a row in the session
            // Then: 
            // ... An exception should be thrown
            Assert.Throws<InvalidOperationException>(() => s.DeleteRow(0));

            // ... The mock edit should still exist
            Assert.Equal(mockEdit, s.EditCache[0]);
        }

        [Fact]
        public void DeleteRowSuccess()
        {
            // Setup: Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);
            Session s = new Session(q, etm);

            // If: I add a row to the session
            s.DeleteRow(0);

            // Then: There should be a new row delete object in the cache
            Assert.Contains(0, s.EditCache.Keys);
            Assert.IsType<RowDelete>(s.EditCache[0]);
        }

        #endregion

        #region Revert Row Tests

        [Fact]
        public void RevertRowOutOfRange()
        {
            // Setup: Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);
            Session s = new Session(q, etm);

            // If: I revert a row that doesn't have any pending changes
            // Then: I should get an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => s.RevertRow(0));
        }

        [Fact]
        public void RevertRowSuccess()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);
            Session s = new Session(q, etm);

            // ... Add a mock edit to the edit cache to cause the .TryAdd to fail
            var mockEdit = new Mock<RowEditBase>().Object;
            s.EditCache[0] = mockEdit;

            // If: I revert the row that has a pending update
            s.RevertRow(0);

            // Then:
            // ... The edit cache should not contain a pending edit for the row
            Assert.DoesNotContain(0, s.EditCache.Keys);
        }

        #endregion

        #region Update Cell Tests

        [Fact]
        public void UpdateCellExisting()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);
            Session s = new Session(q, etm);

            // ... Add a mock edit to the edit cache to cause the .TryAdd to fail
            var mockEdit = new Mock<RowEditBase>();
            mockEdit.Setup(e => e.SetCell(It.IsAny<int>(), It.IsAny<string>()));
            s.EditCache[0] = mockEdit.Object;

            // If: I update a cell on a row that already has a pending edit
            s.UpdateCell(0, 0, null);

            // Then: 
            // ... The mock update should still be in the cache
            // ... And it should have had set cell called on it
            Assert.Contains(mockEdit.Object, s.EditCache.Values);
        }

        [Fact]
        public void UpdateCellNew()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);
            Session s = new Session(q, etm);

            // If: I update a cell on a row that does not have a pending edit
            s.UpdateCell(0, 0, "");

            // Then:
            // ... A new update row edit should have been added to the cache
            Assert.Contains(0, s.EditCache.Keys);
            Assert.IsType<RowUpdate>(s.EditCache[0]);
        }

        #endregion

        #region Script Edits Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\r\n")]
        public void ScriptNullOrEmptyOutput(string outputPath)
        {
            // Setup: Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);
            Session s = new Session(q, etm);

            // If: I try to script the edit cache with a null or whitespace output path
            // Then: It should throw an exception
            Assert.Throws<ArgumentNullException>(() => s.ScriptEdits(outputPath));
        }

        [Fact]
        public void ScriptProvidedOutputPath()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);
            Session s = new Session(q, etm);

            // ... Add two mock edits that will generate a script
            Mock<RowEditBase> edit = new Mock<RowEditBase>();
            edit.Setup(e => e.GetScript()).Returns("test");
            s.EditCache[0] = edit.Object;
            s.EditCache[1] = edit.Object;

            using (SelfCleaningTempFile file = new SelfCleaningTempFile())
            {
                // If: I script the edit cache to a local output path
                string outputPath = s.ScriptEdits(file.FilePath);

                // Then: 
                // ... The output path used should be the same as the one we provided
                Assert.Equal(file.FilePath, outputPath);

                // ... The written file should have two lines, one for each edit
                Assert.Equal(2, File.ReadAllLines(outputPath).Length);
            }
        }

        #endregion
    }
}
