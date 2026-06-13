//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Replay;

namespace Microsoft.SqlTools.Sts2.Testing
{
    /// <summary>
    /// Post-run invariant checks (SPEC §14.3) over a produced journal. Returns
    /// violations as human-readable strings; empty means green.
    /// </summary>
    public static class InvariantChecker
    {
        /// <summary>Checks the requested invariants; unknown ids are violations (never silently skipped).</summary>
        public static IReadOnlyList<string> Check(
            IReadOnlyList<string> invariants,
            string journalDirectory,
            IReadOnlyDictionary<string, int>? terminalsByCorr = null,
            int leakedSessions = 0)
        {
            List<Sts2Envelope> journal = JournalReader.ReadAll(journalDirectory).ToList();
            var violations = new List<string>();

            foreach (string invariant in invariants)
            {
                switch (invariant)
                {
                    case "I1": // request terminality
                        if (terminalsByCorr is null)
                        {
                            violations.Add("I1: runner did not supply terminal tracking");
                            break;
                        }
                        foreach ((string corr, int count) in terminalsByCorr.Where(kv => kv.Value != 1))
                        {
                            violations.Add($"I1: request {corr} received {count} terminal responses (expected exactly 1)");
                        }
                        break;

                    case "I2": // query terminality: every accepted, non-disposed query completes exactly once
                    {
                        Dictionary<string, long?> acceptedQueries = AcceptedQueries(journal);
                        foreach ((string queryId, long? disposedAtSeq) in acceptedQueries)
                        {
                            int completes = journal.Count(e => e.Kind == EnvelopeKinds.RpcOutNotify
                                && e.Type == "v2/query.complete"
                                && GetQueryId(e) == queryId);
                            bool disposedBeforeComplete = disposedAtSeq is not null && completes == 0;
                            if (!disposedBeforeComplete && completes != 1)
                            {
                                violations.Add($"I2: query {queryId} emitted {completes} query.complete notifications (expected exactly 1)");
                            }
                        }
                        break;
                    }

                    case "I3": // no output after complete
                    {
                        var completeSeqByQuery = journal
                            .Where(e => e.Kind == EnvelopeKinds.RpcOutNotify && e.Type == "v2/query.complete")
                            .GroupBy(GetQueryId)
                            .ToDictionary(g => g.Key!, g => g.Min(e => e.Seq));
                        foreach (Sts2Envelope envelope in journal.Where(e => e.Kind == EnvelopeKinds.RpcOutNotify
                            && e.Type is "v2/query.rows" or "v2/query.resultSet" or "v2/query.message"))
                        {
                            string? queryId = GetQueryId(envelope);
                            if (queryId is not null && completeSeqByQuery.TryGetValue(queryId, out long completeSeq)
                                && envelope.Seq > completeSeq)
                            {
                                violations.Add($"I3: {envelope.Type} at seq {envelope.Seq} follows query.complete (seq {completeSeq}) for {queryId}");
                            }
                        }
                        break;
                    }

                    case "I9": // backpressure: unacked pages never exceed the window
                    {
                        var sent = new Dictionary<string, int>();
                        var acked = new Dictionary<string, int>();
                        foreach (Sts2Envelope envelope in journal)
                        {
                            if (envelope.Kind == EnvelopeKinds.RpcOutNotify && envelope.Type == "v2/query.rows"
                                && GetQueryId(envelope) is string sentQuery)
                            {
                                sent[sentQuery] = sent.GetValueOrDefault(sentQuery) + 1;
                                int unacked = sent[sentQuery] - acked.GetValueOrDefault(sentQuery);
                                if (unacked > Contracts.Sts2Defaults.WindowPages)
                                {
                                    violations.Add($"I9: query {sentQuery} had {unacked} unacked pages at seq {envelope.Seq} (window {Contracts.Sts2Defaults.WindowPages})");
                                }
                            }
                            else if (envelope.Kind == EnvelopeKinds.RpcInNotify && envelope.Type == "v2/query.ack"
                                && GetQueryId(envelope) is string ackQuery)
                            {
                                if (envelope.Payload!.Value.TryGetProperty("throughPageSeq", out JsonElement through)
                                    && through.ValueKind == JsonValueKind.Number)
                                {
                                    acked[ackQuery] = Math.Max(acked.GetValueOrDefault(ackQuery), through.GetInt32() + 1);
                                }
                                else
                                {
                                    acked[ackQuery] = Math.Min(sent.GetValueOrDefault(ackQuery), acked.GetValueOrDefault(ackQuery) + 1);
                                }
                            }
                        }
                        break;
                    }

                    case "RD1": // row digest capture: no row cells in the journal
                        foreach (Sts2Envelope envelope in journal.Where(e =>
                            e.Type is "driver.queryEvent" or "v2/query.rows" && e.Payload is not null))
                        {
                            if (envelope.Payload!.Value.TryGetProperty("rows", out JsonElement rows)
                                && rows.ValueKind == JsonValueKind.Array)
                            {
                                violations.Add($"RD1: seq {envelope.Seq} journaled literal row cells under rowCapture=digest");
                            }
                        }
                        break;

                    case "SD1": // sql digest capture: no SQL text in the journal
                        foreach (Sts2Envelope envelope in journal.Where(e =>
                            e.Type is "v2/query.execute" or "driver.queryStart" && e.Payload is not null))
                        {
                            if (envelope.Payload!.Value.TryGetProperty("sql", out JsonElement sql)
                                && sql.ValueKind == JsonValueKind.String)
                            {
                                violations.Add($"SD1: seq {envelope.Seq} journaled literal SQL text under sqlCapture=digest");
                            }
                        }
                        break;

                    case "I5": // journal causality
                        long expected = 1;
                        var seqs = new HashSet<long>();
                        foreach (Sts2Envelope envelope in journal)
                        {
                            if (envelope.Seq != expected)
                            {
                                violations.Add($"I5: seq gap — expected {expected}, found {envelope.Seq}");
                            }
                            expected = envelope.Seq + 1;
                            seqs.Add(envelope.Seq);
                            if (envelope.Cause is long cause && (cause >= envelope.Seq || !seqs.Contains(cause)))
                            {
                                violations.Add($"I5: envelope {envelope.Seq} has invalid cause {cause}");
                            }
                        }
                        break;

                    case "I6": // secret safety
                        violations.AddRange(SecretCanaries.ScanDirectory(journalDirectory)
                            .Select(hit => "I6: canary found — " + hit));
                        break;

                    case "I7": // replay determinism
                        ReplayResult replay = JournalReplayer.Replay(journal);
                        if (!replay.Identical)
                        {
                            violations.Add($"I7: replay diverged at seq {replay.Divergence!.Seq}: recorded {replay.Divergence.Recorded}; replayed {replay.Divergence.Replayed}; causes {string.Join("<-", replay.Divergence.CauseChain)}");
                        }
                        break;

                    case "I8": // lease cleanup
                        if (leakedSessions != 0)
                        {
                            violations.Add($"I8: {leakedSessions} driver session(s) still open at run end");
                        }
                        break;

                    case "I12": // JSON-RPC error shape
                        foreach (Sts2Envelope envelope in journal.Where(e => e.Kind == EnvelopeKinds.RpcOutError))
                        {
                            JsonElement body = envelope.Payload!.Value;
                            if (!body.TryGetProperty("code", out JsonElement code) || code.ValueKind != JsonValueKind.Number)
                            {
                                violations.Add($"I12: error at seq {envelope.Seq} lacks a numeric code");
                            }
                            if (!body.TryGetProperty("data", out JsonElement data)
                                || !data.TryGetProperty("code", out JsonElement dataCode)
                                || dataCode.ValueKind != JsonValueKind.String
                                || !dataCode.GetString()!.StartsWith("Sts2.", StringComparison.Ordinal))
                            {
                                violations.Add($"I12: error at seq {envelope.Seq} lacks a stable data.code");
                            }
                        }
                        break;

                    default:
                        violations.Add($"unknown invariant '{invariant}' — runner support missing (do not silently skip)");
                        break;
                }
            }
            return violations;
        }

