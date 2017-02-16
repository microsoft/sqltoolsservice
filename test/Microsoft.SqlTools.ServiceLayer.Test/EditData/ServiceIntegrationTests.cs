using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.EditData
{
    public class ServiceIntegrationTests
    {
        #region Dispose Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\n\r")]
        [InlineData("Does not exist")]
        public async Task DisposeNullOrMissingSessionId(string sessionId)
        {
            // Setup: Create a edit data service
            var eds = new EditDataService(null, null);

            // If: I ask to dispose of a null session ID
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
            var eds = new EditDataService(null, null);
            eds.ActiveSessions[Common.OwnerUri] = GetDefaultSession();
            
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

        #region Delete Row Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\n\r")]
        [InlineData("Does not exist")]
        public async Task DeleteNullOrMissingSession(string sessionId)
        {
            // Setup: Create an edit data service without a session
            var eds = new EditDataService(null, null);
            
            // If: I ask to delete a row from a non existant session
            var efv = new EventFlowValidator<EditDeleteRowResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleDeleteRowRequest(new EditDeleteRowParams {OwnerUri = sessionId, RowId = 0}, efv.Object);

            // Then: It should have resulted in an error
            efv.Validate();
        }

        [Fact]
        public async Task DeleteThrows()
        {
            // Setup: Create an edit data service with a session that will throw on delete
            var eds = new EditDataService(null, null);
            var session = GetDefaultSession();
            session.EditCache[0] = new Mock<RowEditBase>().Object;
            eds.ActiveSessions[Common.OwnerUri] = session;

            // If: I ask to delete a row that already has an edit pending (ie, the session will throw)
            var efv = new EventFlowValidator<EditDeleteRowResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleDeleteRowRequest(new EditDeleteRowParams {OwnerUri = Common.OwnerUri, RowId = 0}, efv.Object);

            // Then: It should result in an error
            efv.Validate();
        }

        [Fact]
        public async Task DeleteSuccess()
        {
            // Setup: Create an edit data service with a session
            var eds = new EditDataService(null, null);
            eds.ActiveSessions[Common.OwnerUri] = GetDefaultSession();

            // If: I validly ask to delete a row
            var efv = new EventFlowValidator<EditDeleteRowResult>()
                .AddResultValidation(Assert.NotNull)
                .Complete();
            await eds.HandleDeleteRowRequest(new EditDeleteRowParams {OwnerUri = Common.OwnerUri, RowId = 0}, efv.Object);

            // Then: It should be successful
            efv.Validate();
        }

        #endregion

        #region Create Row Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\n\r")]
        [InlineData("Does not exist")]
        public async Task CreateNullOrMissingSession(string sessionId)
        {
            // Setup: Create an edit data service without a session
            var eds = new EditDataService(null, null);

            // If: I ask to create a row in a non existant session
            var efv = new EventFlowValidator<EditCreateRowResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleCreateRowRequest(new EditCreateRowParams { OwnerUri = sessionId }, efv.Object);

            // Then: It should have resulted in an error
            efv.Validate();
        }

        [Fact]
        public async Task CreateThrows()
        {
            // NOTE: This scenario is theoretically impossible, but we'll test it for completeness
            // Setup: Create an edit data service with a session that will throw on create
            var eds = new EditDataService(null, null);
            var session = GetDefaultSession();
            session.EditCache[QueryExecution.Common.StandardRows] = new Mock<RowEditBase>().Object;
            eds.ActiveSessions[Common.OwnerUri] = session;

            // If: I ask to create a row that already has an edit pending (ie, the session will throw)
            var efv = new EventFlowValidator<EditCreateRowResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleCreateRowRequest(new EditCreateRowParams {OwnerUri = Common.OwnerUri}, efv.Object);

            // Then: It should result in an error
            efv.Validate();
        }

        [Fact]
        public async Task CreateSucceeds()
        {
            // Setup: Create an edit data service with a session
            var eds = new EditDataService(null, null);
            eds.ActiveSessions[Common.OwnerUri] = GetDefaultSession();

            // If: I ask to create a row from a non existant session
            var efv = new EventFlowValidator<EditCreateRowResult>()
                .AddResultValidation(ecrr => { Assert.True(ecrr.NewRowId > 0); })
                .Complete();
            await eds.HandleCreateRowRequest(new EditCreateRowParams { OwnerUri = Common.OwnerUri }, efv.Object);

            // Then: It should have resulted in an error
            efv.Validate();
        }

        #endregion

        #region Revert Row Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\n\r")]
        [InlineData("Does not exist")]
        public async Task RevertNullOrMissingSession(string sessionId)
        {
            // Setup: Create an edit data service without a session
            var eds = new EditDataService(null, null);

            // If: I ask to revert a row from a non existant session
            var efv = new EventFlowValidator<EditRevertRowResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleRevertRowRequest(new EditRevertRowParams { OwnerUri = sessionId, RowId = 0}, efv.Object);

            // Then: It should have resulted in an error
            efv.Validate();
        }

        [Fact]
        public async Task RevertThrows()
        {
            // Setup: Create an edit data service with a session that will throw on revert
            var eds = new EditDataService(null, null);
            eds.ActiveSessions[Common.OwnerUri] = GetDefaultSession();

            // If: I ask to revert a row that does not have a pending edit (ie, session will throw)
            var efv = new EventFlowValidator<EditRevertRowResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleRevertRowRequest(new EditRevertRowParams { OwnerUri = Common.OwnerUri, RowId = 0}, efv.Object);

            // Then: It should result in an error
            efv.Validate();
        }

        [Fact]
        public async Task RevertSucceeds()
        {
            // Setup: Create an edit data service with a session that has an pending edit
            var eds = new EditDataService(null, null);
            var session = GetDefaultSession();
            session.EditCache[0] = new Mock<RowEditBase>().Object;
            eds.ActiveSessions[Common.OwnerUri] = session;

            // If: I ask to revert a row that has a pending edit
            var efv = new EventFlowValidator<EditRevertRowResult>()
                .AddResultValidation(Assert.NotNull)
                .Complete();
            await eds.HandleRevertRowRequest(new EditRevertRowParams { OwnerUri = Common.OwnerUri, RowId = 0}, efv.Object);

            // Then: It should have resulted in an error
            efv.Validate();
        }

        #endregion

        #region Update Cell Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\n\r")]
        [InlineData("Does not exist")]
        public async Task UpdateNullOrMissingSession(string sessionId)
        {
            // Setup: Create an edit data service without a session
            var eds = new EditDataService(null, null);

            // If: I ask to update a cell from a non existant session
            var efv = new EventFlowValidator<EditUpdateCellResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleUpdateCellRequest(new EditUpdateCellParams { OwnerUri = sessionId }, efv.Object);

            // Then: It should have resulted in an error
            efv.Validate();
        }

        [Fact]
        public async Task UpdateThrows()
        {
            // Setup: Create an edit data service with a session that will throw on update
            var eds = new EditDataService(null, null);
            var session = GetDefaultSession();
            var edit = new Mock<RowEditBase>();
            edit.Setup(e => e.SetCell(It.IsAny<int>(), It.IsAny<string>())).Throws<Exception>();
            session.EditCache[0] = edit.Object;
            eds.ActiveSessions[Common.OwnerUri] = session;

            // If: I ask to update a cell, that will throw
            var efv = new EventFlowValidator<EditUpdateCellResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleUpdateCellRequest(new EditUpdateCellParams { OwnerUri = Common.OwnerUri, RowId = 0 }, efv.Object);

            // Then: It should result in an error
            efv.Validate();
        }

        [Fact]
        public async Task UpdateSuccess()
        {
            // Setup: Create an edit data service with a session
            var eds = new EditDataService(null, null);
            var session = GetDefaultSession();
            eds.ActiveSessions[Common.OwnerUri] = session;
            var edit = new Mock<RowEditBase>();
            edit.Setup(e => e.SetCell(It.IsAny<int>(), It.IsAny<string>())).Returns(new EditUpdateCellResult
            {
                NewValue = string.Empty,
                HasCorrections = true,
                IsRevert = false,
                IsNull = false
            });
            session.EditCache[0] = edit.Object;

            // If: I validly ask to update a cell
            var efv = new EventFlowValidator<EditUpdateCellResult>()
                .AddResultValidation(eucr =>
                {
                    Assert.NotNull(eucr);
                    Assert.NotNull(eucr.NewValue);
                    Assert.False(eucr.IsRevert);
                    Assert.False(eucr.IsNull);
                })
                .Complete();
            await eds.HandleUpdateCellRequest(new EditUpdateCellParams { OwnerUri = Common.OwnerUri, RowId = 0}, efv.Object);

            // Then: It should be successful
            efv.Validate();
        }

        #endregion

        private static Session GetDefaultSession()
        {
            // ... Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);
            Session s = new Session(q, etm);
            return s;
        }
    }

    public static class EditServiceEventFlowValidatorExtensions
    {
        public static EventFlowValidator<T> AddStandardErrorValidation<T>(this EventFlowValidator<T> evf)
        {
            return evf.AddErrorValidation<string>(p =>
            {
                Assert.NotNull(p);
                Assert.NotEmpty(p);
            });
        }
    }
}
