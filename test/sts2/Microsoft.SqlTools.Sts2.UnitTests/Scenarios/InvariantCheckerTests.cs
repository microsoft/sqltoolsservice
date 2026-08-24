//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Testing;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Scenarios
{
    public sealed class InvariantCheckerTests
    {
        [Fact]
        public async Task I1ReportsAnInboundRequestWithNoTerminalResponse()
        {
            string directory = Path.Combine(Path.GetTempPath(), "sts2-i1-" + Guid.NewGuid().ToString("N"));
            JsonElement payload = JsonDocument.Parse("{}").RootElement.Clone();
            try
            {
                await using (var journal = new JournalWriter("missing-terminal",
                    new JournalOptions { Directory = directory },
                    new JournalRunInfo { ServiceVersion = "test" }))
                {
                    await journal.AppendAsync(new Sts2Envelope
                    {
                        RunId = "missing-terminal",
                        Seq = 1,
                        Ts = DateTimeOffset.UnixEpoch,
                        Kind = EnvelopeKinds.RpcInRequest,
                        Corr = "r-never-answered",
                        Type = "v2/diagnostics.ping",
                        ConfigVersion = 1,
                        Digest = CanonicalJson.DigestOf(payload),
                        Payload = payload,
                    }, flush: true);
                }

                IReadOnlyList<string> violations = InvariantChecker.Check(
                    ["I1"], directory, new Dictionary<string, int>());
                Assert.Contains(violations, violation =>
                    violation.Contains("r-never-answered received 0", StringComparison.Ordinal));
            }
            finally
            {
                try
                {
                    Directory.Delete(directory, recursive: true);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
