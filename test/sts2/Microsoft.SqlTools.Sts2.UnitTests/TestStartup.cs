//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.SqlTools.Sts2.UnitTests
{
    internal static class TestStartup
    {
        /// <summary>
        /// The scenario runner and 200-seed simulator each spin up many short-lived
        /// background coordinator and query-pump tasks. The default ThreadPool grows by
        /// ~1 thread per 500ms, so bursts queue behind that ramp and continuations that
        /// complete a request's terminal can be delayed past test timeouts — a liveness
        /// artifact, not a determinism bug (journals still replay identically, I7). The
        /// production service runs one session per process and never sees this. Raising
        /// the floor removes the ramp delay for the test host only.
        /// </summary>
        [ModuleInitializer]
        internal static void Initialize()
        {
            ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);
            ThreadPool.SetMinThreads(System.Math.Max(workerThreads, 64), System.Math.Max(completionPortThreads, 64));
        }
    }
}
