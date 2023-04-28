//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class ServerRoleViewContext : SqlObjectViewContext
    {
        public ServerRoleViewContext(Contracts.InitializeViewRequestParams parameters) : base(parameters)
        {
        }

        public override void Dispose()
        {
        }
    }
}