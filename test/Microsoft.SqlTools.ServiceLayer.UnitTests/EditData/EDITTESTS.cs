using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.EditData
{
    public class EDITTESTS
    {
        [Fact]
        public async Task TestHarness()
        {
            QueryExecutionService qes = new QueryExecutionService(ConnectionService.Instance, WorkspaceService<SqlToolsSettings>.Instance);
            EditDataService eds = new EditDataService(qes, ConnectionService.Instance, new SmoEditMetadataFactory());
            await ConnectionService.Instance.Connect(new ConnectParams
            {
                Connection = new ConnectionDetails
                {
                    AuthenticationType = "Integrated",
                    ServerName = "localhost",
                    DatabaseName = "testytest"
                },
                OwnerUri = Common.OwnerUri
            });

            var efv = new EventFlowValidator<EditInitializeResult>()
                .AddResultValidation(Assert.NotNull)
                .AddEventValidation(BatchStartEvent.Type, Assert.NotNull)
                .AddEventValidation(MessageEvent.Type, Assert.NotNull)
                .AddEventValidation(ResultSetCompleteEvent.Type, Assert.NotNull)
                .AddEventValidation(BatchCompleteEvent.Type, Assert.NotNull)
                .AddEventValidation(QueryCompleteEvent.Type, Assert.NotNull)
                .AddEventValidation(EditSessionReadyEvent.Type, esrp => Assert.True(esrp.Success))
                .Complete();
            var initParams = new EditInitializeParams
            {
                OwnerUri = Common.OwnerUri,
                ObjectName = "defaulttest",
                ObjectType = "table"
            };
            await eds.HandleInitializeRequest(initParams, efv.Object);
            await eds.InitializeWaitHandles[Common.OwnerUri].Task;
            efv.Validate();
        }

    }
}
