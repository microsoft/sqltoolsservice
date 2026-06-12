//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
using System.Text.Json;
using Microsoft.SqlTools.Sts2.Core;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Core
{
    /// <summary>SPEC §9.2: the reducer is pure, total, and deterministic.</summary>
    public class CoreReducerTests
    {
        private static CoreEnvelope Request(long seq, string type, string corr, string payloadJson = "{}") => new()
        {
            Seq = seq,
            Kind = "rpc.in.request",
            Type = type,
            Corr = corr,
            Payload = JsonDocument.Parse(payloadJson).RootElement.Clone(),
        };

        [Fact]
        public void ToyEchoProducesResultAndIncrementsCounter()
        {
            CoreDecision decision = Sts2CoreReducer.Decide(
                CoreState.Initial, Request(1, "v2/toy.echo", "r-1", """{"text":"hi"}"""));

            Assert.Equal(1, decision.NewState.ToyCounter);
            Assert.Equal(1, decision.NewState.LastSeq);
            RpcResultOutput result = Assert.IsType<RpcResultOutput>(Assert.Single(decision.Outputs));
            Assert.Equal("r-1", result.Corr);
            Assert.Equal("hi", result.Result.GetProperty("echo").GetString());
            Assert.Equal(1, result.Result.GetProperty("counter").GetInt32());
        }

        [Fact]
        public void UnknownMethodYieldsStableErrorNotException()
        {
            CoreDecision decision = Sts2CoreReducer.Decide(
                CoreState.Initial, Request(1, "v2/toy.nope", "r-9"));

            RpcErrorOutput error = Assert.IsType<RpcErrorOutput>(Assert.Single(decision.Outputs));
            Assert.Equal("Sts2.InvalidRequest", error.DataCode);
            Assert.Equal("r-9", error.Corr);
            Assert.Equal(0, decision.NewState.ToyCounter);
        }

        [Fact]
        public void ToyEffectRoundTripsThroughPendingMap()
        {
            CoreDecision first = Sts2CoreReducer.Decide(
                CoreState.Initial, Request(5, "v2/toy.effect", "r-2", """{"value":42}"""));

            EffectRequestOutput effect = Assert.IsType<EffectRequestOutput>(Assert.Single(first.Outputs));
            Assert.Equal("eff-5", effect.EffectId); // deterministic: derived from seq
            Assert.Equal("toy.delay", effect.EffectName);
            Assert.Equal("r-2", first.NewState.PendingToyEffects["eff-5"]);

            CoreDecision second = Sts2CoreReducer.Decide(first.NewState, new CoreEnvelope
            {
                Seq = 6,
                Kind = "effect.res",
                Type = "toy.delay",
                Corr = "eff-5",
                Payload = JsonDocument.Parse("""{"value":42}""").RootElement.Clone(),
            });

            RpcResultOutput result = Assert.IsType<RpcResultOutput>(Assert.Single(second.Outputs));
            Assert.Equal("r-2", result.Corr);
            Assert.Equal("eff-5", result.Result.GetProperty("effectId").GetString());
            Assert.Empty(second.NewState.PendingToyEffects);
        }

        [Fact]
        public void LifecycleControlSetsShuttingDown()
        {
            CoreDecision decision = Sts2CoreReducer.Decide(CoreState.Initial, new CoreEnvelope
            {
                Seq = 1,
                Kind = "control",
                Type = "lifecycle.shutdown",
            });
            Assert.True(decision.NewState.ShuttingDown);
            Assert.Empty(decision.Outputs);
        }

        [Theory]
        [InlineData("metric", "whatever")]
        [InlineData("effect.res", "toy.delay")] // unknown effect id
        [InlineData("control", "lifecycle.bogus")]
        public void GarbageInputBecomesDiagnosticNeverThrows(string kind, string type)
        {
            CoreDecision decision = Sts2CoreReducer.Decide(CoreState.Initial, new CoreEnvelope
            {
                Seq = 1,
                Kind = kind,
                Type = type,
                Corr = "x",
            });
            DiagnosticOutput diag = Assert.IsType<DiagnosticOutput>(Assert.Single(decision.Outputs));
            Assert.Equal("core.unexpectedInput", diag.Name);
        }

        [Fact]
        public void DecideIsDeterministic()
        {
            CoreEnvelope envelope = Request(7, "v2/toy.echo", "r-7", """{"text":"same"}""");
            CoreDecision a = Sts2CoreReducer.Decide(CoreState.Initial, envelope);
            CoreDecision b = Sts2CoreReducer.Decide(CoreState.Initial, envelope);

            Assert.Equal(a.NewState, b.NewState);
            string ja = ((RpcResultOutput)a.Outputs.Single()).Result.GetRawText();
            string jb = ((RpcResultOutput)b.Outputs.Single()).Result.GetRawText();
            Assert.Equal(ja, jb);
        }
    }
}
