//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System.Threading;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Completion;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Xunit;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Completion
{
    public class AutoCompletionResultTest
    {
        [Fact]
        public void CompletionShouldRecordDuration()
        {
            AutoCompletionResult result = new AutoCompletionResult();
            int duration = 200;
            Thread.Sleep(duration);
            result.CompleteResult(new CompletionItem[] { });
            Assert.True(result.Duration >= duration);
        }
    }
}
