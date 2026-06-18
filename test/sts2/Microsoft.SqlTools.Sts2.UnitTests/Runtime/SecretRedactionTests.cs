//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
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
