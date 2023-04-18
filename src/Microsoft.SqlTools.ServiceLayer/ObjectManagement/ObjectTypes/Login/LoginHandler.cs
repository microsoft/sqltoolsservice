//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.Security;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Login object type handler
    /// </summary>
    public class LoginHandler : ObjectTypeHandler
    {
        private LoginServiceHandlerImpl loginImpl = new LoginServiceHandlerImpl();

        public LoginHandler(ConnectionService connectionService) : base(connectionService) { }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.ServerLevelLogin;
        }

        public override void Create(ISqlObjectViewContext context, SqlObject obj)
        {
            throw new NotImplementedException();
        }

        public override Type GetObjectType()
        {
            throw new NotImplementedException();
        }

        public override SqlObjectViewInfo InitializeObjectView(InitializeViewRequestParams requestParams, out ISqlObjectViewContext context)
        {
            throw new NotImplementedException();
        }

        public override string Script(ISqlObjectViewContext context, SqlObject obj)
        {
            throw new NotImplementedException();
        }

        public override void Update(ISqlObjectViewContext context, SqlObject obj)
        {
            throw new NotImplementedException();
        }
    }
}