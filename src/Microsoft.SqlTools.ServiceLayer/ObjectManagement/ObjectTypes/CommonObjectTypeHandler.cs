//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class CommonObjectType : SqlObject { }

    public class CommonObjectTypeViewContext : SqlObjectViewContext
    {
        public CommonObjectTypeViewContext(InitializeViewRequestParams parameters) : base(parameters) { }

        public override void Dispose() { }
    }

    /// <summary>
    /// A handler for the object types that only has rename/drop support
    /// </summary>
    public class CommonObjectTypeHandler : ObjectTypeHandler<CommonObjectType, CommonObjectTypeViewContext>
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

        public override Task Save(CommonObjectTypeViewContext context, CommonObjectType obj)
        {
            throw new NotSupportedException(NotSupportedException);
        }

        public override Task<InitializeViewResult> InitializeObjectView(Contracts.InitializeViewRequestParams requestParams)
        {
            throw new NotSupportedException(NotSupportedException);
        }

        public override Task<string> Script(CommonObjectTypeViewContext context, CommonObjectType obj)
        {
            throw new NotSupportedException(NotSupportedException);
        }
    }
}