        /// <summary>Accepted queryIds mapped to the seq of their dispose request, if any.</summary>
        private static Dictionary<string, long?> AcceptedQueries(IReadOnlyList<Sts2Envelope> journal)
        {
            var accepted = new Dictionary<string, long?>(StringComparer.Ordinal);
            foreach (Sts2Envelope envelope in journal.Where(e => e.Kind == EnvelopeKinds.RpcOutResult
                && e.Payload is { ValueKind: JsonValueKind.Object } p
                && p.TryGetProperty("queryId", out JsonElement q) && q.ValueKind == JsonValueKind.String
                && p.EnumerateObject().Count() == 1))
            {
                accepted[envelope.Payload!.Value.GetProperty("queryId").GetString()!] = null;
            }
            foreach (Sts2Envelope envelope in journal.Where(e => e.Kind == EnvelopeKinds.RpcInRequest && e.Type == "v2/query.dispose"))
            {
                string? queryId = GetQueryId(envelope);
                if (queryId is not null && accepted.ContainsKey(queryId))
                {
                    accepted[queryId] ??= envelope.Seq;
                }
            }
            return accepted;
        }

        private static string? GetQueryId(Sts2Envelope envelope) =>
            envelope.Payload is { ValueKind: JsonValueKind.Object } p
            && p.TryGetProperty("queryId", out JsonElement q) && q.ValueKind == JsonValueKind.String
                ? q.GetString()
                : null;
    }
}
