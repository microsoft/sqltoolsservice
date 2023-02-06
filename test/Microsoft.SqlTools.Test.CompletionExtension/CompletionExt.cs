﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion.Extension;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;

namespace Microsoft.SqlTools.Test.CompletionExtension
{

    [Export(typeof(ICompletionExtension))]
    public class CompletionExt : ICompletionExtension
    {
        public string Name => "CompletionExt";

        private string modelPath;

        public CompletionExt()
        {
        }

        void IDisposable.Dispose()
        {
        }

        async Task<CompletionItem[]> ICompletionExtension.HandleCompletionAsync(ConnectionInfo connInfo, ScriptDocumentInfo scriptDocumentInfo, CompletionItem[] completions, CancellationToken token)
        {
            if (completions == null || completions == null || completions.Length == 0)
            {
                return completions;
            }
            return await Run(completions, token);
        }

        async Task ICompletionExtension.Initialize(IReadOnlyDictionary<string, object> properties, CancellationToken token)
        {
            modelPath = (string)properties["modelPath"];
            await LoadModel(token).ConfigureAwait(false);
            return;
        }

        private async Task LoadModel(CancellationToken token)
        {
            //loading model logic here
            await Task.Delay(2000).ConfigureAwait(false); //for testing
            token.ThrowIfCancellationRequested();
            Console.WriteLine("Model loaded from: " + modelPath);
        }

        private async Task<CompletionItem[]> Run(CompletionItem[] completions, CancellationToken token)
        {
            Console.WriteLine("Enter ExecuteAsync");

            var sortedItems = completions.OrderBy(item => item.SortText);
            sortedItems.First().Preselect = true;
            foreach(var item in sortedItems)
            {
                item.Command = new Command
                {
                    command = "vsintellicode.completionItemSelected",
                    Arguments = new object[] { new Dictionary<string, string> { { "IsCommit", "True" } } }
                };
            }

            //code to augment the default completion list
            await Task.Delay(20); // for testing
            token.ThrowIfCancellationRequested();
            Console.WriteLine("Exit ExecuteAsync");
            return sortedItems.ToArray();
        }
    }
}
