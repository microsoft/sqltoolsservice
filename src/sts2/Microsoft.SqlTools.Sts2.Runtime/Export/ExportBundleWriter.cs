//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Replay;

namespace Microsoft.SqlTools.Sts2.Runtime.Export
{
    /// <summary>Inputs for an export bundle.</summary>
    public sealed record ExportBundleRequest
    {
        /// <summary>Run id (names the bundle and matches the journal).</summary>
        public required string RunId { get; init; }

        /// <summary>Journal directory to bundle.</summary>
        public required string JournalDirectory { get; init; }

        /// <summary>Directory the bundle zip is written to.</summary>
        public required string OutputDirectory { get; init; }

        /// <summary>Generated review docs to include (file name -> absolute path); optional.</summary>
        public IReadOnlyDictionary<string, string> GeneratedDocs { get; init; } = new Dictionary<string, string>();

        /// <summary>Whether the caller asked for SQL text (only honored if capture already has it).</summary>
        public bool IncludeSqlText { get; init; }
    }

    /// <summary>Outcome of an export.</summary>
    public sealed record ExportBundleResult
    {
        /// <summary>Absolute path of the written zip.</summary>
        public required string BundlePath { get; init; }

        /// <summary>Bundle size in bytes.</summary>
        public required long Bytes { get; init; }
    }

    /// <summary>
    /// Writes a shareable export bundle (SPEC §8.6): a real zip with a manifest,
    /// privacy report, the journal segments, and generated docs. Export defaults to safe
    /// mode — the journal is copied verbatim (secrets are already tokenized and, in
    /// product capture, SQL/rows are already digests), and the privacy report records the
    /// canary scan result over everything in the bundle.
    /// </summary>
    public static class ExportBundleWriter
    {
        /// <summary>The canary literals scanned for (mirrors Testing.SecretCanaries; duplicated to avoid a test dependency).</summary>
        private static readonly string[] CanaryLiterals = ["CANARY-pw-1f9b3c7d2e", "eyJhbGciOiJIUzI1NiJ9.CANARY-at-5a8d0e.CANARYSIG"];

