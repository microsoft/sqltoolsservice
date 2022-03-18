﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Diagnostics;
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
            Stopwatch.Start();
        }

        private Stopwatch Stopwatch { get; set; }

        /// <summary>
        /// Completes the results to calculate the duration
        /// </summary>
        public void CompleteResult(CompletionItem[] completionItems)
        {
            Stopwatch.Stop();
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
        /// Completion list
        /// </summary>
        public CompletionItem[] CompletionItems { get; private set; }
    }
}
