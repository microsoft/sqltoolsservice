//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>SPEC §6.2/§8.3 (R020): terminal/lifecycle/config envelopes are flush checkpoints.</summary>
    public sealed class DurabilityPolicyTests
    {
        [Theory]
        [InlineData(EnvelopeKinds.RpcOutResult, "v2/connection.open")]
        [InlineData(EnvelopeKinds.RpcOutError, "v2/query.execute")]
        [InlineData(EnvelopeKinds.Diagnostic, "core.unexpectedInput")]
        [InlineData(EnvelopeKinds.Control, "lifecycle.shutdown")]
        [InlineData(EnvelopeKinds.ConfigChanged, "capture")]
        [InlineData(EnvelopeKinds.RpcOutNotify, "v2/query.complete")] // the terminal notification (R020)
        [InlineData(EnvelopeKinds.RpcOutNotify, "v2/fatal")]
        public void CheckpointKindsForceFlush(string kind, string type)
        {
            Assert.True(DurabilityPolicy.IsCheckpoint(kind, type));
        }

        [Theory]
        [InlineData(EnvelopeKinds.RpcOutNotify, "v2/query.rows")]      // streamed, not terminal
        [InlineData(EnvelopeKinds.RpcOutNotify, "v2/query.resultSet")]
        [InlineData(EnvelopeKinds.RpcInRequest, "v2/query.execute")]
        [InlineData(EnvelopeKinds.EffectRequest, "driver.queryStart")]
        [InlineData(EnvelopeKinds.EffectResponse, "driver.queryEvent")]
        [InlineData(EnvelopeKinds.Metric, "sts2.snapshot")]
        public void NonTerminalKindsDoNotForceFlush(string kind, string type)
        {
            Assert.False(DurabilityPolicy.IsCheckpoint(kind, type));
        }
    }
}
