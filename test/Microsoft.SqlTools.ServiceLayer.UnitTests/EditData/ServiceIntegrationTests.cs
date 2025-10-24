﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    public class ServiceIntegrationTests
    {
        #region EditSession Operation Helper Tests

        [Test]
        public async Task NullOrMissingSessionId([Values(null, "", " \t\n\r", "Does not exist")] string sessionId)
        {
            // Setup: 
            // ... Create a edit data service
            var eds = new EditDataService(null, null, null);

            // ... Create a session params that returns the provided session ID
            var mockParams = new EditCreateRowParams {OwnerUri = sessionId};
            
            // ... Create a context mock that will capture the error
            string errorMessage = null;
            var contextMock = RequestContextMocks.Create<EditDisposeResult>(null)
                .AddErrorHandling((msg, code, data) => errorMessage = msg);

            // If: I ask to perform an action that requires a session
            // Then: An error should have been sent
            await eds.HandleSessionRequest(mockParams, contextMock.Object, session => null);
            Assert.That(errorMessage, Is.Not.Null, "An error message should have been sent");
        }

        [Test]
        public async Task OperationThrows()
        {
            // Setup: 
            // ... Create an edit data service with a session
            var eds = new EditDataService(null, null, null);
            eds.ActiveSessions[Common.OwnerUri] = await GetDefaultSession();

            // ... Create a session param that returns the common owner uri
            var mockParams = new EditCreateRowParams { OwnerUri = Common.OwnerUri };
            
            // ... Create a context mock that will capture the error
            string errorMessage = null;
            var contextMock = RequestContextMocks.Create<EditDisposeResult>(null)
                .AddErrorHandling((msg, code, data) => errorMessage = msg);

            // If: I ask to perform an action that throws an exception
            // Then: An error should have been sent
            await eds.HandleSessionRequest(mockParams, contextMock.Object, s => { throw new Exception(); });
            Assert.That(errorMessage, Is.Not.Null, "An error message should have been sent");
        }



        #endregion

        #region Dispose Tests

        [Test]
        public void DisposeNullOrMissingSessionId([Values(null, "", " \t\n\r", "Does not exist")]  string sessionId)
        {
            // Setup: Create a edit data service
            var eds = new EditDataService(null, null, null);

            // If: I ask to perform an action that requires a session
            // Then: I should get an error from it
            var contextMock = RequestContextMocks.Create<EditDisposeResult>(null);
            Assert.That(() => eds.HandleDisposeRequest(new EditDisposeParams { OwnerUri = sessionId }, contextMock.Object).Wait(), Throws.Exception);
        }

        [Test]
        public async Task DisposeSuccess()
        {
            // Setup: Create an edit data service with a session
            var eds = new EditDataService(null, null, null);
            eds.ActiveSessions[Common.OwnerUri] = await GetDefaultSession();
            
            // If: I ask to dispose of an existing session
            var efv = new EventFlowValidator<EditDisposeResult>()
                .AddResultValidation(Assert.NotNull)
                .Complete();
            await eds.HandleDisposeRequest(new EditDisposeParams {OwnerUri = Common.OwnerUri}, efv.Object);

            // Then:
            // ... It should have completed successfully
            efv.Validate();

            Assert.That(eds.ActiveSessions, Is.Empty, "And the session should have been removed from the active session list");
        }

        #endregion

        [Test]
        public async Task DeleteSuccess()
        {
            // Setup: Create an edit data service with a session
            var eds = new EditDataService(null, null, null);
            eds.ActiveSessions[Constants.OwnerUri] = await GetDefaultSession();

            // If: I validly ask to delete a row
            var efv = new EventFlowValidator<EditDeleteRowResult>()
                .AddResultValidation(Assert.NotNull)
                .Complete();
            await eds.HandleDeleteRowRequest(new EditDeleteRowParams {OwnerUri = Constants.OwnerUri, RowId = 0}, efv.Object);

            // Then:
            // ... It should be successful
            efv.Validate();

            // ... There should be a delete in the session
            EditSession s = eds.ActiveSessions[Constants.OwnerUri];
            Assert.That(s.EditCache.Any(e => e.Value is RowDelete), Is.True);
        }

        [Test]
        public async Task CreateSucceeds()
        {
            // Setup: Create an edit data service with a session
            var eds = new EditDataService(null, null, null);
            eds.ActiveSessions[Constants.OwnerUri] = await GetDefaultSession();

            // If: I ask to create a row from a non existant session
            var efv = new EventFlowValidator<EditCreateRowResult>()
                .AddResultValidation(ecrr => { Assert.That(ecrr.NewRowId, Is.GreaterThan(0)); })
                .Complete();
            await eds.HandleCreateRowRequest(new EditCreateRowParams { OwnerUri = Constants.OwnerUri }, efv.Object);

            // Then:
            // ... It should have been successful
            efv.Validate();

            // ... There should be a create in the session
            EditSession s = eds.ActiveSessions[Constants.OwnerUri];
            Assert.That(s.EditCache.Any(e => e.Value is RowCreate), Is.True);
        }

        [Test]
        public async Task RevertCellSucceeds()
        {
            // Setup: 
            // ... Create an edit data service with a session that has a pending cell edit
            var eds = new EditDataService(null, null, null);
            var session = await GetDefaultSession();
            eds.ActiveSessions[Constants.OwnerUri] = session;
            
            // ... Make sure that the edit has revert capabilities
            var mockEdit = new Mock<RowEditBase>();
            mockEdit.Setup(edit => edit.RevertCell(It.IsAny<int>()))
                .Returns(new EditRevertCellResult());
            session.EditCache[0] = mockEdit.Object;


            // If: I ask to revert a cell that has a pending edit
            var efv = new EventFlowValidator<EditRevertCellResult>()
                .AddResultValidation(Assert.NotNull)
                .Complete();
            var param = new EditRevertCellParams
            {
                OwnerUri = Constants.OwnerUri,
                RowId = 0
            };
            await eds.HandleRevertCellRequest(param, efv.Object);
            
            // Then:
            // ... It should have succeeded
            efv.Validate();
            
            // ... The edit cache should be empty again
            EditSession s = eds.ActiveSessions[Constants.OwnerUri];
            Assert.That(s.EditCache, Is.Empty);
        }
        
        [Test]
        public async Task RevertRowSucceeds()
        {
            // Setup: Create an edit data service with a session that has an pending edit
            var eds = new EditDataService(null, null, null);
            var session = await GetDefaultSession();
            session.EditCache[0] = new Mock<RowEditBase>().Object;
            eds.ActiveSessions[Constants.OwnerUri] = session;

            // If: I ask to revert a row that has a pending edit
            var efv = new EventFlowValidator<EditRevertRowResult>()
                .AddResultValidation(result =>
                {
                    Assert.That(result, Is.Not.Null);
                    Assert.That(result.Row, Is.Not.Null, "The result should contain the reverted row");
                    Assert.That(result.Row.Id, Is.EqualTo(0), "The reverted row should have the correct ID");
                })
                .Complete();
            await eds.HandleRevertRowRequest(new EditRevertRowParams { OwnerUri = Constants.OwnerUri, RowId = 0}, efv.Object);

            // Then:
            // ... It should have succeeded
            efv.Validate();

            // ... The edit cache should be empty again
            EditSession s = eds.ActiveSessions[Constants.OwnerUri];
            Assert.That(s.EditCache, Is.Empty);
        }

        [Test]
        public async Task UpdateSuccess()
        {
            // Setup: Create an edit data service with a session
            var eds = new EditDataService(null, null, null);
            var session = await GetDefaultSession();
            eds.ActiveSessions[Constants.OwnerUri] = session;
            var edit = new Mock<RowEditBase>();
            edit.Setup(e => e.SetCell(It.IsAny<int>(), It.IsAny<string>())).Returns(new EditUpdateCellResult
            {
                IsRowDirty = true,
                Cell = new EditCell(new DbCellValue(), true)
            });
            session.EditCache[0] = edit.Object;

            // If: I validly ask to update a cell
            var efv = new EventFlowValidator<EditUpdateCellResult>()
                .AddResultValidation(eucr =>
                {
                    Assert.That(eucr, Is.Not.Null);
                    Assert.That(eucr.Cell, Is.Not.Null);
                    Assert.That(eucr.IsRowDirty, Is.True);
                })
                .Complete();
            await eds.HandleUpdateCellRequest(new EditUpdateCellParams { OwnerUri = Constants.OwnerUri, RowId = 0}, efv.Object);

            // Then:
            // ... It should be successful
            efv.Validate();

            // ... Set cell should have been called once
            edit.Verify(e => e.SetCell(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task GetRowsSuccess()
        {
            // Setup: Create an edit data service with a session
            // Setup: Create an edit data service with a session
            var eds = new EditDataService(null, null, null);
            var session = await GetDefaultSession();
            eds.ActiveSessions[Constants.OwnerUri] = session;

            // If: I validly ask for rows
            var efv = new EventFlowValidator<EditSubsetResult>()
                .AddResultValidation(esr =>
                {
                    Assert.That(esr, Is.Not.Null);
                    Assert.That(esr.Subset, Is.Not.Empty);
                    Assert.That(esr.RowCount, Is.Not.EqualTo(0));
                })
                .Complete();
            await eds.HandleSubsetRequest(new EditSubsetParams
            {
                OwnerUri = Constants.OwnerUri,
                RowCount = 10,
                RowStartIndex = 0
            }, efv.Object);

            // Then:
            // ... It should be successful
            efv.Validate();
        }

        #region Initialize Tests
        [Test]
        [Sequential]
        public void InitializeNullParams([Values(null, Common.OwnerUri, Common.OwnerUri)] string ownerUri, 
                                               [Values("table", null, "table")] string objName, 
                                               [Values("table", "table", null)] string objType)
        {
            // Setup: Create an edit data service without a session
            var eds = new EditDataService(null, null, null);

            // If:
            // ... I have init params with a null parameter
            var initParams = new EditInitializeParams
            {
                ObjectName = objName,
                OwnerUri = ownerUri,
                ObjectType = objType
            };
            var contextMock = RequestContextMocks.Create<EditInitializeResult>(null);
            // ... And I initialize an edit session with that
            // Then:
            // ... An error event should have been sent
            Assert.That(() => eds.HandleInitializeRequest(initParams, contextMock.Object), Throws.ArgumentException);

            // ... There should not be a session
            Assert.That(eds.ActiveSessions, Is.Empty);
        }

        [Test]
        public async Task InitializeSessionExists()
        {
            // Setup: Create an edit data service with a session already defined
            var eds = new EditDataService(null, null, null);
            var session = await GetDefaultSession();
            eds.ActiveSessions[Constants.OwnerUri] = session;
            
            // If: I request to init a session for an owner URI that already exists
            var initParams = new EditInitializeParams
            {
                ObjectName = "testTable",
                OwnerUri = Constants.OwnerUri,
                ObjectType = "Table",
                Filters = new EditInitializeFiltering()
            };
            var contextMock = RequestContextMocks.Create<EditInitializeResult>(null);

            // Then:
            // ... An error event should have been sent
            Assert.That(() => eds.HandleInitializeRequest(initParams, contextMock.Object), Throws.ArgumentNullException);

            // ... The original session should still be there
            Assert.That(eds.ActiveSessions.Count, Is.EqualTo(1));
            Assert.That(eds.ActiveSessions[Constants.OwnerUri], Is.EqualTo(session));
        }

        // Disable flaky test for investigation (karlb - 3/13/2018)
        //[Test]
        public async Task InitializeSessionSuccess()
        {
            // Setup: 
            // .. Create a mock query
            var mockQueryResults = QueryExecution.Common.StandardTestDataSet;
            var cols = mockQueryResults[0].Columns;
            
            // ... Create a metadata factory that will return some generic column information
            var etm = Common.GetCustomEditTableMetadata(cols.ToArray());
            Mock<IEditMetadataFactory> emf = new Mock<IEditMetadataFactory>();
            emf.Setup(f => f.GetObjectMetadata(It.IsAny<DbConnection>(), It.IsAny<string[]>(), It.IsAny<string>()))
                .Returns(etm);
            
            // ... Create a query execution service that will return a successful query
            var qes = QueryExecution.Common.GetPrimedExecutionService(mockQueryResults, true, false, false, null);
            
            // ... Create a connection service that doesn't throw when asked for a connection
            var cs = new Mock<ConnectionService>();
            cs.Setup(s => s.GetOrOpenConnection(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.FromResult<DbConnection>(null));
            
            // ... Create an edit data service that has mock providers
            var eds = new EditDataService(qes, cs.Object, emf.Object);

            // If: I request to initialize an edit data session
            var initParams = new EditInitializeParams
            {
                ObjectName = "testTable",
                OwnerUri = Constants.OwnerUri,
                ObjectType = "Table",
                Filters = new EditInitializeFiltering()
            };
            var efv = new EventFlowValidator<EditInitializeResult>()
                .AddResultValidation(Assert.NotNull)
                .AddEventValidation(BatchStartEvent.Type, Assert.NotNull)
                .AddEventValidation(ResultSetCompleteEvent.Type, Assert.NotNull)
                .AddEventValidation(MessageEvent.Type, Assert.NotNull)
                .AddEventValidation(BatchCompleteEvent.Type, Assert.NotNull)
                .AddEventValidation(QueryCompleteEvent.Type, Assert.NotNull)
                .AddEventValidation(EditSessionReadyEvent.Type, esrp =>
                {
                    Assert.That(esrp, Is.Not.Null);
                    Assert.That(esrp.OwnerUri, Is.EqualTo(Constants.OwnerUri));
                    Assert.That(esrp.Success, Is.True);
                    Assert.That(esrp.Message, Is.Null);
                })
                .Complete();
            await eds.HandleInitializeRequest(initParams, efv.Object);
            await eds.ActiveSessions[Constants.OwnerUri].InitializeTask;

            // Then:
            // ... The event should have been received successfully
            efv.Validate();

            // ... The session should have been created
            Assert.That(eds.ActiveSessions.Count, Is.EqualTo(1));
            Assert.That(eds.ActiveSessions.Keys.Contains(Constants.OwnerUri), Is.True);
        }

        #endregion
        private static readonly object[] schemaNameParameters =
        {
            new object[] {"table", "myschema", new[] { "myschema", "table" } },                 // Use schema
            new object[] {"table", null, new[] { "table" } },                                   // skip schema
            new object[] {"schema.table", "myschema", new[] { "myschema", "schema.table" } },    // Use schema
            new object[] {"schema.table", null, new[] { "schema", "table" } },                   // Split object name into schema
        };

        [Test, TestCaseSource(nameof(schemaNameParameters))]        
        public void ShouldUseSchemaNameIfDefined(string objName, string schemaName, string[] expectedNameParts)
        {
            // Setup: Create an edit data service without a session
            var eds = new EditDataService(null, null, null);

            // If:
            // ... I have init params with an object and schema parameter
            var initParams = new EditInitializeParams
            {
                ObjectName = objName,
                SchemaName = schemaName,
                OwnerUri = Common.OwnerUri,
                ObjectType = "table"
            };

            // ... And I get named parts for that
            string[] nameParts = EditSession.GetEditTargetName(initParams);

            // Then:
            Assert.That(nameParts, Is.EqualTo(expectedNameParts));
        }

        private static async Task<EditSession> GetDefaultSession()
        {
            // ... Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            EditTableMetadata etm = Common.GetCustomEditTableMetadata(rs.Columns.Cast<DbColumn>().ToArray());
            EditSession s = await Common.GetCustomSession(q, etm);
            return s;
        }
    }
}
