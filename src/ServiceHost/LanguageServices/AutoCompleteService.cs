//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.WorkspaceServices.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Main class for Autocomplete functionality
    /// </summary>
    public class AutoCompleteService
    {
        #region Singleton Instance Implementation

        /// <summary>
        /// Singleton service instance
        /// </summary>
        private static Lazy<AutoCompleteService> instance 
            = new Lazy<AutoCompleteService>(() => new AutoCompleteService());

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
        /// Default, parameterless constructor.
        /// TODO: Figure out how to make this truely singleton even with dependency injection for tests
        /// </summary>
        public AutoCompleteService()
        { 
        }

        #endregion

        /// <summary>
        /// Gets the current autocomplete candidate list
        /// </summary>
        public IEnumerable<string> AutoCompleteList { get; private set; }

        public void InitializeService(ServiceHost serviceHost)
        {
            // Register a callback for when a connection is created
            ConnectionService.Instance.RegisterOnConnectionTask(UpdateAutoCompleteCache);
        }

        /// <summary>
        /// Update the cached autocomplete candidate list when the user connects to a database
        /// </summary>
        /// <param name="connection"></param>
        public async Task UpdateAutoCompleteCache(ISqlConnection connection)
        {
            AutoCompleteList = connection.GetServerObjects();
            await Task.FromResult(0);
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
