//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ServiceHost
{
    public class ThreadSynchronizationContextTests
    {
        [Test]
        public void PostAfterEndLoopDoesNotThrow()
        {
            var context = new ThreadSynchronizationContext();
            var callbackInvoked = false;

            context.EndLoop();

            Assert.DoesNotThrow(() => context.Post(_ => callbackInvoked = true, null));
            Assert.IsFalse(callbackInvoked);
        }
    }
}