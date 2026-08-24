//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.SqlTools.Sts2.Runtime.Effects;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>Exact scalar carriers across the CLR -> JSON -> JavaScript boundary.</summary>
    public sealed class WireValueEncoderTests
    {
        [Theory]
        [InlineData(-9_007_199_254_740_991L)]
        [InlineData(9_007_199_254_740_991L)]
        public void JavaScriptSafeInt64BoundariesRemainJsonNumbers(long value)
        {
            JsonNode encoded = WireValueEncoder.Encode(value)!;
            using JsonDocument document = JsonDocument.Parse(encoded.ToJsonString());
            Assert.Equal(JsonValueKind.Number, document.RootElement.ValueKind);
            Assert.Equal(value, document.RootElement.GetInt64());
        }

        [Theory]
        [InlineData(long.MinValue)]
        [InlineData(-9_007_199_254_740_992L)]
        [InlineData(9_007_199_254_740_992L)]
        [InlineData(long.MaxValue)]
        public void UnsafeInt64ValuesUseAnExactStringWrapper(long value)
        {
            JsonNode encoded = WireValueEncoder.Encode(value)!;
            string json = encoded.ToJsonString();
            using JsonDocument document = JsonDocument.Parse(json);

            Assert.Equal("int64", document.RootElement.GetProperty("$t").GetString());
            Assert.Equal(value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                document.RootElement.GetProperty("v").GetString());
        }
    }
}
