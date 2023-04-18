//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    internal class UserViewContext : SqlObjectViewContext
    {
        public UserViewContext(InitializeViewRequestParams parameters, ServerConnection connection, UserPrototypeData originalUserData) : base(parameters)
        {
            this.OriginalUserData = originalUserData;
            this.Connection = connection;
        }

        public UserPrototypeData OriginalUserData { get; }

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