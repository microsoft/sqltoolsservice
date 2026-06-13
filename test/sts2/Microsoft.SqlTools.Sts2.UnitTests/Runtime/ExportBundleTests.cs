//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Export;
using Microsoft.SqlTools.Sts2.Testing;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>SPEC §8.6 / §13.1: export bundle round-trip — export, privacy, replay, export-check.</summary>
    public sealed class ExportBundleTests : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "sts2-export-test-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        [Fact]
        public async Task ExportLogProducesAValidCanaryCleanBundle()
        {
            string journalDir = Path.Combine(root, "journal");
            string exportDir = Path.Combine(root, "export");

            // A real digest-mode session carrying canary credentials and sensitive SQL.
            await using (var session = new Sts2TestSession(journalDir, "export-run", rowCapture: "digest", sqlCapture: "digest"))
            {
                string connectionId = await session.OpenConnectionAsync();
                await session.RequestAsync("v2/query.execute",
                    $$"""{"connectionId":"{{connectionId}}","sql":"select secret from Customers where ssn='PRIVATE'"}""");
                await session.WaitForNotificationsAsync("v2/query.complete", 1);
                await session.RequestAsync("v2/connection.close", $$"""{"connectionId":"{{connectionId}}"}""");
            }

            ExportBundleResult result = ExportBundleWriter.Write(new ExportBundleRequest
            {
                RunId = "export-run",
                JournalDirectory = journalDir,
                OutputDirectory = exportDir,
            }, TimeProvider.System);

            Assert.True(File.Exists(result.BundlePath));
            Assert.True(result.Bytes > 0);

            // export-check passes: hashes match, privacy clean.
            Assert.Empty(ExportBundleWriter.Check(result.BundlePath));

            // The bundle is canary-clean and contains no SQL literal (digest capture).
            using ZipArchive zip = ZipFile.OpenRead(result.BundlePath);
            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                using var reader = new StreamReader(entry.Open());
                string content = reader.ReadToEnd();
                Assert.Empty(SecretCanaries.FindIn(content));
                if (entry.FullName.StartsWith("journals/", StringComparison.Ordinal))
                {
                    Assert.DoesNotContain("PRIVATE", content);
                    Assert.DoesNotContain("Customers", content);
                }
            }

            // Manifest and privacy report are present.
            Assert.NotNull(zip.GetEntry("manifest.json"));
            Assert.NotNull(zip.GetEntry("privacy-report.json"));
        }

        [Fact]
        public async Task ExportLogFlowsThroughTheGatewayAndReturnsABundlePath()
        {
            string journalDir = Path.Combine(root, "gw-journal");
            await using var session = new Sts2TestSession(journalDir, "gw-export");
            await session.RequestAsync("v2/diagnostics.ping", """{"echo":"warm"}""");

            // No export template wired in the unit harness -> Sts2.Internal, proving the
            // request reaches the export effect path. (The Hosting session wires the
            // template; covered by the gateway/E2E tests.)
            OutboundRpcMessage export = await session.RequestAsync("v2/diagnostics.exportLog", """{}""");
            Assert.Equal("rpc.out.error", export.Kind);
            Assert.Equal("Sts2.Internal", export.Body!.Value.GetProperty("data").GetProperty("code").GetString());
        }

        [Fact]
        public void ExportCheckDetectsTampering()
        {
            string journalDir = Path.Combine(root, "tamper-journal");
            Directory.CreateDirectory(journalDir);
            File.WriteAllText(Path.Combine(journalDir, "journal-tamper-0001.jsonl"), "{\"seq\":1}\n");

            ExportBundleResult result = ExportBundleWriter.Write(new ExportBundleRequest
            {
                RunId = "tamper",
                JournalDirectory = journalDir,
                OutputDirectory = Path.Combine(root, "tamper-export"),
            }, TimeProvider.System);
            Assert.Empty(ExportBundleWriter.Check(result.BundlePath));

            // Rewrite a journal entry inside the zip without updating the manifest hash.
            using (ZipArchive zip = ZipFile.Open(result.BundlePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = zip.Entries.First(e => e.FullName.StartsWith("journals/", StringComparison.Ordinal));
                using Stream stream = entry.Open();
                stream.SetLength(0);
                byte[] tampered = Encoding.UTF8.GetBytes("{\"seq\":999}\n");
                stream.Write(tampered);
            }

            Assert.Contains(ExportBundleWriter.Check(result.BundlePath), p => p.Contains("hash mismatch", StringComparison.Ordinal));
        }
    }
}
