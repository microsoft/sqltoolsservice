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
    }
}
