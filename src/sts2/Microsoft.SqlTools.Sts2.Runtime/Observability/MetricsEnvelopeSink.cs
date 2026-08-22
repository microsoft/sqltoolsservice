//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;

namespace Microsoft.SqlTools.Sts2.Runtime.Observability
{
    /// <summary>
    /// The metrics channel (SPEC §12.3): tallies the envelope stream by kind and counts
    /// outbound errors by code, feeding both the process-wide <see cref="Sts2EventSource"/>
    /// and the in-memory snapshot that health and <c>metric</c> envelopes report. Counting
    /// is lock-free and never blocks the pump.
    /// </summary>
    public sealed class MetricsEnvelopeSink : IEnvelopeSink
    {
        private readonly Lock gate = new();
        private readonly Dictionary<string, long> byKind = new(StringComparer.Ordinal);
        private readonly Dictionary<string, long> errorsByCode = new(StringComparer.Ordinal);
        private long total;
        private long errors;

        /// <summary>Total envelopes observed.</summary>
        public long Total => Interlocked.Read(ref total);

        /// <summary>Total outbound rpc.out.error envelopes observed.</summary>
        public long Errors => Interlocked.Read(ref errors);

        /// <inheritdoc/>
        public ValueTask OnEnvelopeAsync(Sts2Envelope envelope, bool flush)
        {
            Interlocked.Increment(ref total);
            Sts2EventSource.Log.EnvelopeObserved();

            bool isError = envelope.Kind == EnvelopeKinds.RpcOutError;
            string? code = isError ? ExtractErrorCode(envelope.Payload) : null;
            lock (gate)
            {
                byKind[envelope.Kind] = byKind.TryGetValue(envelope.Kind, out long k) ? k + 1 : 1;
                if (code is not null)
                {
                    errorsByCode[code] = errorsByCode.TryGetValue(code, out long c) ? c + 1 : 1;
                }
            }
            if (isError)
            {
                Interlocked.Increment(ref errors);
                Sts2EventSource.Log.ErrorObserved();
            }
            return ValueTask.CompletedTask;
        }

        /// <summary>An immutable snapshot of the per-kind envelope counts (deterministic order).</summary>
        public ImmutableSortedDictionary<string, long> EnvelopesByKind()
        {
            lock (gate)
            {
                return byKind.ToImmutableSortedDictionary(StringComparer.Ordinal);
            }
        }

        /// <summary>An immutable snapshot of the outbound error-code histogram (deterministic order).</summary>
        public ImmutableSortedDictionary<string, long> ErrorsByCode()
        {
            lock (gate)
            {
                return errorsByCode.ToImmutableSortedDictionary(StringComparer.Ordinal);
            }
        }

        // The JSON-RPC error body carries the stable string code at data.code (§7.6).
        private static string ExtractErrorCode(JsonElement? payload) =>
            payload is { ValueKind: JsonValueKind.Object } p
            && p.TryGetProperty("data", out JsonElement data)
            && data.ValueKind == JsonValueKind.Object
            && data.TryGetProperty("code", out JsonElement code)
            && code.ValueKind == JsonValueKind.String
                ? code.GetString()!
                : "unknown";
    }
}
