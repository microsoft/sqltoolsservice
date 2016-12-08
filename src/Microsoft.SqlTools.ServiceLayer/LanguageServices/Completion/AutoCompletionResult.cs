//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion
{
    /// <summary>
    /// Includes the objects created by auto completion service
    /// </summary>
    public class AutoCompletionResult
    {
        /// <summary>
        /// Creates new instance
        /// </summary>
        public AutoCompletionResult()
        {
            Stopwatch = new Stopwatch();
            StartProcesssing();
        }

        /// <summary>
        /// Starts processing the result
        /// </summary>
        public void StartProcesssing()
        {
            Stopwatch.Start();
        }

        private Stopwatch Stopwatch { get; set; }

        /// <summary>
        /// Completes the results to calculate the duration
        /// </summary>
        public void CompleteResult(IEnumerable<Declaration> suggestions, CompletionItem[] completionItems)
        {
            Stopwatch.Stop();
            Suggestions = suggestions;
            CompletionItems = completionItems;
        }

        /// <summary>
        /// The number of milliseconds to process the result
        /// </summary>
        public double Duration
        {
            get
            {
                return Stopwatch.ElapsedMilliseconds;
            }
        }
        
        /// <summary>
        /// Suggestion list
        /// </summary>
        public IEnumerable<Declaration> Suggestions { get; private set; }

        /// <summary>
        /// Completion list
        /// </summary>
        public CompletionItem[] CompletionItems { get; private set; }
    }
}
