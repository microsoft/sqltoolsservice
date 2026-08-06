//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>
    /// SPEC §8.2: canonicalization is deterministic — UTF-8, ordinal-sorted keys,
    /// invariant formatting, no insignificant whitespace. These goldens freeze the
    /// format: changing any of them invalidates every existing journal digest.
    /// </summary>
    public class CanonicalJsonTests
    {
        private static string Canonical(string json) =>
            Encoding.UTF8.GetString(CanonicalJson.Canonicalize(JsonDocument.Parse(json).RootElement));

        [Fact]
        public void ObjectKeysAreSortedOrdinallyAtEveryDepth()
        {
            Assert.Equal(
                """{"a":{"c":3,"d":2},"b":1}""",
                Canonical("""{"b":1,"a":{"d":2,"c":3}}"""));
        }

        [Fact]
        public void InsignificantWhitespaceIsRemoved()
        {
            Assert.Equal(
                """{"a":[1,2,3],"b":"x y"}""",
                Canonical("{ \"b\" : \"x y\" ,\r\n \"a\" : [ 1, 2,  3 ] }"));
        }

        [Fact]
        public void ArrayOrderIsPreserved()
        {
            Assert.Equal("""[3,1,2]""", Canonical("[3, 1, 2]"));
        }

        [Fact]
        public void UnicodeEscapesNormalizeToOneForm()
        {
            // "A" and "A" are the same string and must digest identically.
            Assert.Equal(Canonical("\"\\u0041\""), Canonical("\"A\""));
        }

        [Fact]
        public void NumberTokensArePreservedVerbatim()
        {
            // Wire-faithful: 1.50 and 1.5 are different tokens, so different canonical
            // forms (D-0007). Same bytes in, same digest out.
            Assert.Equal("""{"a":1.50}""", Canonical("""{"a":1.50}"""));
            Assert.NotEqual(Canonical("""{"a":1.5}"""), Canonical("""{"a":1.50}"""));
        }

        [Fact]
        public void KeyOrderDoesNotAffectDigest()
        {
            string d1 = CanonicalJson.DigestOf(JsonDocument.Parse("""{"x":1,"y":[true,null]}""").RootElement);
            string d2 = CanonicalJson.DigestOf(JsonDocument.Parse("""{"y":[true,null],"x":1}""").RootElement);
            Assert.Equal(d1, d2);
        }

        [Fact]
        public void DigestHasStableFormat()
        {
            string digest = CanonicalJson.DigestOf(JsonDocument.Parse("{}").RootElement);
            Assert.Matches("^sha256:[0-9a-f]{64}$", digest);
        }

        [Fact]
        public void KnownAnswerDigestIsFrozen()
        {
            // Golden: digest of {"a":1} canonical form. If this changes, every journal
            // ever written becomes unverifiable — that is a SPEC-CHANGE, not a test fix.
            string digest = CanonicalJson.DigestOf(JsonDocument.Parse("""{ "a" : 1 }""").RootElement);
            Assert.Equal(
                "sha256:" + Convert.ToHexStringLower(
                    System.Security.Cryptography.SHA256.HashData("""{"a":1}"""u8.ToArray())),
                digest);
        }

        [Fact]
        public void StreamingDigestMatchesOwnedCanonicalBytes()
        {
            JsonElement element = JsonDocument.Parse(
                """{"z":["é",null,{"b":2,"a":1.50}],"a":true}""").RootElement;
            byte[] canonical = CanonicalJson.Canonicalize(element);

            Assert.Equal(
                CanonicalJson.DigestOfCanonicalBytes(canonical),
                CanonicalJson.DigestOf(element));
        }

        [Fact]
        public void TrustedWriterOutputFastPathMatchesFrozenCanonicalForm()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                writer.WriteString("z", string.Concat(Enumerable.Repeat("<i>é𐍈</i>", 4096)));
                writer.WritePropertyName("a");
                writer.WriteStartArray();
                writer.WriteNumberValue(1.50m);
                writer.WriteBooleanValue(true);
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.Flush();
            }
            JsonElement element = JsonDocument.Parse(buffer.WrittenMemory).RootElement;

            CanonicalJson.DigestResult expected = CanonicalJson.DigestAndMeasure(element);
            CanonicalJson.DigestResult actual = CanonicalJson.DigestAndMeasureWriterOutput(element);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GeneralPathStillNormalizesNonCanonicalStringEscapes()
        {
            JsonElement escaped = JsonDocument.Parse("\"\\u0041\"").RootElement;
            JsonElement literal = JsonDocument.Parse("\"A\"").RootElement;

            Assert.Equal(
                CanonicalJson.DigestAndMeasure(escaped),
                CanonicalJson.DigestAndMeasure(literal));
        }

        [Fact]
        public void RedactedScalarWrapperIsCanonicalizedAsPlainData()
        {
            // SPEC §8.2: the wrapper is itself deterministic JSON; both record and replay
            // sides produce identical wrappers, so no special-casing is needed.
            Assert.Equal(
                """{"$redacted":true,"bytes":1234,"digest":"sha256:abc","kind":"sql"}""",
                Canonical("""{"kind":"sql","$redacted":true,"digest":"sha256:abc","bytes":1234}"""));
        }

        [Fact]
        public void TrueFalseNullLiteralsRoundTrip()
        {
            Assert.Equal("""{"f":false,"n":null,"t":true}""", Canonical("""{"t":true,"f":false,"n":null}"""));
        }
    }
}
