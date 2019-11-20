//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.Kusto.ServiceLayer.Formatter
{
    public abstract class ASTNodeFormatter
    {
        /// <summary>
        /// Formats the text for a specific node.
        /// </summary>
        public abstract void Format();
        
        internal static LexLocation GetLexLocationForNode(SqlCodeObject obj)
        {
            return obj.Position;
        }
    }
}
