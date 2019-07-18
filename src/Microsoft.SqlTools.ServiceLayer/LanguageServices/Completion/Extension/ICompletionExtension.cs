//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion.Extension
{
    public interface ICompletionExtension : IDisposable
    {
        /// <summary>
        /// Unique name for the extension
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Completion extension initialization
        /// </summary>
        /// <param name="properties">Parameters needed by the extension</param>
        /// <param name="cancelToken">Cancellation token for cancel the initialization</param>
        /// <returns></returns>
        Task Initialize(IReadOnlyDictionary<string, object> properties, CancellationToken token);

        /// <summary>
        /// Implement the completion extension logic
        /// </summary>
        /// <param name="connInfo">Connection information for the completion session</param>
        /// <param name="scriptDocumentInfo">Script parsing information</param>
        /// <param name="completions">Current completion list</param>
        /// <param name="cancelToken">Cancellation token for cancel the completion extension logic</param>
        /// <returns></returns>
        Task<CompletionItem[]> HandleCompletionAsync(ConnectionInfo connInfo, ScriptDocumentInfo scriptDocumentInfo, CompletionItem[] completions, CancellationToken cancelToken);
    }
}
