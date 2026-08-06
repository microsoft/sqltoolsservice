//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Text.Json;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>SPEC §8.1: the envelope is the universal communication unit.</summary>
    public class EnvelopeTests
    {
        private static Sts2Envelope Sample() => new()
        {
            RunId = "run-20260612-170322-8421",
            Seq = 412,
            Ts = new DateTimeOffset(2026, 6, 12, 17, 3, 22, 118, TimeSpan.Zero),
            Kind = EnvelopeKinds.RpcInRequest,
            SessionId = "c-7",
            Corr = "r-91",
            Cause = 408,
            Type = "v2/query.execute",
            ConfigVersion = 3,
            Digest = "sha256:9f31aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            Payload = JsonDocument.Parse("""{"connectionId":"c-7","sql":"select 1"}""").RootElement.Clone(),
        };

        [Fact]
        public void RoundTripsThroughJsonLine()
        {
            string line = EnvelopeJsonCodec.SerializeLine(Sample());
            Sts2Envelope back = EnvelopeJsonCodec.DeserializeLine(line);

            Assert.Equal("sts2.envelope/1", back.Schema);
            Assert.Equal("run-20260612-170322-8421", back.RunId);
            Assert.Equal(412, back.Seq);
            Assert.Equal(Sample().Ts, back.Ts);
            Assert.Equal("rpc.in.request", back.Kind);
            Assert.Equal("c-7", back.SessionId);
            Assert.Equal("r-91", back.Corr);
            Assert.Equal(408, back.Cause);
            Assert.Equal("v2/query.execute", back.Type);
            Assert.Equal(3, back.ConfigVersion);
            Assert.Equal(Sample().Digest, back.Digest);
            Assert.Equal("select 1", back.Payload!.Value.GetProperty("sql").GetString());
            Assert.Null(back.PayloadMeta);
        }

        [Fact]
        public void SerializedLineIsSingleLine()
        {
            string line = EnvelopeJsonCodec.SerializeLine(Sample());
            Assert.DoesNotContain('\n', line);
            Assert.DoesNotContain('\r', line);
        }

        [Fact]
        public void TimestampUsesRoundTripInvariantFormat()
        {
            string line = EnvelopeJsonCodec.SerializeLine(Sample());
            Assert.Contains("\"ts\":\"2026-06-12T17:03:22.1180000Z\"", line);
        }

        [Fact]
        public void FieldOrderMatchesSpecExample()
        {
            string line = EnvelopeJsonCodec.SerializeLine(Sample());
            string[] expectedOrder = ["schema", "runId", "seq", "ts", "kind", "sessionId", "corr", "cause", "type", "configVersion", "digest", "payload", "payloadMeta"];
            int last = -1;
            foreach (string field in expectedOrder)
            {
                int index = line.IndexOf("\"" + field + "\":", StringComparison.Ordinal);
                Assert.True(index > last, $"field '{field}' missing or out of order in: {line}");
                last = index;
            }
        }

        [Fact]
        public void NullCauseRoundTripsForRootEnvelopes()
        {
            Sts2Envelope root = Sample() with { Cause = null, Kind = EnvelopeKinds.Control, Type = "lifecycle.start" };
            Sts2Envelope back = EnvelopeJsonCodec.DeserializeLine(EnvelopeJsonCodec.SerializeLine(root));
            Assert.Null(back.Cause);
        }

        [Fact]
        public void KindSetMatchesSpec()
        {
            string[] specKinds =
            [
                "rpc.in.request", "rpc.in.notify", "rpc.out.result", "rpc.out.error", "rpc.out.notify",
                "cmd", "evt", "effect.req", "effect.res", "timer.due", "config.changed",
                "state.snapshot", "metric", "diag", "control",
            ];
            Assert.Equal(specKinds.OrderBy(k => k, StringComparer.Ordinal), EnvelopeKinds.All.OrderBy(k => k, StringComparer.Ordinal));
            Assert.All(specKinds, k => Assert.True(EnvelopeKinds.IsValid(k)));
            Assert.False(EnvelopeKinds.IsValid("rpc.bogus"));
        }

        [Fact]
        public void DeserializeRejectsUnknownSchema()
        {
            string line = EnvelopeJsonCodec.SerializeLine(Sample()).Replace("sts2.envelope/1", "sts2.envelope/9");
            Assert.Throws<System.IO.InvalidDataException>(() => EnvelopeJsonCodec.DeserializeLine(line));
        }
    }
}
