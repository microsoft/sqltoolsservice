//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Security;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// User object type handler
    /// </summary>
    public class UserHandler : ObjectTypeHandler
    {
        private UserServiceHandlerImpl userImpl = new UserServiceHandlerImpl();

        public UserHandler(ConnectionService connectionService) : base(connectionService)
        {
        }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.User;
        }

        public override void Create(ISqlObjectViewContext context, SqlObject obj)
        {
            
        }

        public override Type GetObjectType()
        {
            return typeof(UserInfo);
        }

        public override SqlObjectViewInfo InitializeObjectView(Contracts.InitializeViewRequestParams requestParams, out ISqlObjectViewContext context)
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