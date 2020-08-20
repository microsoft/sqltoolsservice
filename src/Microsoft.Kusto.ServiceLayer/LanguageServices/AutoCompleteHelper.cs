//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using Range = Microsoft.Kusto.ServiceLayer.Workspace.Contracts.Range;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices
{
    /// <summary>
    /// All the conversion of intellisense info to vscode format is done in this class.
    /// </summary>
    [Export(typeof(IAutoCompleteHelper))]
    public class AutoCompleteHelper : IAutoCompleteHelper
    {
        /// <summary>
        /// Create a completion item from the default item text since VS Code expects CompletionItems
        /// </summary>
        /// <param name="label"></param>
        /// <param name="detail"></param>
        /// <param name="insertText"></param>
        /// <param name="kind"></param>
        /// <param name="row"></param>
        /// <param name="startColumn"></param>
        /// <param name="endColumn"></param>
        public CompletionItem CreateCompletionItem(
            string label,
           string detail,
           string insertText,
           CompletionItemKind kind,
            int row,
            int startColumn,
            int endColumn)
        {
            CompletionItem item = new CompletionItem
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

        /// <summary>
        /// Converts QuickInfo object into a VS Code Hover object
        /// </summary>
        /// <param name="quickInfo"></param>
        /// <param name="language"></param>
        /// <param name="row"></param>
        /// <param name="startColumn"></param>
        /// <param name="endColumn"></param>
        public Hover ConvertQuickInfoToHover(
            string quickInfoText,
            string language,
            int row,
            int startColumn,
            int endColumn)
        {
            // convert from the parser format to the VS Code wire format
            var markedStrings = new MarkedString[1];
            if (quickInfoText != null)
            {
                markedStrings[0] = new MarkedString()
                {
                    Language = language,
                    Value = quickInfoText
                };

                return new Hover()
                {
                    Contents = markedStrings,
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
                };
            }
            else
            {
                return null;
            }
        }
    }
}
