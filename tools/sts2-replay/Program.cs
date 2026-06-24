//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Replay;

namespace Microsoft.SqlTools.Sts2.Replay
{
    /// <summary>
    /// sts2-replay (SPEC §13.1): consumes journals without starting the product host.
    /// Commands: run, verify, until, diff, explain. (export-check arrives with the
    /// export bundle in M6.)
    /// </summary>
    internal static class Program
    {
        internal static int Main(string[] args)
        {
            try
            {
                return args switch
                {
                    ["run", string path] => Run(path),
                    ["verify", string path] => Verify(path),
                    ["until", string path, "--seq", string seq] => Until(path, long.Parse(seq, CultureInfo.InvariantCulture)),
                    ["diff", string path] => Diff(path),
                    ["explain", string path, "--seq", string seq] => Explain(path, long.Parse(seq, CultureInfo.InvariantCulture)),
                    ["export-check", string bundle] => ExportCheck(bundle),
                    _ => Usage(),
                };
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or FormatException)
            {
                Console.Error.WriteLine("sts2-replay: " + ex.Message);
                return 2;
            }
        }

        private static int Usage()
        {
            Console.Error.WriteLine("usage:");
            Console.Error.WriteLine("  sts2-replay run <journal-dir>            replay and print the outbound digest sequence");
            Console.Error.WriteLine("  sts2-replay verify <dir>                 batch-verify every journal under <dir>");
            Console.Error.WriteLine("  sts2-replay until <journal-dir> --seq N  replay to seq N and dump redacted state");
            Console.Error.WriteLine("  sts2-replay diff <journal-dir>           print the first divergence with its cause chain");
            Console.Error.WriteLine("  sts2-replay explain <journal-dir> --seq N  print the causal tree around an envelope");
            Console.Error.WriteLine("  sts2-replay export-check <bundle.zip>     validate manifest hashes, privacy report, replayability");
            return 2;
        }

        private static int Run(string path)
        {
            ReplayResult result = JournalReplayer.Replay(JournalReader.ReadAll(path));
            foreach (string digest in result.OutboundDigests)
            {
                Console.WriteLine(digest);
            }
            if (!result.Identical)
            {
                Console.Error.WriteLine($"DIVERGED at seq {result.Divergence!.Seq}: recorded {result.Divergence.Recorded}; replayed {result.Divergence.Replayed}");
                return 1;
            }
            return 0;
        }

        private static int Verify(string path)
        {
            // A journal = any directory containing journal-*.manifest.json (searched recursively).
            string[] manifests = Directory.Exists(path)
                ? Directory.EnumerateFiles(path, "journal-*.manifest.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray()
                : [path];
            if (manifests.Length == 0)
            {
                Console.Error.WriteLine("sts2-replay verify: no journal manifests under " + path);
                return 2;
            }

            int identical = 0;
            foreach (string manifest in manifests)
            {
                ReplayResult result = JournalReplayer.Replay(JournalReader.ReadAll(manifest));
                if (result.Identical)
                {
                    identical++;
                }
                else
                {
                    Console.Error.WriteLine($"{manifest}: DIVERGED at seq {result.Divergence!.Seq}");
                }
            }
            Console.WriteLine($"replay: {identical}/{manifests.Length} journals identical");
            return identical == manifests.Length ? 0 : 1;
        }

        private static int Until(string path, long seq)
        {
            ReplayResult result = JournalReplayer.Replay(JournalReader.ReadAll(path), untilSeq: seq);
            Console.WriteLine(JournalReplayer.DumpState(result.FinalState, result.LastSeq));
            return result.Identical ? 0 : 1;
        }

        private static int Diff(string path)
        {
            ReplayResult result = JournalReplayer.Replay(JournalReader.ReadAll(path));
            if (result.Identical)
            {
                Console.WriteLine("identical: no divergence");
                return 0;
            }
            ReplayDivergence d = result.Divergence!;
            Console.WriteLine($"first divergence at seq {d.Seq}");
            Console.WriteLine($"  recorded: {d.Recorded}");
            Console.WriteLine($"  replayed: {d.Replayed}");
            Console.WriteLine($"  cause chain: {string.Join(" <- ", d.CauseChain)}");
            return 1;
        }

        private static int ExportCheck(string bundlePath)
        {
            if (!File.Exists(bundlePath))
            {
                Console.Error.WriteLine("sts2-replay export-check: no bundle at " + bundlePath);
                return 2;
            }
            IReadOnlyList<string> problems = Microsoft.SqlTools.Sts2.Runtime.Export.ExportBundleWriter.Check(bundlePath);
            if (problems.Count > 0)
            {
                foreach (string problem in problems)
                {
                    Console.Error.WriteLine("  " + problem);
                }
                Console.Error.WriteLine($"export-check: {problems.Count} problem(s)");
                return 1;
            }
            Console.WriteLine("export-check: bundle valid (hashes + privacy clean)");
            return 0;
        }

        private static int Explain(string path, long seq)
        {
            List<Sts2Envelope> journal = JournalReader.ReadAll(path).ToList();
            Sts2Envelope? target = journal.FirstOrDefault(e => e.Seq == seq);
            if (target is null)
            {
                Console.Error.WriteLine($"sts2-replay explain: no envelope with seq {seq}");
                return 2;
            }

            var causeBySeq = journal.ToDictionary(e => e.Seq, e => e.Cause);
            IReadOnlyList<long> chain = JournalReplayer.CauseChainOf(causeBySeq, seq);
            var bySeq = journal.ToDictionary(e => e.Seq);

            Console.WriteLine("causal chain (newest first):");
            foreach (long s in chain)
            {
                Sts2Envelope e = bySeq[s];
                Console.WriteLine($"  seq {e.Seq}: {e.Kind} {e.Type} corr={e.Corr ?? "-"} digest={e.Digest}");
            }
            Console.WriteLine("direct consequences:");
            foreach (Sts2Envelope e in journal.Where(e => e.Cause == seq))
            {
                Console.WriteLine($"  seq {e.Seq}: {e.Kind} {e.Type} corr={e.Corr ?? "-"}");
            }
            return 0;
        }
    }
}
