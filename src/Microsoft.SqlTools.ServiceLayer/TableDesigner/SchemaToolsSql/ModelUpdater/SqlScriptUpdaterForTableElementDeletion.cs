//------------------------------------------------------------------------------------
// <copyright file="SqlScriptUpdaterForTableElementDeletion.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Tools.Components.Diagnostics;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Microsoft.Data.Tools.Schema.Sql.SchemaModel.ModelUpdater
{
    internal static class SqlScriptUpdaterForTableElementDeletion
    {
        /// <summary>
        /// Enum to represent type of element(a column or an inline constraint) location in a table
        /// </summary>
        private enum ElementLocationType
        {
            /// <summary>
            /// First element in a table
            /// </summary>
            FirstElement,

            /// <summary>
            /// A middle element in a table
            /// </summary>
            MiddleElement,

            /// <summary>
            /// Last element in a table
            /// </summary>
            LastElement,

            /// <summary>
            /// Column scoped inline constraint in a table. Example: default constraint from "C1 INT DEFAULT 1"
            /// </summary>
            ColumnScopedInlineConstraint
        }

        public static IList<SqlScriptUpdateItem> DeleteColumn(SqlColumn column)
        {
            if (column == null)
            {
                throw new ArgumentNullException("column");
            }

            return DeleteElementImpl((SqlTable)column.Parent, column);
        }

        public static IList<SqlScriptUpdateItem> DeleteConstraint(SqlConstraint constraint)
        {
            if (constraint == null)
            {
                throw new ArgumentNullException("constraint");
            }

            return DeleteElementImpl((SqlTable)constraint.DefiningTable, constraint);
        }

        public static IList<SqlScriptUpdateItem> DeleteInlineIndex(SqlIndex inlineIndex)
        {
            if (inlineIndex == null)
            {
                throw new ArgumentNullException("inlineIndex");
            }
            return DeleteElementImpl(inlineIndex.IndexedObject, inlineIndex);
        }

        private static IList<SqlScriptUpdateItem> DeleteElementImpl(ISqlColumnSource table, SqlModelElement sqlModelElement)
        {
            TSqlFragment ast;
            int startOffset, endOffset;
            GetAstAndStartEndOffset(sqlModelElement, out ast, out startOffset, out endOffset);

            // Go through columns and constrains in the table to determine location type for the element to be deleted
            ElementLocationType elementLocationType = GetElementLocationType(table, startOffset, endOffset);

            List<SqlScriptUpdateItem> updateItems = new List<SqlScriptUpdateItem>();
            IList<TSqlParserToken> tokenStream = ast.ScriptTokenStream;

            // Delete the element based on its element location type
            switch (elementLocationType)
            {
                case ElementLocationType.FirstElement:
                case ElementLocationType.MiddleElement:
                    updateItems.Add(DeleteElement(tokenStream, ast));
                    break;

                case ElementLocationType.ColumnScopedInlineConstraint:
                    updateItems.Add(DeleteElement(tokenStream, ast, isColumnScopedInlineConstraint: true));
                    break;

                case ElementLocationType.LastElement:
                    SqlScriptUpdateItem removeColumn = DeleteElement(tokenStream, ast);
                    bool allowRemoveLastColumn = (table is SqlTable && (table as SqlTable).IsEdge);
                    updateItems.Add(removeColumn);
                    updateItems.AddRange(RemoveOrphanCommaOrParentheses(tokenStream, ast, removeColumn, allowRemoveLastColumn));
                    break;
            }

            return updateItems;
        }

        private static SqlScriptUpdateItem DeleteElement(IList<TSqlParserToken> tokenStream, TSqlFragment ast, bool isColumnScopedInlineConstraint = false)
        {
            int startTokenIndex;
            int endTokenIndex;
            LocateElementTokenIndexes(
                tokenStream,
                ast,
                includeTrailingComma: !isColumnScopedInlineConstraint,  // we want to delete the trailing comma only for columns and table-scoped constraints, not for column-scoped constraints
                startTokenIndex: out startTokenIndex,
                endTokenIndex: out endTokenIndex);

            // Expand token scope to include preceding and tailing non-newline whitespace
            if (!isColumnScopedInlineConstraint)
            {
                ExpandTokenScopeToIncludeWhiteSpace(tokenStream, ref startTokenIndex, ref endTokenIndex);
            }
            else
            {
                // For a column-scoped constraint, we expand the portion to be deleted to include whitespace only when
                // the constraint is at its own line. if the constraint shares the same line with any other non-whitespace
                // AST, we don't remove its preceding and trailing whitespace.
                int start = startTokenIndex;
                int end = endTokenIndex;
                ExpandTokenScopeToIncludeWhiteSpace(tokenStream, ref start, ref end);
                if (AreBothNewLines(tokenStream, start, end))
                {
                    startTokenIndex = start;
                    endTokenIndex = end;
                }
            }

            int endOffset;
            RemoveEmptyLine(tokenStream, startTokenIndex, endTokenIndex, out endOffset);

            TSqlParserToken startToken = tokenStream[startTokenIndex];

            SqlScriptUpdateItem item = new SqlScriptUpdateItem(
                startToken.Offset,
                startToken.Line,
                startToken.Column,
                length: endOffset - startToken.Offset + 1,
                newText: String.Empty); // deleting

            return item;
        }

        /// <summary>
        /// Locate deleting table element start and end token index
        /// </summary>
        private static void LocateElementTokenIndexes(IList<TSqlParserToken> tokenStream, TSqlFragment ast, bool includeTrailingComma, out int startTokenIndex, out int endTokenIndex)
        {
            startTokenIndex = ast.FirstTokenIndex;
            endTokenIndex = ast.LastTokenIndex;

            if (includeTrailingComma)
            {
                // Try to find right-hand comma
                TSqlParserToken commaToken;
                int commaTokenIndex;
                SqlModelUpdaterUtils.FindToken(
                    tokenStream,
                    endTokenIndex + 1,
                    token => token.TokenType == TSqlTokenType.Comma,
                    token => token.TokenType == TSqlTokenType.WhiteSpace
                             || token.TokenType == TSqlTokenType.SingleLineComment
                             || token.TokenType == TSqlTokenType.MultilineComment,
                    out commaTokenIndex,
                    out commaToken);

                if (commaToken != null)
                {
                    endTokenIndex = commaTokenIndex;
                }
            }
        }

        /// <summary>
        /// Expand the token scope of the element to be deleted to include preceding and tailing non-newline whitespace
        /// </summary>
        private static void ExpandTokenScopeToIncludeWhiteSpace(IList<TSqlParserToken> tokenStream, ref int startTokenIndex, ref int endTokenIndex)
        {
            // expand starting token
            while (startTokenIndex > 0)
            {
                if (IsNonNewLineWhiteSpace(tokenStream[startTokenIndex - 1]))
                {
                    startTokenIndex--;
                }
                else
                {
                    break;
                }
            }

            // expand ending token
            while (endTokenIndex < tokenStream.Count - 1)
            {
                if (IsNonNewLineWhiteSpace(tokenStream[endTokenIndex + 1]))
                {
                    endTokenIndex++;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// If the line is empty after delete, remove that empty line
        /// </summary>
        private static void RemoveEmptyLine(IList<TSqlParserToken> tokenStream, int startTokenIndex, int endTokenIndex, out int endOffset)
        {
            TSqlParserToken endToken = AreBothNewLines(tokenStream, startTokenIndex, endTokenIndex) ? tokenStream[endTokenIndex + 1] : tokenStream[endTokenIndex];
            endOffset = endToken.Offset + endToken.Text.Length - 1;
        }

        /// <summary>
        /// If precedding and tailing of an element are both newlines
        /// </summary>
        private static bool AreBothNewLines(IList<TSqlParserToken> tokenStream, int startTokenIndex, int endTokenIndex)
        {
            return
                startTokenIndex > 0 &&
                IsNewLineToken(tokenStream[startTokenIndex - 1]) &&
                endTokenIndex < tokenStream.Count - 1 &&
                IsNewLineToken(tokenStream[endTokenIndex + 1]);
        }

        /// <summary>
        /// Remove the orphan comma after the table element before it was deleted.
        /// If the element is the last in the list, it removes the surrounding parentheses, which needs to be done
        /// in 2 edits, to not overlap with the column delete inside the parentheses.
        /// </summary>
        private static List<SqlScriptUpdateItem> RemoveOrphanCommaOrParentheses(IList<TSqlParserToken> tokenStream, TSqlFragment ast, SqlScriptUpdateItem removeColumn, bool allowRemoveLastColumn)
        {
            // ast represents a column or a table-scoped constraint

            List<SqlScriptUpdateItem> updates = new List<SqlScriptUpdateItem>();

            TSqlParserToken commaToken;
            int commaTokenIndex;
            SqlModelUpdaterUtils.FindTokenBackward(
                tokenStream,
                ast.FirstTokenIndex - 1,
                token => token.TokenType == TSqlTokenType.Comma,
                out commaTokenIndex,
                out commaToken);

            if (commaToken != null)
            {
                updates.Add(new SqlScriptUpdateItem(
                    commaToken.Offset,
                    commaToken.Line,
                    commaToken.Column,
                    length: commaToken.Text.Length,
                    newText: String.Empty)); // deleting

                return updates;
            }

            if (!allowRemoveLastColumn)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Failed to find comma.");
            }

            // no comma found, try to find surrounding parentheses.
            TSqlParserToken leftParenthesis;
            SqlModelUpdaterUtils.FindTokenBackward(
                tokenStream,
                ast.FirstTokenIndex - 1,
                token => token.TokenType == TSqlTokenType.LeftParenthesis,
                out commaTokenIndex,
                out leftParenthesis);

            TSqlParserToken rightParenthesis;
            SqlModelUpdaterUtils.FindToken(
                tokenStream,
                ast.LastTokenIndex + 1,
                token => token.TokenType == TSqlTokenType.RightParenthesis,
                out commaTokenIndex,
                out rightParenthesis);

            if (leftParenthesis == null || rightParenthesis == null )
            {
                SqlModelUpdaterUtils.TraceAndThrow("Failed to find surrounding parentheses.");
            }

            // remove left parenthesis
            updates.Add(new SqlScriptUpdateItem(
                leftParenthesis.Offset,
                leftParenthesis.Line,
                leftParenthesis.Column,
                length: leftParenthesis.Text.Length,
                newText: String.Empty)); // deleting

            // remove right parenthesis
            updates.Add(new SqlScriptUpdateItem(
                rightParenthesis.Offset,
                rightParenthesis.Line,
                rightParenthesis.Column,
                length: rightParenthesis.Text.Length,
                newText: " ")); // replace with one space

            return updates;
        }

        private static bool IsNonNewLineWhiteSpace(TSqlParserToken token)
        {
            return
                token.TokenType == TSqlTokenType.WhiteSpace &&
                !IsNewLineToken(token);
        }

        private static bool IsNewLineToken(TSqlParserToken token)
        {
            // we can have three types of newline tokens: '\r', '\n' and "\r\n". "\n\r" is treated as two newline tokens: '\n' and '\r'.
            return
                token.TokenType == TSqlTokenType.WhiteSpace &&
                (token.Text.EndsWith("\r", StringComparison.OrdinalIgnoreCase) || token.Text.EndsWith("\n", StringComparison.OrdinalIgnoreCase));
        }

        private static ElementLocationType GetElementLocationType(ISqlColumnSource source, int startOffset, int endOffset)
        {
            bool hasElementBefore = false;
            bool hasElementAfter = false;
            List<SqlModelElement> inlineIndexes =
                source.Indexes.Where(index => index.GetAnnotations<SqlInlineIndexAnnotation>().Count > 0).Cast<SqlModelElement>().ToList();
            IEnumerable<SqlModelElement> allInlineElements = source.Columns.Union<SqlModelElement>(inlineIndexes);

            SqlTable table = source as SqlTable;
            if (table != null)
            {
                allInlineElements = allInlineElements.Union<SqlModelElement>(table.Constraints);
            }

            SqlTracer.AssertTraceEvent(allInlineElements.Any(), TraceEventType.Error, SqlTraceId.CoreServices, "allColumnsAndConstraints enumeration should not be empty.");

            foreach (SqlModelElement modelElement in allInlineElements)
            {
                TSqlFragment currentAst;
                int currentStartOffset;
                int currentEndOffset;
                GetAstAndStartEndOffset(modelElement, out currentAst, out currentStartOffset, out currentEndOffset);

                if (currentStartOffset < startOffset && currentEndOffset >= endOffset)
                {
                    // It is a column-scoped inline constraint
                    return ElementLocationType.ColumnScopedInlineConstraint;
                }

                if (currentEndOffset < startOffset)
                {
                    hasElementBefore = true;
                }
                else if (currentStartOffset > endOffset)
                {
                    hasElementAfter = true;
                }
            }

            if (hasElementBefore && hasElementAfter)
            {
                // It is a middle element
                return ElementLocationType.MiddleElement;
            }

            SqlTracer.AssertTraceEvent(hasElementBefore || hasElementAfter, TraceEventType.Error, SqlTraceId.CoreServices, "The element must either has before element, after element or both.");

            return hasElementBefore ? ElementLocationType.LastElement : ElementLocationType.FirstElement;
        }

        private static void GetAstAndStartEndOffset(SqlModelElement element, out TSqlFragment ast, out int startOffset, out int endOffset)
        {
            ast = SqlModelUpdaterUtils.GetPrimaryAst<TSqlFragment>(element);

            if (ast != null)
            {
                startOffset = ast.StartOffset;
                endOffset = ast.StartOffset + ast.FragmentLength - 1;
            }
            else
            {
                startOffset = 0;
                endOffset = 0;
            }
        }
    }
}
