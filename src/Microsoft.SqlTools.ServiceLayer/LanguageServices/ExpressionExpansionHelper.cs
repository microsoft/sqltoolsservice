//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Helper class for expanding SQL expressions in the editor
    /// </summary>
    public static class ExpressionExpansionHelper
    {
        public class SupportedExpressionObject
        {
            public SupportedExpressions ExpressionType { get; set; }
            public SqlCodeObject codeObject { get; set; }
        }

        /// <summary>
        /// Enumeration of supported SQL expressions for expansion
        /// </summary>
        public enum SupportedExpressions
        {
            SELECT_STAR_STATEMENT,
            INSERT_STATEMENT,
        }

        /// <summary>
        /// Checks if cursor is within a node's scope
        /// </summary>
        /// <param name="node">The SQL code object to check</param>
        /// <param name="scriptDocumentInfo">Information about the script document</param>
        /// <returns>True if cursor is within the node's scope, false otherwise</returns>
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

        /// <summary>
        /// Tries to find a supported SQL expression at the current cursor position
        /// </summary>
        /// <param name="currentNode">The current SQL code object to check</param>
        /// <param name="scriptDocumentInfo">Information about the script document</param>
        /// <returns>A supported expression object if found, null otherwise</returns>
        public static SupportedExpressionObject? TryGetSqlExpression(SqlCodeObject currentNode, ScriptDocumentInfo scriptDocumentInfo)
        {
            if (currentNode == null || scriptDocumentInfo == null)
            {
                return null;
            }

            if (currentNode is SqlSelectStarExpression selectStatement)
            {
                return new SupportedExpressionObject
                {
                    ExpressionType = SupportedExpressions.SELECT_STAR_STATEMENT,
                    codeObject = selectStatement
                };
            }
            else if (currentNode is SqlInsertSpecification insertStatement)
            {
                return new SupportedExpressionObject
                {
                    ExpressionType = SupportedExpressions.INSERT_STATEMENT,
                    codeObject = insertStatement
                };
            }

            foreach (SqlCodeObject child in currentNode.Children)
            {
                if (IsCursorInNodeScope(child, scriptDocumentInfo))
                {
                    SupportedExpressionObject? childExpression = TryGetSqlExpression(child, scriptDocumentInfo);
                    if (childExpression != null)
                    {
                        return childExpression;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Expands the SQL expression at the current cursor position
        /// </summary>
        /// <param name="scriptDocumentInfo">Information about the script document</param>
        /// <returns>Array of completion items for the expanded expression</returns>
        public static CompletionItem[]? ExpandExpression(ScriptDocumentInfo scriptDocumentInfo)
        {
            var expression = TryGetSqlExpression(scriptDocumentInfo.ScriptParseInfo.ParseResult.Script, scriptDocumentInfo);
            if (expression == null)
            {
                return null;
            }


            return expression.ExpressionType switch
            {
                SupportedExpressions.SELECT_STAR_STATEMENT => ExpandSelectStarExpression(scriptDocumentInfo, (SqlSelectStarExpression)expression.codeObject),
                SupportedExpressions.INSERT_STATEMENT => ExpandInsertExpression(scriptDocumentInfo, (SqlInsertSpecification)expression.codeObject),
                _ => null,
            };
        }

        /// <summary>
        /// Expands a SELECT * expression into individual column names
        /// </summary>
        /// <param name="scriptDocumentInfo">Information about the script document</param>
        /// <param name="selectStarExpression">The SELECT * expression to expand</param>
        /// <returns>Array of completion items for the expanded SELECT * expression</returns>
        public static CompletionItem[]? ExpandSelectStarExpression(ScriptDocumentInfo scriptDocumentInfo, SqlSelectStarExpression selectStarExpression)
        {
            if (selectStarExpression == null)
            {
                return null;
            }


            if (selectStarExpression.BoundTables == null)
            {
                return null;
            }

            // Get object identifier for expressions like a.*
            SqlObjectIdentifier? starObjectIdentifier = null;
            if (selectStarExpression.Children.Any())
            {
                starObjectIdentifier = selectStarExpression.Children.ElementAt(0) as SqlObjectIdentifier;
            }

            List<ITabular> boundedTableList = selectStarExpression.BoundTables.ToList();
            if (!boundedTableList.Any())
            {
                return null;
            }

            IList<string> columnNames = new List<string>();
            /*
             We include table names in 2 conditions.
             1. When there are multiple tables to avoid column ambiguity
             2. When there is single table with an alias
            */
            bool includeTableName = boundedTableList.Count > 1 || (boundedTableList.Count == 1 && boundedTableList[0] != boundedTableList[0].Unaliased);

            // Handing case for object identifiers where the column names will contain the identifier for eg: a.* becomes a.column_name
            if (starObjectIdentifier != null)
            {
                string objectIdentifierName = starObjectIdentifier.ObjectName.ToString();
                ITabular relatedTable = boundedTableList.Single(t => t.Name == objectIdentifierName);
                if (relatedTable != null)
                {
                    columnNames = relatedTable.Columns
                    .Select(c => $"{Utils.MakeSqlBracket(objectIdentifierName)}.{Utils.MakeSqlBracket(c.Name)}")
                    .ToList();
                }
            }
            else
            {
                foreach (var table in boundedTableList)
                {
                    foreach (var column in table.Columns)
                    {
                        string columnName = includeTableName 
                        ? $"{Utils.MakeSqlBracket(table.Name)}.{Utils.MakeSqlBracket(column.Name)}"
                        : Utils.MakeSqlBracket(column.Name);
                        if (!columnNames.Contains(columnName))
                        {
                            columnNames.Add(columnName);
                        }
                    }
                }
            }

            if (!columnNames.Any())
            {
                return null;
            }

            var insertText = Environment.NewLine +
            "\t" + String.Join("," + Environment.NewLine + "\t", columnNames.ToArray()) +
                 Environment.NewLine;

            var completionItems = new CompletionItem[] {
                new CompletionItem
                {
                    InsertText = insertText,
                    Label = insertText,
                    Detail = "Expand * expression into column names",
                    Kind = CompletionItemKind.Text,
                    /*
                    Vscode/ADS only shows completion items that match the text present in the editor. However, in case of star expansion that is never going to happen as columns names are different than '*'. 
                    Therefore adding an explicit filterText that contains the original star expression to trick vscode/ADS into showing this suggestion item. 
                    */
                    FilterText = selectStarExpression.Sql,
                    Preselect = true,
                    TextEdit = new TextEdit {
                        NewText = insertText,
                        Range = new Workspace.Contracts.Range {
                            Start = new Workspace.Contracts.Position{
                                Line = scriptDocumentInfo.StartLine,
                                Character = selectStarExpression.StartLocation.ColumnNumber - 1
                            },
                            End = new Workspace.Contracts.Position {
                                Line = scriptDocumentInfo.StartLine,
                                Character = selectStarExpression.EndLocation.ColumnNumber - 1
                            }
                        }
                    }
                }
            };
            return completionItems;
        }

        public static CompletionItem[] ExpandInsertExpression(ScriptDocumentInfo scriptDocumentInfo, SqlInsertSpecification insertStatement)
        {
            // Find the table reference in the insert statement
            var tableRef = insertStatement.Target as SqlTableRefExpression;
            if (tableRef == null)
            {
                return null;
            }

            var boundTable = tableRef.BoundTable;

            if (boundTable == null)
            {
                return null;
            }

            var IsCursorAtTableRef = IsCursorInNodeScope(tableRef, scriptDocumentInfo);

            if (IsCursorAtTableRef)
            {
                IList<string> columnNames = new List<string>();
                IList<bool> doesColumnHaveDefault = new List<bool>();
                IList<bool> isColumnNullable = new List<bool>();

                foreach (var column in boundTable.Columns)
                {
                    if (column.ComputedColumnInfo != null)
                    {
                        continue;
                    }
                    columnNames.Add(column.Name);
                    doesColumnHaveDefault.Add(column.DefaultValue != null);
                    isColumnNullable.Add(column.Nullable);
                }

                StringBuilder insertStatementBuilder = new StringBuilder();
                insertStatementBuilder.Append(insertStatement.Sql);
                insertStatementBuilder.Append(Environment.NewLine);

                insertStatementBuilder.Append("(");

                for (int i = 0; i < columnNames.Count; i++)
                {
                    insertStatementBuilder.Append($"[{columnNames[i]}]");
                    if (i < columnNames.Count - 1)
                    {
                        insertStatementBuilder.Append(", ");
                    }
                }

                insertStatementBuilder.Append(")");
                insertStatementBuilder.Append(Environment.NewLine + "VALUES" + Environment.NewLine);
                insertStatementBuilder.Append("(");

                for (int i = 0; i < columnNames.Count; i++)
                {
                    if (doesColumnHaveDefault[i])
                    {
                        insertStatementBuilder.Append("DEFAULT, ");
                    }
                    else
                    {
                        if (isColumnNullable[i])
                        {
                            insertStatementBuilder.Append("NULL, ");
                        }
                        else
                        {
                            insertStatementBuilder.Append("DEFAULT, ");
                        }
                    }
                }

                insertStatementBuilder.Remove(insertStatementBuilder.Length - 2, 2); // Remove the last comma and space

                insertStatementBuilder.Append(')');
                string insertStatementText = insertStatementBuilder.ToString();

                var completionItem = new CompletionItem
                {
                    Label = insertStatementText,
                    Kind = CompletionItemKind.Text,
                    InsertText = insertStatementText,
                    Detail = "Insert Statement",
                    FilterText = insertStatement.Sql,
                    Range = new NewRangeObject
                    {
                        StartLineNumber = insertStatement.StartLocation.LineNumber - 1,
                        EndLineNumber = insertStatement.EndLocation.LineNumber - 1,
                        StartColumn = insertStatement.StartLocation.ColumnNumber - 1,
                        EndColumn = insertStatement.EndLocation.ColumnNumber - 1
                    },
                    TextEdit = new TextEdit
                    {
                        NewText = insertStatementText,
                        Range = new Workspace.Contracts.Range
                        {
                            Start = new Workspace.Contracts.Position
                            {
                                Line = insertStatement.StartLocation.LineNumber - 1,
                                Character = insertStatement.StartLocation.ColumnNumber - 1
                            },
                            End = new Workspace.Contracts.Position
                            {
                                Line = insertStatement.EndLocation.LineNumber - 1,
                                Character = insertStatement.EndLocation.ColumnNumber - 1
                            }
                        }
                    }
                };

                return [completionItem];
            }

            SqlCodeObject currentNode = insertStatement;

            while (currentNode != null)
            {
                if (currentNode is SqlRowConstructorExpression)
                {
                    break;
                }
                if (currentNode.Children.Count() == 0)
                {
                    break;
                }

                var isChildInScope = false;
                foreach (var child in currentNode.Children)
                {
                    if (IsCursorInNodeScope(child, scriptDocumentInfo))
                    {
                        currentNode = child;
                        isChildInScope = true;
                        continue;
                    }
                }
                if (!isChildInScope)
                {
                    break;
                }
            }

            SqlRowConstructorExpression? rowExpression = currentNode as SqlRowConstructorExpression;

            if (rowExpression == null)
            {
                return null;
            }

            StringBuilder rowValueTextBuilder = new StringBuilder();

            rowValueTextBuilder.Append(",");
            rowValueTextBuilder.Append(Environment.NewLine);
            rowValueTextBuilder.Append("(");

            for (int i = 0; i < boundTable.Columns.Count; i++)
            {
                var column = boundTable.Columns[i];
                if (column.ComputedColumnInfo != null)
                {
                    continue;
                }
                if (column.DefaultValue != null)
                {
                    rowValueTextBuilder.Append("DEFAULT, ");
                }
                else
                {
                    if (column.Nullable)
                    {
                        rowValueTextBuilder.Append("NULL, ");
                    }
                    else
                    {
                        rowValueTextBuilder.Append("DEFAULT, ");
                    }
                }
            }
            rowValueTextBuilder.Remove(rowValueTextBuilder.Length - 2, 2); // Remove the last comma and space

            rowValueTextBuilder.Append(')');
            string rowValueText = rowValueTextBuilder.ToString();

            var rowcompletionItem = new CompletionItem
            {
                Label = rowValueText,
                Kind = CompletionItemKind.Text,
                InsertText = rowValueText,
                Detail = "Insert new row",
                FilterText = rowExpression.Sql + (scriptDocumentInfo.Token.Text == "," ? "," : ""),
                Range = new NewRangeObject
                {
                    StartLineNumber = rowExpression.StartLocation.LineNumber - 1,
                    EndLineNumber = rowExpression.EndLocation.LineNumber - 1,
                    StartColumn = rowExpression.StartLocation.ColumnNumber - 1,
                    EndColumn = rowExpression.EndLocation.ColumnNumber - 1
                },
                TextEdit = new TextEdit
                {
                    NewText = rowValueText,
                    Range = new Workspace.Contracts.Range
                    {
                        Start = new Workspace.Contracts.Position
                        {
                            Line = rowExpression.EndLocation.LineNumber - 1,
                            Character = rowExpression.EndLocation.ColumnNumber - 1
                        },
                        End = new Workspace.Contracts.Position
                        {
                            Line = rowExpression.EndLocation.LineNumber - 1,
                            Character = rowExpression.EndLocation.ColumnNumber - 1
                        }
                    }
                }
            };

            return new[] { rowcompletionItem };
        }
    }
}