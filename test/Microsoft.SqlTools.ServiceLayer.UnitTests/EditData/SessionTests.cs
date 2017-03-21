//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    public class SessionTests
    {
        #region Construction Tests

        [Fact]
        public void SessionConstructionNullMetadataFactory()
        {
            // If: I create a session object with a null metadata factory
            // Then: It should throw an exception
            Assert.Throws<ArgumentNullException>(() => new EditSession(null, Constants.OwnerUri, Constants.OwnerUri));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\r\n")]
        public void SessionConstructionNullObjectName(string objName)
        {
            // If: I create a session object with a null or whitespace object name
            // Then: It should throw an exception
            Mock<IEditMetadataFactory> mockFactory = new Mock<IEditMetadataFactory>();
            Assert.Throws<ArgumentException>(() => new EditSession(mockFactory.Object, objName, Constants.OwnerUri));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\r\n")]
        public void SessionConstructionNullObjectType(string objType)
        {
            // If: I create a session object with a null or whitespace object type
            // Then: It should throw an exception
            Mock<IEditMetadataFactory> mockFactory = new Mock<IEditMetadataFactory>();
            Assert.Throws<ArgumentException>(() => new EditSession(mockFactory.Object, Constants.OwnerUri, objType));
        }

        [Fact]
        public void SessionConstructionValid()
        {
            // If: I create a session object with a proper arguments
            Mock<IEditMetadataFactory> mockFactory = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(mockFactory.Object, Constants.OwnerUri, Constants.OwnerUri);

            // Then:
            // ... The edit cache should not exist
            Assert.Null(s.EditCache);

            // ... The session shouldn't be initialized
            Assert.False(s.IsInitialized);
            Assert.Null(s.EditCache);
            Assert.Null(s.CommitTask);

            // ... The next row ID should be the default long
            Assert.Equal(default(long), s.NextRowId);
        }

        #endregion

        #region Validate Tests

        [Fact]
        public void SessionValidateUnfinishedQuery()
        {
            // If: I create a session object with a query that hasn't finished execution
            // Then: It should throw an exception
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            q.HasExecuted = false;
            Assert.Throws<InvalidOperationException>(() => EditSession.ValidateQueryForSession(q));
        }

        [Fact]
        public void SessionValidateIncorrectResultSet()
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

            var fsf = MemoryFileSystem.GetFileStreamFactory();
            Query query = new Query(Constants.StandardQuery, ci, new QueryExecutionSettings(), fsf);
            query.Execute();
            query.ExecutionTask.Wait();

            // If: I create a session object with a query that has !=1 result sets
            // Then: It should throw an exception
            Assert.Throws<InvalidOperationException>(() => EditSession.ValidateQueryForSession(query));
        }

        [Fact]
        public void SessionValidateValidResultSet()
        {
            // If: I validate a query for a session with a valid query
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = EditSession.ValidateQueryForSession(q);

            // Then: I should get the only result set back
            Assert.NotNull(rs);
        }

        #endregion

        #region Create Row Tests

        [Fact]
        public void CreateRowNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object, Constants.OwnerUri, Constants.OwnerUri);

            // If: I ask to create a row without initializing
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.CreateRow());
        }

        [Fact]
        public async Task CreateRowAddFailure()
        {
            // NOTE: This scenario should theoretically never occur, but is tested for completeness
            // Setup:
            // ... Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            EditTableMetadata etm = Common.GetStandardMetadata(rs.Columns);
            EditSession s = await Common.GetCustomSession(q, etm);

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
        public async Task CreateRowSuccess()
        {
            // Setup: Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            EditTableMetadata etm = Common.GetStandardMetadata(rs.Columns);
            EditSession s = await Common.GetCustomSession(q, etm);

            // If: I add a row to the session
            EditCreateRowResult result = s.CreateRow();

            // Then:
            // ... The new ID should be equal to the row count
            Assert.Equal(rs.RowCount, result.NewRowId);

            // ... The next row ID should have been incremented
            Assert.Equal(rs.RowCount + 1, s.NextRowId);

            // ... There should be a new row create object in the cache
            Assert.Contains(result.NewRowId, s.EditCache.Keys);
            Assert.IsType<RowCreate>(s.EditCache[result.NewRowId]);

            // ... The default values should be returned (we will test this in depth below)
            Assert.NotEmpty(result.DefaultValues);
        }

        [Fact]
        public async Task CreateRowDefaultTest()
        {
            // Setup:
            // ... We will have 3 columns
            DbColumnWrapper[] cols =
            {
                new DbColumnWrapper(new TestDbColumn("col1")),    // No default
                new DbColumnWrapper(new TestDbColumn("col2")),    // Has default (defined below)
                new DbColumnWrapper(new TestDbColumn("filler"))   // Filler column so we can use the common code
            };

            // ... Metadata provider will return 3 columns
            EditColumnMetadata[] metas =
            {
                new EditColumnMetadata                   // No default
                {
                    DefaultValue = null,
                    EscapedName = cols[0].ColumnName,
                },
                new EditColumnMetadata                   // Has default
                {
                    DefaultValue = "default",
                    EscapedName = cols[0].ColumnName,
                },
                new EditColumnMetadata()
            };
            var etm = new EditTableMetadata
            {
                Columns = metas,
                EscapedMultipartName = "tbl",
                IsMemoryOptimized = false
            };
            etm.Extend(cols);

            // ... Create a result set
            var q = Common.GetQuery(cols, false);

            // ... Create a session from all this
            EditSession s = await Common.GetCustomSession(q, etm);

            // If: I add a row to the session, on a table that has defaults
            var result = s.CreateRow();

            // Then:
            // ... Result should not be null, new row ID should be > 0
            Assert.NotNull(result);
            Assert.True(result.NewRowId > 0);

            // ... There should be 3 default values (3 columns)
            Assert.NotEmpty(result.DefaultValues);
            Assert.Equal(3, result.DefaultValues.Length);

            // ... There should be specific values for each kind of default
            Assert.Null(result.DefaultValues[0]);
            Assert.Equal("default", result.DefaultValues[1]);
        }

        #endregion

        [Theory]
        [MemberData(nameof(RowIdOutOfRangeData))]
        public async Task RowIdOutOfRange(long rowId, Action<EditSession, long> testAction)
        {
            // Setup: Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

            // If: I delete a row that is out of range for the result set
            // Then: I should get an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => testAction(s, rowId));
        }

        public static IEnumerable<object> RowIdOutOfRangeData
        {
            get
            {
                // Delete Row
                Action<EditSession, long> delAction = (s, l) => s.DeleteRow(l);
                yield return new object[] { -1L, delAction };
                yield return new object[] {(long) QueryExecution.Common.StandardRows, delAction};
                yield return new object[] { 100L, delAction };

                // Update Cell
                Action<EditSession, long> upAction = (s, l) => s.UpdateCell(l, 0, null);
                yield return new object[] { -1L, upAction };
                yield return new object[] {(long) QueryExecution.Common.StandardRows, upAction};
                yield return new object[] { 100L, upAction };

                // Revert Row
                Action<EditSession, long> revertRowAction = (s, l) => s.RevertRow(l);
                yield return new object[] {-1L, revertRowAction};
                yield return new object[] {0L, revertRowAction};    // This is invalid b/c there isn't an edit pending for this row
                yield return new object[] {(long) QueryExecution.Common.StandardRows, revertRowAction};
                yield return new object[] {100L, revertRowAction};

                // Revert Cell
                Action<EditSession, long> revertCellAction = (s, l) => s.RevertCell(l, 0);
                yield return new object[] {-1L, revertRowAction};
                yield return new object[] {0L, revertRowAction};    // This is invalid b/c there isn't an edit pending for this row
                yield return new object[] {(long) QueryExecution.Common.StandardRows, revertRowAction};
                yield return new object[] {100L, revertRowAction};
            }
        }

        #region Initialize Tests

        [Fact]
        public void InitializeAlreadyInitialized()
        {
            // Setup:
            // ... Create a session and fake that it has been initialized
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object, Constants.OwnerUri, Constants.OwnerUri);
            s.IsInitialized = true;

            // If: I initialize it
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.Initialize(null, null, null, null));
        }

        [Fact]
        public void InitializeAlreadyInitializing()
        {
            // Setup:
            // ... Create a session and fake that it is in progress of initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object, Constants.OwnerUri, Constants.OwnerUri);
            s.InitializeTask = new Task(() => { });

            // If: I initialize it
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.Initialize(null, null, null, null));
        }

        [Theory]
        [MemberData(nameof(InitializeNullParamsData))]
        public void InitializeNullParams(EditSession.Connector c, EditSession.QueryRunner qr,
            Func<Task> sh, Func<Exception, Task> fh)
        {
            // Setup:
            // ... Create a session that hasn't been initialized
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object, Constants.OwnerUri, Constants.OwnerUri);

            // If: I initialize it with a missing parameter
            // Then: It should throw an exception
            Assert.ThrowsAny<ArgumentException>(() => s.Initialize(c, qr, sh, fh));
        }

        public static IEnumerable<object> InitializeNullParamsData
        {
            get
            {
                yield return new object[] {null, DoNothingQueryRunner, DoNothingSuccessHandler, DoNothingFailureHandler};
                yield return new object[] {DoNothingConnector, null, DoNothingSuccessHandler, DoNothingFailureHandler};
                yield return new object[] {DoNothingConnector, DoNothingQueryRunner, null, DoNothingFailureHandler};
                yield return new object[] {DoNothingConnector, DoNothingQueryRunner, DoNothingSuccessHandler, null};
            }
        }

        [Fact]
        public async Task InitializeMetadataFails()
        {
            // Setup:
            // ... Create a metadata factory that throws
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            emf.Setup(f => f.GetObjectMetadata(It.IsAny<DbConnection>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws<Exception>();

            // ... Create a session that hasn't been initialized
            EditSession s = new EditSession(emf.Object, Constants.OwnerUri, Constants.OwnerUri);

            // ... Create a mock for verifying the failure handler will be called
            var successHandler = DoNothingSuccessMock;
            var failureHandler = DoNothingFailureMock;

            // If: I initalize the session with a metadata factory that will fail
            s.Initialize(DoNothingConnector, DoNothingQueryRunner, successHandler.Object, failureHandler.Object);
            await s.InitializeTask;

            // Then:
            // ... The session should not be initialized
            Assert.False(s.IsInitialized);

            // ... The failure handler should have been called once
            failureHandler.Verify(f => f(It.IsAny<Exception>()), Times.Once);

            // ... The success handler should not have been called at all
            successHandler.Verify(f => f(), Times.Never);
        }

        [Fact]
        public async Task InitializeQueryFailException()
        {
            // Setup:
            // ... Create a metadata factory that will return some generic column information
            var b = QueryExecution.Common.GetBasicExecutedBatch();
            var etm = Common.GetStandardMetadata(b.ResultSets[0].Columns);
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            emf.Setup(f => f.GetObjectMetadata(It.IsAny<DbConnection>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(etm);

            // ... Create a session that hasn't been initialized
            EditSession s = new EditSession(emf.Object, Constants.OwnerUri, Constants.OwnerUri);

            // ... Create a query runner that will fail via exception
            Mock<EditSession.QueryRunner> qr = new Mock<EditSession.QueryRunner>();
            qr.Setup(r => r(It.IsAny<string>())).Throws(new Exception("qqq"));

            // ... Create a mock for verifying the failure handler will be called
            var successHandler = DoNothingSuccessMock;
            var failureHandler = DoNothingFailureMock;

            // If: I initialize the session with a query runner that will fail
            s.Initialize(DoNothingConnector, qr.Object, successHandler.Object, failureHandler.Object);
            await s.InitializeTask;

            // Then:
            // ... The session should not be initialized
            Assert.False(s.IsInitialized);

            // ... The failure handler should have been called once
            failureHandler.Verify(f => f(It.IsAny<Exception>()), Times.Once);

            // ... The success handler should not have been called at all
            successHandler.Verify(f => f(), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("It fail.")]
        public async Task InitializeQueryFailReturnNull(string message)
        {
            // Setup:
            // ... Create a metadata factory that will return some generic column information
            var b = QueryExecution.Common.GetBasicExecutedBatch();
            var etm = Common.GetStandardMetadata(b.ResultSets[0].Columns);
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            emf.Setup(f => f.GetObjectMetadata(It.IsAny<DbConnection>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(etm);

            // ... Create a session that hasn't been initialized
            EditSession s = new EditSession(emf.Object, Constants.OwnerUri, Constants.OwnerUri);

            // ... Create a query runner that will fail via returning a null query
            Mock<EditSession.QueryRunner> qr = new Mock<EditSession.QueryRunner>();
            qr.Setup(r => r(It.IsAny<string>()))
                .Returns(Task.FromResult(new EditSession.EditSessionQueryExecutionState(null, message)));

            // ... Create a mock for verifying the failure handler will be called
            var successHandler = DoNothingSuccessMock;
            var failureHandler = DoNothingFailureMock;

            // If: I initialize the session with a query runner that will fail
            s.Initialize(DoNothingConnector, qr.Object, successHandler.Object, failureHandler.Object);
            await s.InitializeTask;

            // Then:
            // ... The session should not be initialized
            Assert.False(s.IsInitialized);

            // ... The failure handler should have been called once
            failureHandler.Verify(f => f(It.IsAny<Exception>()), Times.Once);

            // ... The success handler should not have been called at all
            successHandler.Verify(f => f(), Times.Never);
        }

        [Fact]
        public async Task InitializeSuccess()
        {
            // Setup:
            // ... Create a metadata factory that will return some generic column information
            var q = QueryExecution.Common.GetBasicExecutedQuery();
            var rs = q.Batches[0].ResultSets[0];
            var etm = Common.GetStandardMetadata(rs.Columns);
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            emf.Setup(f => f.GetObjectMetadata(It.IsAny<DbConnection>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(etm);

            // ... Create a session that hasn't been initialized
            EditSession s = new EditSession(emf.Object, Constants.OwnerUri, Constants.OwnerUri);

            // ... Create a query runner that will return a successful query
            Mock<EditSession.QueryRunner> qr = new Mock<EditSession.QueryRunner>();
            qr.Setup(r => r(It.IsAny<string>()))
                .Returns(Task.FromResult(new EditSession.EditSessionQueryExecutionState(q, null)));

            // ... Create a mock for verifying the failure handler will be called
            var successHandler = DoNothingSuccessMock;
            var failureHandler = DoNothingFailureMock;

            // If: I initialize the session with a query runner that will fail
            s.Initialize(DoNothingConnector, qr.Object, successHandler.Object, failureHandler.Object);
            await s.InitializeTask;

            // Then:
            // ... The failure handler should not have been called
            failureHandler.Verify(f => f(It.IsAny<Exception>()), Times.Never);

            // ... The success handler should have been called
            successHandler.Verify(f => f(), Times.Once);

            // ... The session should have been initialized
            Assert.True(s.IsInitialized);
            Assert.Equal(rs.RowCount, s.NextRowId);
            Assert.NotNull(s.EditCache);
            Assert.Empty(s.EditCache);
        }

        #endregion

        #region Delete Row Tests

        [Fact]
        public void DeleteRowNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object, Constants.OwnerUri, Constants.OwnerUri);

            // If: I ask to delete a row without initializing
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.DeleteRow(0));
        }

        [Fact]
        public async Task DeleteRowAddFailure()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

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
        public async Task DeleteRowSuccess()
        {
            // Setup: Create a session with a proper query and metadata
            var s = await GetBasicSession();

            // If: I add a row to the session
            s.DeleteRow(0);

            // Then: There should be a new row delete object in the cache
            Assert.Contains(0, s.EditCache.Keys);
            Assert.IsType<RowDelete>(s.EditCache[0]);
        }

        #endregion

        #region Revert Row Tests

        [Fact]
        public void RevertRowNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object, Constants.OwnerUri, Constants.OwnerUri);

            // If: I ask to revert a row without initializing
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.RevertRow(0));
        }

        [Fact]
        public async Task RevertRowSuccess()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

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

        #region Revert Cell Tests

        [Fact]
        public void RevertCellNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object, Constants.OwnerUri, Constants.OwnerUri);

            // If: I ask to revert a cell without initializing
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.RevertCell(0, 0));
        }

        #endregion

        #region Update Cell Tests

        [Fact]
        public void UpdateCellNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object, Constants.OwnerUri, Constants.OwnerUri);

            // If: I ask to update a cell without initializing
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.UpdateCell(0, 0, ""));
        }

        [Fact]
        public async Task UpdateCellExisting()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

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
        public async Task UpdateCellNew()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

            // If: I update a cell on a row that does not have a pending edit
            s.UpdateCell(0, 0, "");

            // Then:
            // ... A new update row edit should have been added to the cache
            Assert.Contains(0, s.EditCache.Keys);
            Assert.IsType<RowUpdate>(s.EditCache[0]);
        }

        #endregion

        #region Script Edits Tests

        [Fact]
        public void ScriptEditsNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object, Constants.OwnerUri, Constants.OwnerUri);

            // If: I ask to script edits without initializing
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.ScriptEdits(string.Empty));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\r\n")]
        public async Task ScriptNullOrEmptyOutput(string outputPath)
        {
            // Setup: Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

            // If: I try to script the edit cache with a null or whitespace output path
            // Then: It should throw an exception
            Assert.Throws<ArgumentNullException>(() => s.ScriptEdits(outputPath));
        }

        [Fact]
        public async Task ScriptProvidedOutputPath()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

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

        #region Commit Tests

        [Fact]
        public void CommitEditsNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object, Constants.OwnerUri, Constants.OwnerUri);

            // If: I ask to script edits without initializing
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.CommitEdits(null, null, null));
        }

        [Fact]
        public async Task CommitNullConnection()
        {
            // Setup: Create a basic session
            EditSession s = await GetBasicSession();

            // If: I attempt to commit with a null connection
            // Then: I should get an exception
            Assert.Throws<ArgumentNullException>(
                () => s.CommitEdits(null, () => Task.CompletedTask, e => Task.CompletedTask));
        }

        [Fact]
        public async Task CommitNullSuccessHandler()
        {
            // Setup: 
            // ... Create a basic session
            EditSession s = await GetBasicSession();

            // ... Mock db connection
            DbConnection conn = new TestSqlConnection(null);

            // If: I attempt to commit with a null success handler
            // Then: I should get an exception
            Assert.Throws<ArgumentNullException>(() => s.CommitEdits(conn, null, e => Task.CompletedTask));
        }

        [Fact]
        public async Task CommitNullFailureHandler()
        {
            // Setup: 
            // ... Create a basic session
            EditSession s = await GetBasicSession();

            // ... Mock db connection
            DbConnection conn = new TestSqlConnection(null);

            // If: I attempt to commit with a null success handler
            // Then: I should get an exception
            Assert.Throws<ArgumentNullException>(() => s.CommitEdits(conn, () => Task.CompletedTask, null));
        }

        [Fact]
        public async Task CommitInProgress()
        {
            // Setup: 
            // ... Basic session and db connection
            EditSession s = await GetBasicSession();
            DbConnection conn = new TestSqlConnection(null);

            // ... Mock a task that has not completed
            Task notCompleted = new Task(() => {});
            s.CommitTask = notCompleted;

            // If: I attempt to commit while a task is in progress
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(
                () => s.CommitEdits(conn, () => Task.CompletedTask, e => Task.CompletedTask));
        }

        [Fact]
        public async Task CommitSuccess()
        {
            // Setup:
            // ... Basic session and db connection
            EditSession s = await GetBasicSession();
            DbConnection conn = new TestSqlConnection(null);

            // ... Add a mock commands for fun
            Mock<RowEditBase> edit = new Mock<RowEditBase>();
            edit.Setup(e => e.GetCommand(It.IsAny<DbConnection>())).Returns<DbConnection>(dbc => dbc.CreateCommand());
            edit.Setup(e => e.ApplyChanges(It.IsAny<DbDataReader>())).Returns(Task.FromResult(0));
            s.EditCache[0] = edit.Object;

            // If: I commit these changes (and await completion)
            bool successCalled = false;
            bool failureCalled = false;
            s.CommitEdits(conn, 
                () => {
                    successCalled = true;
                    return Task.FromResult(0);
                },
                e => {
                    failureCalled = true;
                    return Task.FromResult(0);
                });
            await s.CommitTask;

            // Then:
            // ... The task should still exist
            Assert.NotNull(s.CommitTask);

            // ... The success handler should have been called (not failure)
            Assert.True(successCalled);
            Assert.False(failureCalled);

            // ... The mock edit should have generated a command and applied changes
            edit.Verify(e => e.GetCommand(conn), Times.Once);
            edit.Verify(e => e.ApplyChanges(It.IsAny<DbDataReader>()), Times.Once);

            // ... The edit cache should be empty
            Assert.Empty(s.EditCache);
        }

        [Fact]
        public async Task CommitFailure()
        {
            // Setup:
            // ... Basic session and db connection
            EditSession s = await GetBasicSession();
            DbConnection conn = new TestSqlConnection(null);

            // ... Add a mock edit that will explode on generating a command
            Mock<RowEditBase> edit = new Mock<RowEditBase>();
            edit.Setup(e => e.GetCommand(It.IsAny<DbConnection>())).Throws<Exception>();
            s.EditCache[0] = edit.Object;

            // If: I commit these changes (and await completion)
            bool successCalled = false;
            bool failureCalled = false;
            s.CommitEdits(conn,
                () => {
                    successCalled = true;
                    return Task.FromResult(0);
                },
                e => {
                    failureCalled = true;
                    return Task.FromResult(0);
                });
            await s.CommitTask;

            // Then:
            // ... The task should still exist
            Assert.NotNull(s.CommitTask);

            // ... The error handler should have been called (not success)
            Assert.False(successCalled);
            Assert.True(failureCalled);

            // ... The mock edit should have been asked to generate a command
            edit.Verify(e => e.GetCommand(conn), Times.Once);

            // ... The edit cache should not be empty
            Assert.NotEmpty(s.EditCache);
        }

        #endregion

        private static EditSession.Connector DoNothingConnector
        {
            get { return () => Task.FromResult<DbConnection>(null); }
        }

        private static EditSession.QueryRunner DoNothingQueryRunner
        {
            get { return q => Task.FromResult<EditSession.EditSessionQueryExecutionState>(null); }
        }

        private static Func<Task> DoNothingSuccessHandler
        {
            get { return () => Task.FromResult(0); }
        }

        private static Func<Exception, Task> DoNothingFailureHandler
        {
            get { return e => Task.FromResult(0); }
        }

        private static Mock<Func<Task>> DoNothingSuccessMock
        {
            get {
                Mock<Func<Task>> successHandler = new Mock<Func<Task>>();
                successHandler.Setup(f => f()).Returns(Task.FromResult(0));
                return successHandler;
            }
        }

        private static Mock<Func<Exception, Task>> DoNothingFailureMock
        {
            get
            {
                Mock<Func<Exception, Task>> failureHandler = new Mock<Func<Exception, Task>>();
                failureHandler.Setup(f => f(It.IsAny<Exception>())).Returns(Task.FromResult(0));
                return failureHandler;
            }
        }

        private static async Task<EditSession> GetBasicSession()
        {
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            EditTableMetadata etm = Common.GetStandardMetadata(rs.Columns);
            return await Common.GetCustomSession(q, etm);
        }
    }
}
