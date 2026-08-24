//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Export;
using Microsoft.SqlTools.Sts2.Runtime.Redaction;
using Microsoft.SqlTools.Sts2.Testing;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>SPEC §8.5 / I6: secrets are tokenized before anything else sees them.</summary>
    public class SecretRedactionTests
    {
        private static readonly string ConnectionOpenPayload = $$"""
            {
              "openId": "open-7",
              "profile": {
                "server": "tcp:host,1433",
                "database": "master",
                "auth": {
                  "kind": "sqlLogin",
                  "user": "sa",
                  "password": "{{SecretCanaries.Password}}",
                  "customSecret": "{{SecretCanaries.AccessToken}}"
                }
              }
            }
            """;

        [Fact]
        public void TokenIsOpaqueRandomNotDerivedFromTheSecret() // R032
        {
            var table = new SecretSideTable();
            string a = table.Tokenize(SecretCanaries.Password);
            string b = table.Tokenize(SecretCanaries.Password); // SAME secret

            Assert.Matches(@"^secret:ref:[0-9a-f]{32}:\d+$", a);
            // Identical secrets get DIFFERENT, unpredictable tokens (no correlation, no
            // brute-forceable hash prefix of the credential).
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void RedactReportsCreatedTokensForCleanup() // R004
        {
            var table = new SecretSideTable();
            var created = new List<string>();
            SecretRedactor.Redact(JsonNode.Parse(ConnectionOpenPayload), table, created);

            // The gateway uses this list to release tokens on EVERY terminal, including a
            // request Core rejects before any driver resolves them.
            Assert.Equal(2, created.Count);
            Assert.All(created, t => Assert.True(table.TryResolve(t, out _)));
            table.RemoveAll(created);
            Assert.Equal(0, table.Count);
        }

        [Fact]
        public void TokensResolveAndRemove()
        {
            var table = new SecretSideTable();
            string token = table.Tokenize(SecretCanaries.Password);

            Assert.True(table.TryResolve(token, out string secret));
            Assert.Equal(SecretCanaries.Password, secret);
            Assert.Equal(1, table.Count);

            Assert.True(table.Remove(token));
            Assert.False(table.TryResolve(token, out _));
            Assert.Equal(0, table.Count);
        }

        [Fact]
        public void AuthFieldsExceptKindAndUserAreTokenized()
        {
            var table = new SecretSideTable();
            JsonNode redacted = SecretRedactor.Redact(JsonNode.Parse(ConnectionOpenPayload), table)!;
            string json = redacted.ToJsonString();

            Assert.Empty(SecretCanaries.FindIn(json));
            JsonNode auth = redacted["profile"]!["auth"]!;
            Assert.Equal("sqlLogin", auth["kind"]!.GetValue<string>());
            Assert.Equal("sa", auth["user"]!.GetValue<string>());
            Assert.StartsWith("secret:ref:", auth["password"]!.GetValue<string>());
            Assert.StartsWith("secret:ref:", auth["customSecret"]!.GetValue<string>());
            Assert.Equal(2, table.Count);

            // The original secrets are recoverable only through the side table.
            Assert.True(table.TryResolve(auth["password"]!.GetValue<string>(), out string password));
            Assert.Equal(SecretCanaries.Password, password);
        }

        [Fact]
        public void SecretKeysOutsideAuthAreTokenizedAnywhere()
        {
            var table = new SecretSideTable();
            JsonNode redacted = SecretRedactor.Redact(
                JsonNode.Parse($$"""{"nested":{"accessToken":"{{SecretCanaries.AccessToken}}"},"items":[{"password":"{{SecretCanaries.Password}}"}]}"""),
                table)!;
            string json = redacted.ToJsonString();

            Assert.Empty(SecretCanaries.FindIn(json));
            Assert.Equal(2, table.Count);
        }

        [Fact]
        public async Task NonStringCredentialContentNeverReachesTheJournal()
        {
            string directory = Path.Combine(Path.GetTempPath(), "sts2-secret-shape-" + Guid.NewGuid().ToString("N"));
            const string secretDigits = "987654321098765432";
            try
            {
                await using (var session = new Sts2TestSession(directory, "secret-shape"))
                {
                    string payload = """{"openId":"bad-secret","profile":{"driver":"fake","server":"s","auth":{"kind":"sqlLogin","user":"sa","password":SECRET}}}"""
                        .Replace("SECRET", secretDigits, StringComparison.Ordinal);
                    OutboundRpcMessage response = await session.RequestAsync("v2/connection.open", payload);
                    Assert.Equal("rpc.out.error", response.Kind);
                    Assert.Equal("Sts2.InvalidRequest",
                        response.Body!.Value.GetProperty("data").GetProperty("code").GetString());
                }

                string artifacts = string.Join("\n", Directory.EnumerateFiles(directory)
                    .Select(File.ReadAllText));
                Assert.DoesNotContain(secretDigits, artifacts, StringComparison.Ordinal);
                Assert.Contains("$redactedSecret", artifacts, StringComparison.Ordinal);

                ExportBundleResult export = ExportBundleWriter.Write(new ExportBundleRequest
                {
                    RunId = "secret-shape",
                    JournalDirectory = directory,
                    OutputDirectory = directory,
                }, TimeProvider.System);
                using ZipArchive bundle = ZipFile.OpenRead(export.BundlePath);
                string exportedArtifacts = string.Join("\n", bundle.Entries.Select(entry =>
                {
                    using StreamReader reader = new(entry.Open());
                    return reader.ReadToEnd();
                }));
                Assert.DoesNotContain(secretDigits, exportedArtifacts, StringComparison.Ordinal);
                Assert.Contains("$redactedSecret", exportedArtifacts, StringComparison.Ordinal);
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

        [Theory]
        [InlineData("987654321098765432")]
        [InlineData("[\"credential-shaped\"]")]
        public void MalformedAuthValueIsRedactedAsAWhole(string authJson)
        {
            var table = new SecretSideTable();
            JsonNode payload = JsonNode.Parse("""{"profile":{"auth":AUTH}}"""
                .Replace("AUTH", authJson, StringComparison.Ordinal))!;

            JsonNode redacted = SecretRedactor.Redact(payload, table)!;

            Assert.True(redacted["profile"]!["auth"]!["$redactedSecret"]!.GetValue<bool>());
            Assert.DoesNotContain(authJson, redacted.ToJsonString(), StringComparison.Ordinal);
            Assert.Equal(0, table.Count);
        }

        [Fact]
        public void NonSecretFieldsSurviveUntouched()
        {
            var table = new SecretSideTable();
            JsonNode redacted = SecretRedactor.Redact(JsonNode.Parse(ConnectionOpenPayload), table)!;
            Assert.Equal("tcp:host,1433", redacted["profile"]!["server"]!.GetValue<string>());
            Assert.Equal("open-7", redacted["openId"]!.GetValue<string>());
        }

        [Fact]
        public void SideTableSerializationLeaksNoSecrets()
        {
            var table = new SecretSideTable();
            table.Tokenize(SecretCanaries.Password);
            string serialized = JsonSerializer.Serialize(table);
            Assert.Empty(SecretCanaries.FindIn(serialized));
        }

        [Fact]
        public void SameSecretTokenizedTwiceGetsDistinctTokens()
        {
            // The counter suffix keeps tokens unique per open attempt so lifecycle
            // removal of one attempt cannot orphan or free another's secret.
            var table = new SecretSideTable();
            string t1 = table.Tokenize(SecretCanaries.Password);
            string t2 = table.Tokenize(SecretCanaries.Password);
            Assert.NotEqual(t1, t2);
        }
    }
}
