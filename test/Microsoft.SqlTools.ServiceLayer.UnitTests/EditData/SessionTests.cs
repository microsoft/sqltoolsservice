﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    public class SessionTests
    {
        #region Construction Tests

        [Test]
        public void SessionConstructionNullMetadataFactory()
        {
            // If: I create a session object with a null metadata factory
            // Then: It should throw an exception
            Assert.Throws<ArgumentNullException>(() => new EditSession(null));
        }

        [Test]
        public void SessionConstructionValid()
        {
            // If: I create a session object with a proper arguments
            Mock<IEditMetadataFactory> mockFactory = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(mockFactory.Object);

            // Then:
            // ... The edit cache should not exist
            Assert.Null(s.EditCache);

            // ... The session shouldn't be initialized
            Assert.False(s.IsInitialized);
            Assert.Null(s.EditCache);
            Assert.Null(s.CommitTask);

            // ... The next row ID should be the default long
            Assert.AreEqual(default(long), s.NextRowId);
        }

        #endregion

        #region Validate Tests

        [Test]
        public void SessionValidateUnfinishedQuery()
        {
            // If: I create a session object with a query that hasn't finished execution
            // Then: It should throw an exception
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            q.HasExecuted = false;
            Assert.Throws<InvalidOperationException>(() => EditSession.ValidateQueryForSession(q));
        }

        [Test]
        public void SessionValidateIncorrectResultSet()
        {
            // Setup: Create a query that yields >1 result sets
            TestResultSet[] results =
            {
                QueryExecution.Common.StandardTestResultSet,
                QueryExecution.Common.StandardTestResultSet
            };

            // @TODO: Fix when the connection service is fixed
            ConnectionInfo ci = QueryExecution.Common.CreateConnectedConnectionInfo(results, false, false);
            ConnectionService.Instance.OwnerToConnectionMap[ci.OwnerUri] = ci;

            var fsf = MemoryFileSystem.GetFileStreamFactory();
            Query query = new Query(Constants.StandardQuery, ci, new QueryExecutionSettings(), fsf);
            query.Execute();
            query.ExecutionTask.Wait();

            // If: I create a session object with a query that has !=1 result sets
            // Then: It should throw an exception
            Assert.Throws<InvalidOperationException>(() => EditSession.ValidateQueryForSession(query));
        }

        [Test]
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

        [Test]
        public void CreateRowNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object);

            // If: I ask to create a row without initializing
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.CreateRow());
        }

        [Test]
        public async Task CreateRowAddFailure()
        {
            // NOTE: This scenario should theoretically never occur, but is tested for completeness
            // Setup:
            // ... Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            EditTableMetadata etm = Common.GetCustomEditTableMetadata(rs.Columns.Cast<DbColumn>().ToArray());
            EditSession s = await Common.GetCustomSession(q, etm);

            // ... Add a mock edit to the edit cache to cause the .TryAdd to fail
            var mockEdit = new Mock<RowEditBase>().Object;
            s.EditCache[rs.RowCount] = mockEdit;

            // If: I create a row in the session
            // Then:
            // ... An exception should be thrown
            Assert.Throws<InvalidOperationException>(() => s.CreateRow());

            // ... The mock edit should still exist
            Assert.AreEqual(mockEdit, s.EditCache[rs.RowCount]);

            // ... The next row ID should not have changes
            Assert.AreEqual(rs.RowCount, s.NextRowId);
        }

        [Test]
        public async Task CreateRowSuccess()
        {
            // Setup: Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            EditTableMetadata etm = Common.GetCustomEditTableMetadata(rs.Columns.Cast<DbColumn>().ToArray());
            EditSession s = await Common.GetCustomSession(q, etm);

            // If: I add a row to the session
            EditCreateRowResult result = s.CreateRow();

            // Then:
            // ... The new ID should be equal to the row count
            Assert.AreEqual(rs.RowCount, result.NewRowId);

            // ... The next row ID should have been incremented
            Assert.AreEqual(rs.RowCount + 1, s.NextRowId);

            // ... There should be a new row create object in the cache
            Assert.That(s.EditCache.Keys, Has.Member(result.NewRowId));
            Assert.That(s.EditCache[result.NewRowId], Is.InstanceOf<RowCreate>());

            // ... The default values should be returned (we will test this in depth below)
            Assert.That(result.DefaultValues, Is.Not.Empty);
        }

        [Test]
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
                    EscapedName = cols[0].ColumnName
                },
                new EditColumnMetadata                   // Has default
                {
                    DefaultValue = "default",
                    EscapedName = cols[1].ColumnName
                },
                new EditColumnMetadata
                {
                    EscapedName = cols[2].ColumnName
                }
            };
            var etm = new EditTableMetadata
            {
                Columns = metas,
                EscapedMultipartName = "tbl",
                IsMemoryOptimized = false
            };
            etm.Extend(cols);

            // ... Create a result set
            var q = await Common.GetQuery(cols.Cast<DbColumn>().ToArray(), false);

            // ... Create a session from all this
            EditSession s = await Common.GetCustomSession(q, etm);

            // If: I add a row to the session, on a table that has defaults
            var result = s.CreateRow();

            // Then:
            // ... Result should not be null, new row ID should be > 0
            Assert.NotNull(result);
            Assert.True(result.NewRowId > 0);

            // ... There should be 3 default values (3 columns)
            Assert.That(result.DefaultValues, Is.Not.Empty);
            Assert.AreEqual(3, result.DefaultValues.Length);

            // ... There should be specific values for each kind of default
            Assert.Null(result.DefaultValues[0]);
            Assert.AreEqual("default", result.DefaultValues[1]);
        }

        #endregion

        [Test]
        [TestCaseSource(nameof(RowIdOutOfRangeData))]
        public async Task RowIdOutOfRange(long rowId, Action<EditSession, long> testAction)
        {
            // Setup: Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

            // If: I delete a row that is out of range for the result set
            // Then: I should get an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => testAction(s, rowId));
        }

        public static IEnumerable<object[]> RowIdOutOfRangeData
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
                yield return new object[] {-1L, revertCellAction};
                yield return new object[] {0L, revertCellAction};    // This is invalid b/c there isn't an edit pending for this row
                yield return new object[] {(long) QueryExecution.Common.StandardRows, revertCellAction};
                yield return new object[] {100L, revertCellAction};
            }
        }

        #region Initialize Tests

        [Test]
        public void InitializeAlreadyInitialized()
        {
            // Setup:
            // ... Create a session and fake that it has been initialized
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object) {IsInitialized = true};

            // If: I initialize it
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.Initialize(null, null, null, null, null));
        }

        [Test]
        public void InitializeAlreadyInitializing()
        {
            // Setup:
            // ... Create a session and fake that it is in progress of initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object) {InitializeTask = new Task(() => {})};

            // If: I initialize it
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.Initialize(null, null, null, null, null));
        }

        [Test]
        [TestCaseSource(nameof(InitializeNullParamsData))]
        public void InitializeNullParams(EditInitializeParams initParams, EditSession.Connector c,
            EditSession.QueryRunner qr, Func<Task> sh, Func<Exception, Task> fh)
        {
            // Setup:
            // ... Create a session that hasn't been initialized
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object);

            Assert.Catch<ArgumentException>(() => s.Initialize(initParams, c, qr, sh, fh), "I initialize it with a missing parameter. It should throw an exception");
        }

        public static IEnumerable<object[]> InitializeNullParamsData
        {
            get
            {
                yield return new object[] {Common.BasicInitializeParameters, null, DoNothingQueryRunner, DoNothingSuccessHandler, DoNothingFailureHandler};
                yield return new object[] {Common.BasicInitializeParameters, DoNothingConnector, null, DoNothingSuccessHandler, DoNothingFailureHandler};
                yield return new object[] {Common.BasicInitializeParameters, DoNothingConnector, DoNothingQueryRunner, null, DoNothingFailureHandler};
                yield return new object[] {Common.BasicInitializeParameters, DoNothingConnector, DoNothingQueryRunner, DoNothingSuccessHandler, null};

                string[] nullOrWhitespace = {null, string.Empty, " \t\r\n"};

                // Tests with invalid object name or type
                foreach (string str in nullOrWhitespace)
                {
                    // Invalid object name
                    var eip1 = new EditInitializeParams
                    {
                        ObjectName = str,
                        ObjectType = "table",
                        Filters = new EditInitializeFiltering()
                    };
                    yield return new object[] {eip1, DoNothingConnector, DoNothingQueryRunner, DoNothingSuccessHandler, DoNothingFailureHandler};

                    // Invalid object type
                    var eip2 = new EditInitializeParams
                    {
                        ObjectName = "tbl",
                        ObjectType = str,
                        Filters = new EditInitializeFiltering()
                    };
                    yield return new object[] {eip2, DoNothingConnector, DoNothingQueryRunner, DoNothingSuccessHandler, DoNothingFailureHandler};
                }

                // Test with null init filters
                yield return new object[]
                {
                    new EditInitializeParams
                    {
                        ObjectName = "tbl",
                        ObjectType = "table",
                        Filters = null
                    },
                    DoNothingConnector,
                    DoNothingQueryRunner,
                    DoNothingSuccessHandler,
                    DoNothingFailureHandler
                };
            }
        }

        [Test]
        public async Task InitializeMetadataFails()
        {
            // Setup:
            // ... Create a metadata factory that throws
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            emf.Setup(f => f.GetObjectMetadata(It.IsAny<DbConnection>(), It.IsAny<string[]>(), It.IsAny<string>()))
                .Throws<Exception>();

            // ... Create a session that hasn't been initialized
            EditSession s = new EditSession(emf.Object);

            // ... Create a mock for verifying the failure handler will be called
            var successHandler = DoNothingSuccessMock;
            var failureHandler = DoNothingFailureMock;

            // If: I initalize the session with a metadata factory that will fail
            s.Initialize(Common.BasicInitializeParameters, DoNothingConnector, DoNothingQueryRunner, successHandler.Object, failureHandler.Object);
            await s.InitializeTask;

            // Then:
            // ... The session should not be initialized
            Assert.False(s.IsInitialized);

            // ... The failure handler should have been called once
            failureHandler.Verify(f => f(It.IsAny<Exception>()), Times.Once);

            // ... The success handler should not have been called at all
            successHandler.Verify(f => f(), Times.Never);
        }

        [Test]
        public async Task InitializeQueryFailException()
        {
            // Setup:
            // ... Create a metadata factory that will return some generic column information
            var b = QueryExecution.Common.GetBasicExecutedBatch();
            var etm = Common.GetCustomEditTableMetadata(b.ResultSets[0].Columns.Cast<DbColumn>().ToArray());
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            emf.Setup(f => f.GetObjectMetadata(It.IsAny<DbConnection>(), It.IsAny<string[]>(), It.IsAny<string>()))
                .Returns(etm);

            // ... Create a session that hasn't been initialized
            EditSession s = new EditSession(emf.Object);

            // ... Create a query runner that will fail via exception
            Mock<EditSession.QueryRunner> qr = new Mock<EditSession.QueryRunner>();
            qr.Setup(r => r(It.IsAny<string>())).Throws(new Exception("qqq"));

            // ... Create a mock for verifying the failure handler will be called
            var successHandler = DoNothingSuccessMock;
            var failureHandler = DoNothingFailureMock;

            // If: I initialize the session with a query runner that will fail
            s.Initialize(Common.BasicInitializeParameters, DoNothingConnector, qr.Object, successHandler.Object, failureHandler.Object);
            await s.InitializeTask;

            // Then:
            // ... The session should not be initialized
            Assert.False(s.IsInitialized);

            // ... The failure handler should have been called once
            failureHandler.Verify(f => f(It.IsAny<Exception>()), Times.Once);

            // ... The success handler should not have been called at all
            successHandler.Verify(f => f(), Times.Never);
        }

        [Test]
        public async Task InitializeQueryFailReturnNull([Values(null, "It fail.")] string message)
        {
            // Setup:
            // ... Create a metadata factory that will return some generic column information
            var b = QueryExecution.Common.GetBasicExecutedBatch();
            var etm = Common.GetCustomEditTableMetadata(b.ResultSets[0].Columns.Cast<DbColumn>().ToArray());
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            emf.Setup(f => f.GetObjectMetadata(It.IsAny<DbConnection>(), It.IsAny<string[]>(), It.IsAny<string>()))
                .Returns(etm);

            // ... Create a session that hasn't been initialized
            EditSession s = new EditSession(emf.Object);

            // ... Create a query runner that will fail via returning a null query
            Mock<EditSession.QueryRunner> qr = new Mock<EditSession.QueryRunner>();
            qr.Setup(r => r(It.IsAny<string>()))
                .Returns(Task.FromResult(new EditSession.EditSessionQueryExecutionState(null, message)));

            // ... Create a mock for verifying the failure handler will be called
            var successHandler = DoNothingSuccessMock;
            var failureHandler = DoNothingFailureMock;

            // If: I initialize the session with a query runner that will fail
            s.Initialize(Common.BasicInitializeParameters, DoNothingConnector, qr.Object, successHandler.Object, failureHandler.Object);
            await s.InitializeTask;

            // Then:
            // ... The session should not be initialized
            Assert.False(s.IsInitialized);

            // ... The failure handler should have been called once
            failureHandler.Verify(f => f(It.IsAny<Exception>()), Times.Once);

            // ... The success handler should not have been called at all
            successHandler.Verify(f => f(), Times.Never);
        }

        [Test]
        public async Task InitializeSuccess()
        {
            // Setup:
            // ... Create a metadata factory that will return some generic column information
            var q = QueryExecution.Common.GetBasicExecutedQuery();
            var rs = q.Batches[0].ResultSets[0];
            var etm = Common.GetCustomEditTableMetadata(rs.Columns.Cast<DbColumn>().ToArray());
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            emf.Setup(f => f.GetObjectMetadata(It.IsAny<DbConnection>(), It.IsAny<string[]>(), It.IsAny<string>()))
                .Returns(etm);

            // ... Create a session that hasn't been initialized
            EditSession s = new EditSession(emf.Object);

            // ... Create a query runner that will return a successful query
            Mock<EditSession.QueryRunner> qr = new Mock<EditSession.QueryRunner>();
            qr.Setup(r => r(It.IsAny<string>()))
                .Returns(Task.FromResult(new EditSession.EditSessionQueryExecutionState(q)));

            // ... Create a mock for verifying the failure handler will be called
            var successHandler = DoNothingSuccessMock;
            var failureHandler = DoNothingFailureMock;

            // If: I initialize the session with a query runner that will fail
            s.Initialize(Common.BasicInitializeParameters, DoNothingConnector, qr.Object, successHandler.Object, failureHandler.Object);
            await s.InitializeTask;

            // Then:
            // ... The failure handler should not have been called
            failureHandler.Verify(f => f(It.IsAny<Exception>()), Times.Never);

            // ... The success handler should have been called
            successHandler.Verify(f => f(), Times.Once);

            // ... The session should have been initialized
            Assert.True(s.IsInitialized);
            Assert.AreEqual(rs.RowCount, s.NextRowId);
            Assert.NotNull(s.EditCache);
            Assert.That(s.EditCache, Is.Empty);
        }

        #endregion

        #region Delete Row Tests

        [Test]
        public void DeleteRowNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object);

            // If: I ask to delete a row without initializing
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.DeleteRow(0));
        }

        [Test]
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
            Assert.AreEqual(mockEdit, s.EditCache[0]);
        }

        [Test]
        public async Task DeleteRowSuccess()
        {
            // Setup: Create a session with a proper query and metadata
            var s = await GetBasicSession();

            // If: I add a row to the session
            s.DeleteRow(0);

            Assert.That(s.EditCache.Keys, Has.Member(0));
            Assert.That(s.EditCache[0], Is.InstanceOf<RowDelete>(), "There should be a new row delete object in the cache");
        }

        #endregion

        #region Revert Row Tests

        [Test]
        public void RevertRowNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object);

            // If: I ask to revert a row without initializing
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.RevertRow(0));
        }

        [Test]
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

            Assert.That(s.EditCache.Keys, Has.No.Zero, "The edit cache should not contain a pending edit for the row");
        }

        #endregion

        #region Revert Cell Tests

        [Test]
        public void RevertCellNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object);

            // If: I ask to revert a cell without initializing
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.RevertCell(0, 0));
        }

        [Test]
        public async Task RevertCellRowRevert()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

            // ... Add a edit that we will say the edit was reverted if we update a cell
            var mockEdit = new Mock<RowEditBase>();
            mockEdit.Setup(e => e.RevertCell(It.IsAny<int>()))
                .Returns(new EditRevertCellResult {IsRowDirty = false});
            s.EditCache[0] = mockEdit.Object;

            // If: I update a cell that will return an implicit revert
            s.RevertCell(0, 0);

            // Then:
            // ... Set cell should have been called on the mock update once
            mockEdit.Verify(e => e.RevertCell(0), Times.Once);

            // ... The mock update should no longer be in the edit cache
            Assert.That(s.EditCache, Is.Empty);
        }

        #endregion

        #region Update Cell Tests

        [Test]
        public void UpdateCellNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object);

            // If: I ask to update a cell without initializing
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.UpdateCell(0, 0, ""));
        }

        [Test]
        public async Task UpdateCellExisting()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

            // ... Add a mock edit to the edit cache to cause the .TryAdd to fail
            var mockEdit = new Mock<RowEditBase>();
            mockEdit.Setup(e => e.SetCell(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(new EditUpdateCellResult {IsRowDirty = true});
            s.EditCache[0] = mockEdit.Object;

            // If: I update a cell on a row that already has a pending edit
            s.UpdateCell(0, 0, null);

            // Then:
            // ... The mock update should still be in the cache
            // ... And it should have had set cell called on it
            Assert.That(s.EditCache.Values, Has.Member(mockEdit.Object));
        }

        [Test]
        public async Task UpdateCellNew()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

            // If: I update a cell on a row that does not have a pending edit
            s.UpdateCell(0, 0, "");

            // Then:
            Assert.Multiple(() =>
            {
                Assert.That(s.EditCache.Keys, Has.Member(0));
                Assert.That(s.EditCache[0], Is.InstanceOf<RowUpdate>(), "A new update row edit should have been added to the cache");
            });
        }

        [Test]
        public async Task UpdateCellRowRevert()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

            // ... Add a edit that we will say the edit was reverted if we update a cell
            var mockEdit = new Mock<RowEditBase>();
            mockEdit.Setup(e => e.SetCell(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(new EditUpdateCellResult {IsRowDirty = false});
            s.EditCache[0] = mockEdit.Object;

            // If: I update a cell that will return an implicit revert
            s.UpdateCell(0, 0, null);

            // Then:
            // ... Set cell should have been called on the mock update once
            mockEdit.Verify(e => e.SetCell(0, null), Times.Once);

            // ... The mock update should no longer be in the edit cache
            Assert.That(s.EditCache, Is.Empty);
        }



        #endregion

        #region SubSet Tests

        [Test]
        public async Task SubsetNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object);

            // If: I ask to update a cell without initializing
            // Then: I should get an exception
            Assert.ThrowsAsync<InvalidOperationException>(() => s.GetRows(0, 100));
        }

        [Test]
        public async Task GetRowsNoEdits()
        {
            // Setup: Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            EditTableMetadata etm = Common.GetCustomEditTableMetadata(rs.Columns.Cast<DbColumn>().ToArray());
            EditSession s = await Common.GetCustomSession(q, etm);

            // If: I ask for 3 rows from session skipping the first
            EditRow[] rows = await s.GetRows(1, 3);

            // Then:
            // ... I should get back 3 rows
            Assert.AreEqual(3, rows.Length);

            // ... Each row should...
            for (int i = 0; i < rows.Length; i++)
            {
                EditRow er = rows[i];

                // ... Have properly set IDs
                Assert.AreEqual(i + 1, er.Id);

                // ... Have cells equal to the cells in the result set
                DbCellValue[] cachedRow = rs.GetRow(i + 1).ToArray();
                Assert.AreEqual(cachedRow.Length, er.Cells.Length);
                for (int j = 0; j < cachedRow.Length; j++)
                {
                    Assert.AreEqual(cachedRow[j].DisplayValue, er.Cells[j].DisplayValue);
                    Assert.AreEqual(cachedRow[j].IsNull, er.Cells[j].IsNull);
                    Assert.False(er.Cells[j].IsDirty);
                }

                // ... Be clean, since we didn't apply any updates
                Assert.AreEqual(EditRow.EditRowState.Clean, er.State);
                Assert.False(er.IsDirty);
            }
        }

        [Test]
        public async Task GetRowsPendingUpdate()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

            // ... Add a cell update to it
            s.UpdateCell(1, 0, "foo");

            // If: I ask for 3 rows from the session, skipping the first, including the updated one
            EditRow[] rows = await s.GetRows(1, 3);

            // Then:
            // ... I should get back 3 rows
            Assert.AreEqual(3, rows.Length);

            // ... The first row should reflect that there is an update pending
            //     (More in depth testing is done in the RowUpdate class tests)
            var updatedRow = rows[0];
            Assert.AreEqual(EditRow.EditRowState.DirtyUpdate, updatedRow.State);
            Assert.AreEqual("foo", updatedRow.Cells[0].DisplayValue);

            // ... The other rows should be clean
            for (int i = 1; i < rows.Length; i++)
            {
                Assert.AreEqual(EditRow.EditRowState.Clean, rows[i].State);
            }
        }

        [Test]
        public async Task GetRowsPendingDeletion()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

            // ... Add a row deletion
            s.DeleteRow(1);

            // If: I ask for 3 rows from the session, skipping the first, including the updated one
            EditRow[] rows = await s.GetRows(1, 3);

            // Then:
            // ... I should get back 3 rows
            Assert.AreEqual(3, rows.Length);

            // ... The first row should reflect that there is an update pending
            //     (More in depth testing is done in the RowUpdate class tests)
            var updatedRow = rows[0];
            Assert.AreEqual(EditRow.EditRowState.DirtyDelete, updatedRow.State);
            Assert.That(updatedRow.Cells[0].DisplayValue, Is.Not.Empty);

            // ... The other rows should be clean
            for (int i = 1; i < rows.Length; i++)
            {
                Assert.AreEqual(EditRow.EditRowState.Clean, rows[i].State);
            }
        }

        [Test]
        public async Task GetRowsPendingInsertion()
        {
            // Setup:
            // ... Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

            // ... Add a row creation
            s.CreateRow();

            // If: I ask for the rows including the new rows
            EditRow[] rows = await s.GetRows(0, 6);

            // Then:
            // ... I should get back 6 rows
            Assert.AreEqual(6, rows.Length);

            // ... The last row should reflect that there's a new row
            var updatedRow = rows[5];
            Assert.AreEqual(EditRow.EditRowState.DirtyInsert, updatedRow.State);

            // ... The other rows should be clean
            for (int i = 0; i < rows.Length - 1; i++)
            {
                Assert.AreEqual(EditRow.EditRowState.Clean, rows[i].State);
            }
        }

        [Test]
        public async Task GetRowsAllNew()
        {
            // Setup:
            // ... Create a session with a query and metadata
            EditSession s = await GetBasicSession();

            // ... Add a few row creations
            s.CreateRow();
            s.CreateRow();
            s.CreateRow();

            // If: I ask for the rows included the new rows
            EditRow[] rows = await s.GetRows(5, 5);

            // Then:
            // ... I should get back 3 rows back
            Assert.AreEqual(3, rows.Length);

            Assert.That(rows.Select(r => r.State), Has.All.EqualTo(EditRow.EditRowState.DirtyInsert), "All the rows should be new");
        }

        #endregion

        #region Script Edits Tests

        [Test]
        public void ScriptEditsNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object);

            // If: I ask to script edits without initializing
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.ScriptEdits(string.Empty));
        }

        [Test]
        public async Task ScriptNullOrEmptyOutput([Values(null, "", " \t\r\n")] string outputPath)
        {
            // Setup: Create a session with a proper query and metadata
            EditSession s = await GetBasicSession();

            // If: I try to script the edit cache with a null or whitespace output path
            // Then: It should throw an exception
            Assert.Throws<ArgumentNullException>(() => s.ScriptEdits(outputPath));
        }

        [Test]
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
                Assert.AreEqual(file.FilePath, outputPath);

                // ... The written file should have two lines, one for each edit
                Assert.AreEqual(2, File.ReadAllLines(outputPath).Length);
            }
        }

        #endregion

        #region Commit Tests

        [Test]
        public void CommitEditsNotInitialized()
        {
            // Setup:
            // ... Create a session without initializing
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            EditSession s = new EditSession(emf.Object);

            // If: I ask to script edits without initializing
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => s.CommitEdits(null, null, null));
        }

        [Test]
        public async Task CommitNullConnection()
        {
            // Setup: Create a basic session
            EditSession s = await GetBasicSession();

            // If: I attempt to commit with a null connection
            // Then: I should get an exception
            Assert.Throws<ArgumentNullException>(
                () => s.CommitEdits(null, () => Task.CompletedTask, e => Task.CompletedTask));
        }

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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
            Assert.That(s.EditCache, Is.Empty);
        }

        [Test]
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
            Assert.That(s.EditCache, Is.Not.Empty);
        }

        #endregion

        #region Construct Initialize Query Tests

        [Test]
        public void ConstructQueryWithoutLimit()
        {
            // Setup: Create a metadata provider for some basic columns
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);

            // If: I generate a query for selecting rows without a limit
            EditInitializeFiltering eif = new EditInitializeFiltering
            {
                LimitResults = null
            };
            string query = EditSession.ConstructInitializeQuery(data.TableMetadata, eif);

            // Then:
            // ... The query should look like a select statement
            Regex selectRegex = new Regex("SELECT (.+) FROM (.+)", RegexOptions.IgnoreCase);
            var match = selectRegex.Match(query);
            Assert.True(match.Success);

            // ... There should be columns in it
            Assert.AreEqual(data.DbColumns.Length, match.Groups[1].Value.Split(',').Length);

            // ... The table name should be in it
            Assert.AreEqual(data.TableMetadata.EscapedMultipartName, match.Groups[2].Value);

            Assert.That(query, Does.Not.Contain("TOP"), "It should NOT have a TOP clause in it");
        }

        [Test]
        public void ConstructQueryNegativeLimit()
        {
            // Setup: Create a metadata provider for some basic columns
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);

            // If: I generate a query for selecting rows with a negative limit
            // Then: An exception should be thrown
            EditInitializeFiltering eif = new EditInitializeFiltering
            {
                LimitResults = -1
            };
            Assert.Throws<ArgumentOutOfRangeException>(() => EditSession.ConstructInitializeQuery(data.TableMetadata, eif));
        }

        [Test]
        public void ConstructQueryWithLimit([Values(0,10,1000)]int limit)
        {
            // Setup: Create a metadata provider for some basic columns
            var data = new Common.TestDbColumnsWithTableMetadata(false, false, 0, 0);

            // If: I generate a query for selecting rows without a limit
            EditInitializeFiltering eif = new EditInitializeFiltering
            {
                LimitResults = limit
            };
            string query = EditSession.ConstructInitializeQuery(data.TableMetadata, eif);

            // Then:
            // ... The query should look like a select statement
            Regex selectRegex = new Regex(@"SELECT TOP (\d+) (.+) FROM (.+)", RegexOptions.IgnoreCase);
            var match = selectRegex.Match(query);
            Assert.True(match.Success);

            // ... There should be columns in it
            Assert.AreEqual(data.DbColumns.Length, match.Groups[2].Value.Split(',').Length);

            // ... The table name should be in it
            Assert.AreEqual(data.TableMetadata.EscapedMultipartName, match.Groups[3].Value);

            // ... The top count should be equal to what we provided
            int limitFromQuery;
            Assert.True(int.TryParse(match.Groups[1].Value, out limitFromQuery));
            Assert.AreEqual(limit, limitFromQuery);
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
            EditTableMetadata etm = Common.GetCustomEditTableMetadata(rs.Columns.Cast<DbColumn>().ToArray());
            return await Common.GetCustomSession(q, etm);
        }
    }
}
