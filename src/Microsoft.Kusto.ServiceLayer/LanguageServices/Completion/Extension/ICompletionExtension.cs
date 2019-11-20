//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices.Completion.Extension
{
    public interface ICompletionExtension : IDisposable
    {
        /// <summary>
        /// Unique name for the extension
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Method for initializing the extension, this is called once when the extension is loaded
        /// </summary>
        /// <param name="properties">Parameters needed by the extension</param>
        /// <param name="cancelToken">Cancellation token used to indicate that the initialization should be cancelled</param>
        /// <returns></returns>
        Task Initialize(IReadOnlyDictionary<string, object> properties, CancellationToken token);

        /// <summary>
        /// Handles the completion request, returning the modified CompletionItemList if used
        /// </summary>
        /// <param name="connInfo">Connection information for the completion session</param>
        /// <param name="scriptDocumentInfo">Script parsing information</param>
        /// <param name="completions">Current completion list</param>
        /// <param name="cancelToken">Token used to indicate that the completion request should be cancelled</param>
        /// <returns></returns>
        Task<CompletionItem[]> HandleCompletionAsync(ConnectionInfo connInfo, ScriptDocumentInfo scriptDocumentInfo, CompletionItem[] completions, CancellationToken cancelToken);
    }
}
