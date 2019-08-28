//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.DataProtocol.Contracts.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.SqlTools.Hosting.UnitTests.Contracts.Utilities
{
    public class FlagsStringConverterTests
    {
        [Fact]
        public void NullableValueCanBeDeserialized()
        {
            var jsonObject = JObject.Parse("{\"optionalValue\": [\"First\", \"Second\"]}");
            var contract = jsonObject.ToObject<DataContract>();
            Assert.NotNull(contract);
            Assert.NotNull(contract.OptionalValue);
            Assert.Equal(TestFlags.FirstItem | TestFlags.SecondItem, contract.OptionalValue);
        }

        [Fact]
        public void RegularValueCanBeDeserialized()
        {
            var jsonObject = JObject.Parse("{\"Value\": [\"First\", \"Third\"]}");
            var contract = jsonObject.ToObject<DataContract>();
            Assert.NotNull(contract);
            Assert.Equal(TestFlags.FirstItem | TestFlags.ThirdItem, contract.Value);
        }

        [Fact]
        public void ExplicitNullCanBeDeserialized()
        {
            var jsonObject = JObject.Parse("{\"optionalValue\": null}");
            var contract = jsonObject.ToObject<DataContract>();
            Assert.NotNull(contract);
            Assert.Null(contract.OptionalValue);
        }

        [Flags]
        [JsonConverter(typeof(FlagsStringConverter))]
        private enum TestFlags
        {
            [FlagsStringConverter.SerializeValue("First")]
            FirstItem = 1 << 0,

            [FlagsStringConverter.SerializeValue("Second")]
            SecondItem = 1 << 1,

            [FlagsStringConverter.SerializeValue("Third")]
            ThirdItem = 1 << 2,
        }

        private class DataContract
        {
            public TestFlags? OptionalValue { get; set; }

            public TestFlags Value { get; set; }
        }
    }
}