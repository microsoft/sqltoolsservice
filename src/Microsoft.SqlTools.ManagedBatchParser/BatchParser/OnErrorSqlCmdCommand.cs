//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    public class OnErrorSqlCmdCommand : SqlCmdCommand
    {
        public OnErrorSqlCmdCommand(OnErrorAction action) : base(LexerTokenType.OnError)
        {
            Action = action;
        }

        public OnErrorAction Action { get; private set; }
    }
}
