//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    public class ServiceIntegrationTests
    {
        #region EditSession Operation Helper Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\n\r")]
        [InlineData("Does not exist")]
        public async Task NullOrMissingSessionId(string sessionId)
        {
            // Setup: 
            // ... Create a edit data service
            var eds = new EditDataService(null, null, null);

            // ... Create a session params that returns the provided session ID
            var mockParams = new EditCreateRowParams {OwnerUri = sessionId};

            // If: I ask to perform an action that requires a session
            // Then: I should get an error from it
            var efv = new EventFlowValidator<EditDisposeResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleSessionRequest(mockParams, efv.Object, session => null);
            efv.Validate();
        }

        [Fact]
        public async Task OperationThrows()
        {
            // Setup: 
            // ... Create an edit data service with a session
            var eds = new EditDataService(null, null, null);
            eds.ActiveSessions[Common.OwnerUri] = await GetDefaultSession();

            // ... Create a session param that returns the common owner uri
            var mockParams = new EditCreateRowParams { OwnerUri = Common.OwnerUri };

            // If: I ask to perform an action that requires a session
            // Then: I should get an error from it
            var efv = new EventFlowValidator<EditDisposeResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleSessionRequest(mockParams, efv.Object, s => { throw new Exception(); });
            efv.Validate();
        }



        #endregion

        #region Dispose Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\n\r")]
        [InlineData("Does not exist")]
        public async Task DisposeNullOrMissingSessionId(string sessionId)
        {
            // Setup: Create a edit data service
            var eds = new EditDataService(null, null, null);

            // If: I ask to perform an action that requires a session
            // Then: I should get an error from it
            var efv = new EventFlowValidator<EditDisposeResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleDisposeRequest(new EditDisposeParams {OwnerUri = sessionId}, efv.Object);
            efv.Validate();
        }

        [Fact]
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

            // ... And the session should have been removed from the active session list
            Assert.Empty(eds.ActiveSessions);
        }

        #endregion

        [Fact]
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
            Assert.True(s.EditCache.Any(e => e.Value is RowDelete));
        }

        [Fact]
        public async Task CreateSucceeds()
        {
            // Setup: Create an edit data service with a session
            var eds = new EditDataService(null, null, null);
            eds.ActiveSessions[Constants.OwnerUri] = await GetDefaultSession();

            // If: I ask to create a row from a non existant session
            var efv = new EventFlowValidator<EditCreateRowResult>()
                .AddResultValidation(ecrr => { Assert.True(ecrr.NewRowId > 0); })
                .Complete();
            await eds.HandleCreateRowRequest(new EditCreateRowParams { OwnerUri = Constants.OwnerUri }, efv.Object);

            // Then:
            // ... It should have been successful
            efv.Validate();

            // ... There should be a create in the session
            EditSession s = eds.ActiveSessions[Constants.OwnerUri];
            Assert.True(s.EditCache.Any(e => e.Value is RowCreate));
        }

        [Fact]
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
            Assert.Empty(s.EditCache);
        }
        
        [Fact]
        public async Task RevertRowSucceeds()
        {
            // Setup: Create an edit data service with a session that has an pending edit
            var eds = new EditDataService(null, null, null);
            var session = await GetDefaultSession();
            session.EditCache[0] = new Mock<RowEditBase>().Object;
            eds.ActiveSessions[Constants.OwnerUri] = session;

            // If: I ask to revert a row that has a pending edit
            var efv = new EventFlowValidator<EditRevertRowResult>()
                .AddResultValidation(Assert.NotNull)
                .Complete();
            await eds.HandleRevertRowRequest(new EditRevertRowParams { OwnerUri = Constants.OwnerUri, RowId = 0}, efv.Object);

            // Then: 
            // ... It should have succeeded
            efv.Validate();

            // ... The edit cache should be empty again
            EditSession s = eds.ActiveSessions[Constants.OwnerUri];
            Assert.Empty(s.EditCache);
        }

        [Fact]
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
                    Assert.NotNull(eucr);
                    Assert.NotNull(eucr.Cell);
                    Assert.True(eucr.IsRowDirty);
                })
                .Complete();
            await eds.HandleUpdateCellRequest(new EditUpdateCellParams { OwnerUri = Constants.OwnerUri, RowId = 0}, efv.Object);

            // Then: 
            // ... It should be successful
            efv.Validate();

            // ... Set cell should have been called once
            edit.Verify(e => e.SetCell(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
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
                    Assert.NotNull(esr);
                    Assert.NotEmpty(esr.Subset);
                    Assert.NotEqual(0, esr.RowCount);
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
        [Theory]
        [InlineData(null, "table", "table")]            // Null owner URI
        [InlineData(Common.OwnerUri, null, "table")]    // Null object name
        [InlineData(Common.OwnerUri, "table", null)]    // Null object type
        public async Task InitializeNullParams(string ownerUri, string objName, string objType)
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

            // ... And I initialize an edit session with that
            var efv = new EventFlowValidator<EditInitializeResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleInitializeRequest(initParams, efv.Object);

            // Then:
            // ... An error event should have been raised
            efv.Validate();

            // ... There should not be a session
            Assert.Empty(eds.ActiveSessions);
        }

        [Fact]
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
            var efv = new EventFlowValidator<EditInitializeResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleInitializeRequest(initParams, efv.Object);
            
            // Then:
            // ... An error event should have been sent
            efv.Validate();
            
            // ... The original session should still be there
            Assert.Equal(1, eds.ActiveSessions.Count);
            Assert.Equal(session, eds.ActiveSessions[Constants.OwnerUri]);
        }

        // Disable flaky test for investigation (karlb - 3/13/2018)
        //[Fact]
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
                    Assert.NotNull(esrp);
                    Assert.Equal(Constants.OwnerUri, esrp.OwnerUri);
                    Assert.True(esrp.Success);
                    Assert.Null(esrp.Message);
                })
                .Complete();
            await eds.HandleInitializeRequest(initParams, efv.Object);
            await eds.ActiveSessions[Constants.OwnerUri].InitializeTask;
            
            // Then:
            // ... The event should have been received successfully
            efv.Validate();
            
            // ... The session should have been created
            Assert.Equal(1, eds.ActiveSessions.Count);
            Assert.True(eds.ActiveSessions.Keys.Contains(Constants.OwnerUri));
        }
        
        #endregion
        
        [Theory]
        [InlineData("table", "myschema", new [] { "myschema", "table" })]                 // Use schema
        [InlineData("table", null, new [] { "table" })]                                   // skip schema
        [InlineData("schema.table", "myschema", new [] { "myschema", "schema.table"})]    // Use schema
        [InlineData("schema.table", null, new [] { "schema", "table"})]                   // Split object name into schema
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
            Assert.Equal(expectedNameParts, nameParts);
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