        /// <summary>Builds the bundle and returns its path and size.</summary>
        public static ExportBundleResult Write(ExportBundleRequest request, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(timeProvider);
            Directory.CreateDirectory(request.OutputDirectory);

            string bundlePath = Path.Combine(request.OutputDirectory, "sts2-export-" + request.RunId + ".zip");
            if (File.Exists(bundlePath))
            {
                File.Delete(bundlePath);
            }

            var canaryHits = new List<string>();
            var segmentEntries = new JsonArray();

            using (var zip = ZipFile.Open(bundlePath, ZipArchiveMode.Create))
            {
                foreach (string segment in Directory.EnumerateFiles(request.JournalDirectory).Order(StringComparer.Ordinal))
                {
                    string fileName = Path.GetFileName(segment);
                    string entryName = "journals/" + fileName;
                    byte[] bytes = File.ReadAllBytes(segment);
                    AddEntry(zip, entryName, bytes);
                    ScanForCanaries(entryName, bytes, canaryHits);
                    if (fileName.EndsWith(".jsonl", StringComparison.Ordinal))
                    {
                        segmentEntries.Add(new JsonObject
                        {
                            ["file"] = entryName,
                            ["bytes"] = bytes.Length,
                            ["sha256"] = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                        });
                    }
                }

                foreach ((string name, string path) in request.GeneratedDocs.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                {
                    if (File.Exists(path))
                    {
                        byte[] bytes = File.ReadAllBytes(path);
                        string entryName = "generated/" + name;
                        AddEntry(zip, entryName, bytes);
                        ScanForCanaries(entryName, bytes, canaryHits);
                    }
                }

                var manifest = new JsonObject
                {
                    ["schema"] = "sts2.export.manifest/1",
                    ["runId"] = request.RunId,
                    ["exportedAt"] = timeProvider.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                    ["safeMode"] = !request.IncludeSqlText,
                    ["segments"] = segmentEntries,
                };
                AddEntry(zip, "manifest.json", Encoding.UTF8.GetBytes(manifest.ToJsonString(Indented)));

                var privacyReport = new JsonObject
                {
                    ["schema"] = "sts2.export.privacy/1",
                    ["canaryScan"] = canaryHits.Count == 0 ? "clean" : "FAILED",
                    ["canaryHits"] = new JsonArray(canaryHits.Select(h => (JsonNode)JsonValue.Create(h)).ToArray()),
                    ["redaction"] = new JsonArray(
                        (JsonNode)"secrets tokenized before journaling (SecretRef)",
                        (JsonNode)"row cells elided in digest capture",
                        (JsonNode)"sql text elided in digest capture"),
                };
                AddEntry(zip, "privacy-report.json", Encoding.UTF8.GetBytes(privacyReport.ToJsonString(Indented)));
            }

            return new ExportBundleResult { BundlePath = bundlePath, Bytes = new FileInfo(bundlePath).Length };
        }

        private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

        private static void AddEntry(ZipArchive zip, string name, byte[] bytes)
        {
            ZipArchiveEntry entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using Stream stream = entry.Open();
            stream.Write(bytes);
        }

        private static void ScanForCanaries(string entryName, byte[] bytes, List<string> hits)
        {
            string text = Encoding.UTF8.GetString(bytes);
            foreach (string canary in CanaryLiterals)
            {
                if (text.Contains(canary, StringComparison.Ordinal))
                {
                    hits.Add(entryName + ": canary");
                }
            }
        }

        /// <summary>Validates a bundle (SPEC §13.1 export-check): hashes, privacy, replayability.</summary>
        public static IReadOnlyList<string> Check(string bundlePath)
        {
            var problems = new List<string>();
            using ZipArchive zip = ZipFile.OpenRead(bundlePath);

            ZipArchiveEntry? manifestEntry = zip.GetEntry("manifest.json");
            if (manifestEntry is null)
            {
                problems.Add("missing manifest.json");
                return problems;
            }

            JsonNode manifest = JsonNode.Parse(ReadEntry(manifestEntry))!;
            foreach (JsonNode? segment in manifest["segments"]!.AsArray())
            {
                string file = segment!["file"]!.GetValue<string>();
                string expected = segment["sha256"]!.GetValue<string>();
                ZipArchiveEntry? entry = zip.GetEntry(file);
                if (entry is null)
                {
                    problems.Add("manifest references missing entry " + file);
                    continue;
                }
                string actual = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(ReadEntryBytes(entry)));
                if (actual != expected)
                {
                    problems.Add($"hash mismatch for {file}: manifest {expected}, actual {actual}");
                }
            }

            ZipArchiveEntry? privacy = zip.GetEntry("privacy-report.json");
            if (privacy is null)
            {
                problems.Add("missing privacy-report.json");
            }
            else if (JsonNode.Parse(ReadEntry(privacy))!["canaryScan"]!.GetValue<string>() != "clean")
            {
                problems.Add("privacy report canary scan is not clean");
            }

            // Replay the bundled journal (R023): a hash-consistent bundle can still be
            // semantically invalid or non-replayable. The export-check contract promises
            // replayability, so prove it — strict replay must reproduce the outbound digest
            // sequence exactly and the journal must not be truncated.
            if (problems.Count == 0)
            {
                try
                {
                    var envelopes = new List<Sts2Envelope>();
                    foreach (JsonNode? segment in manifest["segments"]!.AsArray())
                    {
                        ZipArchiveEntry? entry = zip.GetEntry(segment!["file"]!.GetValue<string>());
                        if (entry is null)
                        {
                            continue;
                        }
                        foreach (string line in ReadEntry(entry).Split('\n'))
                        {
                            if (line.Length > 0)
                            {
                                envelopes.Add(EnvelopeJsonCodec.DeserializeLine(line));
                            }
                        }
                    }
                    ReplayResult replay = JournalReplayer.Replay(envelopes);
                    if (replay.Outcome == ReplayOutcome.Diverged)
                    {
                        problems.Add($"replay diverged at seq {replay.Divergence!.Seq}: recorded {replay.Divergence.Recorded}; replayed {replay.Divergence.Replayed}");
                    }
                    else if (replay.Outcome == ReplayOutcome.Incomplete)
                    {
                        problems.Add($"bundle journal is truncated: {replay.PendingOutputCount} reduced output(s) after seq {replay.LastSeq} were never recorded");
                    }
                }
                catch (Exception ex) when (ex is JsonException or InvalidDataException or FormatException)
                {
                    problems.Add("bundle journal could not be parsed for replay: " + ex.Message);
                }
            }

            return problems;
        }

        private static string ReadEntry(ZipArchiveEntry entry) => Encoding.UTF8.GetString(ReadEntryBytes(entry));

        private static byte[] ReadEntryBytes(ZipArchiveEntry entry)
        {
            using Stream stream = entry.Open();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }
}
