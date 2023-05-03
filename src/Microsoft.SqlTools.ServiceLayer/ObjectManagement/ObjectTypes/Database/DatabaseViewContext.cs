//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class DatabaseViewContext : SqlObjectViewContext
    {
        public DatabaseViewContext(InitializeViewRequestParams parameters, ServerConnection connection) : base(parameters)
        {
            this.Connection = connection;
        }

        public ServerConnection Connection { get; }

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