//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
    [Export(typeof(ICompletionExtensionProvider))]
    public class CompletionExtProvider1 : ICompletionExtensionProvider
    {
        Task<ICompletionExtension> ICompletionExtensionProvider.CreateAsync(IReadOnlyDictionary<string, object> properties, CancellationToken cancellationToken)
        {
            return Task.FromResult<ICompletionExtension>(new CompletionExt1(properties));
        }
    }

    public class CompletionExt1 : ICompletionExtension
    {
        public string Name => "CompletionExt1";

        private readonly string _modelPath;

        public CompletionExt1(IReadOnlyDictionary<string, object> properties)
        {
            _modelPath = (string)properties["modelPath"];
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
            await Run(completions, token);
            return completions;
        }

        async Task ICompletionExtension.Initialize(CancellationToken token)
        {
            await LoadModel(token).ConfigureAwait(false);
            return;
        }

        private async Task LoadModel(CancellationToken token)
        {
            //loading model logic here
            await Task.Delay(2000).ConfigureAwait(false); //for testing
            token.ThrowIfCancellationRequested();
            Console.WriteLine("Model loaded from: " + _modelPath);
        }

        private async Task Run(CompletionItem[] completions, CancellationToken token)
        {
            Console.WriteLine("Enter ExecuteAsync");

            var sortedItems = completions.OrderBy(item => item.SortText);
            sortedItems.First().Preselect = true;
            foreach(var item in sortedItems)
            {
                item.Command = new Command
                {
                    command = "vsintellicode.completionItemSelected",
                    arguments = new object[] { new Dictionary<string, string> { { "IsCommit", "True" } } }
                };
            }

            //code to augment the default completion list
            await Task.Delay(20); // for testing
            token.ThrowIfCancellationRequested();
            Console.WriteLine("Exit ExecuteAsync");
        }
    }
}
