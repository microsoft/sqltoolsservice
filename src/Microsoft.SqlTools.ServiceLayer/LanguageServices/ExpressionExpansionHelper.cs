//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    public static class ExpresisonExpansionHelper
    {

        public static readonly Func<SqlCodeObject, SqlCodeObject>[] SupportedExpressions =
        {
            node => node as SqlInsertStatement,
            node => node as SqlSelectStatement,
        };

        // Helper method to check if cursor is within a node's scope
        private static bool IsCursorInNodeScope(SqlCodeObject node, ScriptDocumentInfo scriptDocumentInfo)
        {
            int nodeStartLineNumber = node.StartLocation.LineNumber - 1;
            int nodeEndLineNumber = node.EndLocation.LineNumber - 1;

            bool isStartPositionBeforeCursor =
                nodeStartLineNumber < scriptDocumentInfo.StartLine ||
                (nodeStartLineNumber == scriptDocumentInfo.StartLine &&
                 node.StartLocation.ColumnNumber <= scriptDocumentInfo.StartColumn);

            bool isEndPositionAfterCursor =
                nodeEndLineNumber > scriptDocumentInfo.StartLine ||
                (nodeEndLineNumber == scriptDocumentInfo.StartLine &&
                 node.EndLocation.ColumnNumber >= scriptDocumentInfo.EndColumn);

            return isStartPositionBeforeCursor && isEndPositionAfterCursor;
        }

        public static SqlCodeObject TryGetSqlExpression(SqlCodeObject currentNode, ScriptDocumentInfo scriptDocumentInfo)
        {
            if (currentNode == null || scriptDocumentInfo == null)
            {
                return null;
            }

            foreach (var expression in SupportedExpressions)
            {
                var result = expression(currentNode);
                if (result != null)
                {
                    return result;
                }
            }

            foreach (SqlCodeObject child in currentNode.Children)
            {
                if (IsCursorInNodeScope(child, scriptDocumentInfo))
                {
                    SqlCodeObject childExpression = TryGetSqlExpression(child, scriptDocumentInfo);
                    if (childExpression != null)
                    {
                        return childExpression;
                    }
                }
            }

            return null;
        }
    }
}