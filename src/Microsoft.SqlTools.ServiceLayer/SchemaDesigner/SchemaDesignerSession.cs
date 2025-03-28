//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using DacSchemaDesigner = Microsoft.Data.Tools.Sql.DesignServices.TableDesigner.SchemaDesigner;
using Microsoft.SqlTools.ServiceLayer.Connection;
using System.Threading.Tasks;
using Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerSession : IDisposable
    {
        private SchemaDesignerModel InitialSchema;
        private string SessionId;
        DacSchemaDesigner schemaDesigner;
        private string connectionString;

        public SchemaDesignerSession(string sessionId, SchemaDesignerModel initialSchema)
        {
            if (!ConnectionService.Instance.TryFindConnection(sessionId, out ConnectionInfo connInfo))
            {
                throw new Exception(SR.QueryServiceQueryInvalidOwnerUri);
            }
            connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
            InitialSchema = initialSchema;
            SessionId = sessionId;
            schemaDesigner = new DacSchemaDesigner(connectionString, connInfo.ConnectionDetails.AzureAccountToken);
        }

        private string getAzureToken()
        {
            if (ConnectionService.Instance.TryFindConnection(SessionId, out ConnectionInfo connInfo))
            {
                return connInfo.ConnectionDetails.AzureAccountToken;
            }
            return null;
        }

        public async Task<GetReportResponse> GetReport(SchemaDesignerModel updatedSchema)
        {
            if (schemaDesigner == null)
            {
                schemaDesigner.Dispose();
            }
            schemaDesigner = new DacSchemaDesigner(connectionString, getAzureToken());
            return await SchemaDesignerUpdater.GenerateUpdateScripts(InitialSchema, updatedSchema, schemaDesigner);
        }

        public void Dispose()
        {
            TableDesignerCacheManager.InvalidateItem(connectionString);
        }
    }
}