﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable


using System.Threading;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Completion
{
    [TestFixture]
    public class AutoCompletionResultTest
    {
        [Test]
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
