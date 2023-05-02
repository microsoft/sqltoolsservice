//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class DatabaseRoleViewContext : SqlObjectViewContext
    {
        public ServerConnection Connection { get; }
        public DatabaseRoleViewContext(Contracts.InitializeViewRequestParams parameters, ServerConnection connection) : base(parameters)
        {
            this.Connection = connection;
        }

        public override void Dispose()
        {
            try
            {
                this.Connection.Disconnect();
            }
            catch
            {
                // ignore
            }
        }
    }
}