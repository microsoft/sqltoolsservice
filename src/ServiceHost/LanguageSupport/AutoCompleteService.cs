//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.EditorServices.Connection;
using Microsoft.SqlTools.EditorServices.Protocol.LanguageServer;
using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.LanguageSupport
{
    /// <summary>
    /// Main class for Autocomplete functionality
    /// </summary>
    public class AutoCompleteService
    {
        /// <summary>
        /// Singleton service instance
        /// </summary>
        private static Lazy<AutoCompleteService> instance 
            = new Lazy<AutoCompleteService>(() => new AutoCompleteService());

        /// <summary>
        /// The current autocomplete candidate list
        /// </summary>
        private IEnumerable<string> autoCompleteList;

        /// <summary>
        /// Gets the current autocomplete candidate list
        /// </summary>
        public IEnumerable<string> AutoCompleteList
        {
            get
            {
                return this.autoCompleteList;
            }
        }

        /// <summary>
        /// Gets the singleton service instance
        /// </summary>
        public static AutoCompleteService Instance 
        {
            get
            {
                return instance.Value;
            }
        }

        /// <summary>
        /// Update the cached autocomplete candidate list when the user connects to a database
        /// </summary>
        /// <param name="connection"></param>
        public void UpdateAutoCompleteCache(ISqlConnection connection)
        {
            this.autoCompleteList = connection.GetServerObjects();
        }

        /// <summary>
        /// Return the completion item list for the current text position
        /// </summary>
        /// <param name="textDocumentPosition"></param>
        public CompletionItem[] GetCompletionItems(TextDocumentPosition textDocumentPosition)
        {
            var completions = new List<CompletionItem>();

            int i = 0;

            // the completion list will be null is user not connected to server
            if (this.AutoCompleteList != null)
            {
                foreach (var autoCompleteItem in this.AutoCompleteList)
                {
                    // convert the completion item candidates into CompletionItems
                    completions.Add(new CompletionItem()
                    {
                        Label = autoCompleteItem,
                        Kind = CompletionItemKind.Keyword,
                        Detail = autoCompleteItem + " details",
                        Documentation = autoCompleteItem + " documentation",
                        TextEdit = new TextEdit
                        {
                            NewText = autoCompleteItem,
                            Range = new Range
                            {
                                Start = new Position
                                {
                                    Line = textDocumentPosition.Position.Line,
                                    Character = textDocumentPosition.Position.Character
                                },
                                End = new Position
                                {
                                    Line = textDocumentPosition.Position.Line,
                                    Character = textDocumentPosition.Position.Character + 5
                                }
                            }
                        }
                    });

                    // only show 50 items
                    if (++i == 50)
                    {
                        break;
                    }
                }
            }
            return completions.ToArray();
        }
        
    }
}
