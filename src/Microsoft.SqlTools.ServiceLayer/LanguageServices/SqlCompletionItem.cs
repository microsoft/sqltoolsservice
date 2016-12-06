//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Creates a completion item from SQL parser declaration item
    /// </summary>
    public class SqlCompletionItem
    {
        private static Regex ValidSqlNameRegex = new Regex(@"^[\p{L}_@][\p{L}\p{N}@$#_]{0,127}$");

        /// <summary>
        /// Create new instance given the SQL parser declaration
        /// </summary>
        public SqlCompletionItem(Declaration declaration, string tokenText) :
            this(declaration == null ? null : declaration.Title, declaration == null ? DeclarationType.Table : declaration.Type, tokenText)
        {
        }

        /// <summary>
        /// Creates new instance given declaration title and type
        /// </summary>
        public SqlCompletionItem(string declarationTitle, DeclarationType declarationType, string tokenText)
        {
            Validate.IsNotNullOrEmptyString("declarationTitle", declarationTitle);

            DeclarationTitle = declarationTitle;
            DeclarationType = declarationType;
            TokenText = tokenText;

            Init();
        }

        private void Init()
        {
            InsertText = GetCompletionItemInsertName();
            Label = DeclarationTitle;
            if (StartsWithBracket(TokenText))
            {
                Label = WithBracket(Label);
                InsertText = WithBracket(InsertText);
            }
            Detail = Label;
            Kind = CreateCompletionItemKind();
        }

        private CompletionItemKind CreateCompletionItemKind()
        {
            CompletionItemKind kind = CompletionItemKind.Variable;
            switch (DeclarationType)
            {
                case DeclarationType.Schema:
                    kind = CompletionItemKind.Module;
                    break;
                case DeclarationType.Column:
                    kind = CompletionItemKind.Field;
                    break;
                case DeclarationType.Table:
                case DeclarationType.View:
                    kind = CompletionItemKind.File;
                    break;
                case DeclarationType.Database:
                    kind = CompletionItemKind.Method;
                    break;
                case DeclarationType.ScalarValuedFunction:
                case DeclarationType.TableValuedFunction:
                case DeclarationType.BuiltInFunction:
                    kind = CompletionItemKind.Value;
                    break;
                default:
                    kind = CompletionItemKind.Unit;
                    break;
            }

            return kind;
        }

        /// <summary>
        /// Declaration Title
        /// </summary>
        public string DeclarationTitle { get; private set; }

        /// <summary>
        /// Token text from the editor
        /// </summary>
        public string TokenText { get; private set; }

        /// <summary>
        /// SQL declaration type
        /// </summary>
        public DeclarationType DeclarationType { get; private set; }

        /// <summary>
        /// Completion item label
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Completion item kind
        /// </summary>
        public CompletionItemKind Kind { get; private set; }

        /// <summary>
        /// Completion insert text
        /// </summary>
        public string InsertText { get; private set; }

        /// <summary>
        /// Completion item detail
        /// </summary>
        public string Detail { get; private set; }

        /// <summary>
        /// Creates a completion item given the editor info
        /// </summary>
        public CompletionItem CreateCompletionItem(
          int row,
          int startColumn,
          int endColumn)
        {
            return CreateCompletionItem(Label, Detail, InsertText, Kind, row, startColumn, endColumn);
        }

        /// <summary>
        /// Creates a completion item
        /// </summary>
        public static CompletionItem CreateCompletionItem(
           string label,
           string detail,
           string insertText,
           CompletionItemKind kind,
           int row,
           int startColumn,
           int endColumn)
        {
            CompletionItem item = new CompletionItem()
            {
                Label = label,
                Kind = kind,
                Detail = detail,
                InsertText = insertText,
                TextEdit = new TextEdit
                {
                    NewText = insertText,
                    Range = new Range
                    {
                        Start = new Position
                        {
                            Line = row,
                            Character = startColumn
                        },
                        End = new Position
                        {
                            Line = row,
                            Character = endColumn
                        }
                    }
                }
            };

            return item;
        }

        private string GetCompletionItemInsertName()
        {
            string insertText = DeclarationTitle;
            if (!string.IsNullOrEmpty(DeclarationTitle) && !ValidSqlNameRegex.IsMatch(DeclarationTitle))
            {
                insertText = WithBracket(DeclarationTitle);
            }
            return insertText;
        }

        private bool HasBrackets(string text)
        {
            return text != null && text.StartsWith("[") && text.EndsWith("]");
        }

        private bool StartsWithBracket(string text)
        {
            return text != null && text.StartsWith("[");
        }

        private string WithBracket(string text)
        {
            if (!HasBrackets(text))
            {
                return string.Format(CultureInfo.InvariantCulture, "[{0}]", text);
            }
            else
            {
                return text;
            }
        }
    }
}
