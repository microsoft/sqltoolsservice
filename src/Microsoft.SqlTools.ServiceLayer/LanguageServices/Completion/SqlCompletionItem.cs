//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion
{
    /// <summary>
    /// Creates a completion item from SQL parser declaration item
    /// </summary>
    public class SqlCompletionItem
    {
        private static Regex ValidSqlNameRegex = new Regex(@"^[\p{L}_@#][\p{L}\p{N}@$#_]{0,127}$");
        private static DelimitedIdentifier BracketedIdentifiers = new DelimitedIdentifier { Start = "[", End = "]" };
        private static DelimitedIdentifier FunctionPostfix = new DelimitedIdentifier { Start = "", End = "()" };
        private static DelimitedIdentifier[] DelimitedIdentifiers =
            new DelimitedIdentifier[] { BracketedIdentifiers, new DelimitedIdentifier { Start = "\"", End = "\"" } };

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
            InsertText = DeclarationTitle;
            Label = DeclarationTitle;
            DelimitedIdentifier delimitedIdentifier = GetDelimitedIdentifier(TokenText);

            // If we're not already going to quote this then handle special cases for various
            // DeclarationTypes
            if (delimitedIdentifier == null && !string.IsNullOrEmpty(DeclarationTitle))
            {
                switch (this.DeclarationType)
                {
                    case DeclarationType.Server:
                    case DeclarationType.Database:
                    case DeclarationType.Table:
                    case DeclarationType.Column:
                    case DeclarationType.View:
                    case DeclarationType.Schema:
                        // Only quote if we need to - i.e. if this isn't a valid name (has characters that need escaping such as [) 
                        // or if it's a reserved word
                        if (!ValidSqlNameRegex.IsMatch(DeclarationTitle) || AutoCompleteHelper.IsReservedWord(InsertText)) {
                            InsertText = WithDelimitedIdentifier(BracketedIdentifiers, DeclarationTitle);
                        }
                        break;
                    case DeclarationType.BuiltInFunction:
                    case DeclarationType.ScalarValuedFunction:
                    case DeclarationType.TableValuedFunction:
                        // Functions we add on the () at the end since they'll always have them
                        InsertText = WithDelimitedIdentifier(FunctionPostfix, DeclarationTitle);
                        break;
                }
            }

            // If the user typed a token that starts with a delimiter then always quote both
            // the display label and text to be inserted
            if (delimitedIdentifier != null)
            {
                Label = WithDelimitedIdentifier(delimitedIdentifier, Label);
                InsertText = WithDelimitedIdentifier(delimitedIdentifier, InsertText);
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

        private bool HasDelimitedIdentifier(DelimitedIdentifier delimiteIidentifier, string text)
        {
            return text != null && delimiteIidentifier != null && text.StartsWith(delimiteIidentifier.Start) 
                && text.EndsWith(delimiteIidentifier.End);
        }

        private DelimitedIdentifier GetDelimitedIdentifier(string text)
        {
            return text != null ? DelimitedIdentifiers.FirstOrDefault(x => text.StartsWith(x.Start)) : null;
        }

        private string WithDelimitedIdentifier(DelimitedIdentifier delimitedIdentifier, string text)
        {
            if (!HasDelimitedIdentifier(delimitedIdentifier, text))
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", delimitedIdentifier.Start, text, delimitedIdentifier.End);
            }
            else
            {
                return text;
            }
        }
    }

    internal class DelimitedIdentifier
    {
        public string Start { get; set; }
        public string End { get; set; }
    }
}
