//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
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

        public override void Create(ISqlObjectViewContext context, SqlObject obj)
        {
            throw new NotSupportedException(NotSupportedException);
        }

        public override SqlObjectViewInfo InitializeObjectView(Contracts.InitializeViewRequestParams requestParams, out ISqlObjectViewContext context)
        {
            throw new NotSupportedException(NotSupportedException);
        }

        public override Type GetObjectType()
        {
            throw new NotSupportedException(NotSupportedException);
        }

        public override string Script(ISqlObjectViewContext context, SqlObject obj)
        {
            throw new NotSupportedException(NotSupportedException);
        }

        public override void Update(ISqlObjectViewContext context, SqlObject obj)
        {
            throw new NotSupportedException(NotSupportedException);
        }
    }
}