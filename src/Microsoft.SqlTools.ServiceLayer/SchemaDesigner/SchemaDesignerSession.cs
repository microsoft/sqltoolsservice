//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using TableSchemaDesigner = Microsoft.Data.Tools.Sql.DesignServices.TableDesigner.SchemaDesigner;
using Microsoft.SqlTools.ServiceLayer.Connection;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerSession : IDisposable
    {
        private SchemaDesignerModel InitialSchema;
        private string SessionId;

        public SchemaDesignerSession(string sessionId, SchemaDesignerModel initialSchema)
        {
            if (!ConnectionService.Instance.TryFindConnection(sessionId, out ConnectionInfo connInfo))
            {
                throw new Exception(SR.QueryServiceQueryInvalidOwnerUri);
            }
            var connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
            InitialSchema = initialSchema;
            SessionId = sessionId;
            var schemaDesigner = new TableSchemaDesigner(connectionString, connInfo.ConnectionDetails.AzureAccountToken);
        }

        public GetReportResponse GetReport(SchemaDesignerModel updatedSchema)
        {
            return SchemaDesignerUpdater.GenerateUpdateScripts(InitialSchema, updatedSchema);
        }

        public void Dispose()
        {
        }
    }
}