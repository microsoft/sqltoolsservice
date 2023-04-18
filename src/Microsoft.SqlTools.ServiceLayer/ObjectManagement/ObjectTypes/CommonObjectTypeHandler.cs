//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// A handler for the object types that only has rename/drop support
    /// </summary>
    public class CommonObjectTypeHandler : ObjectTypeHandler
    {
        // The message is only used in developing time, no need to be localized.
        private const string NotSupportedException = "This operation is not supported for this object type";

        public CommonObjectTypeHandler(ConnectionService connectionService) : base(connectionService) { }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.Column ||
                   objectType == SqlObjectType.Table ||
                   objectType == SqlObjectType.View;
        }

        public override Task Save(SqlObjectViewContext context, SqlObject obj)
        {
            throw new NotSupportedException(NotSupportedException);
        }

        public override Task<InitializeViewResult> InitializeObjectView(Contracts.InitializeViewRequestParams requestParams)
        {
            throw new NotSupportedException(NotSupportedException);
        }

        public override Type GetObjectType()
        {
            throw new NotSupportedException(NotSupportedException);
        }

        public override Task<string> Script(SqlObjectViewContext context, SqlObject obj)
        {
            throw new NotSupportedException(NotSupportedException);
        }
    }
}