//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    /// <summary>
    /// Class to pass back SqlCmd specific properties from Parser to Query Execution
    /// </summary>
    public class SqlCmdCommand
    {
        internal SqlCmdCommand(LexerTokenType tokenType)
        {
            this.LexerTokenType = tokenType;
        }
    
        public LexerTokenType LexerTokenType { get; private set; }
    }
}
