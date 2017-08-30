//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.TaskServices;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.TaskServices
{
    public class SmoScriptableTaskOperationStub : SmoScriptableTaskOperation
    {
        private Server server;
        public string DatabaseName { get; set; }
        public SmoScriptableTaskOperationStub(Server server)
        {
            this.server = server;
        }
        public override string ErrorMessage
        {
            get
            {
                return string.Empty;
            }
        }

        public override Server Server
        {
            get
            {
                return server;
            }
        }

        public override void Cancel()
        {
        }

        public string TableName { get; set; }

        public override void Execute()
        {
            var database = server.Databases[DatabaseName];
            Table table = new Table(database, TableName, "test");
            Column column = new Column(table, "c1");
            column.DataType = DataType.Int;
            table.Columns.Add(column);
            database.Tables.Add(table);
            table.Create();
        }
    }
}
