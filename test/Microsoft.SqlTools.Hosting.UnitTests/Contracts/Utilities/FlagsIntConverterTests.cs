//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.DataProtocol.Contracts.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using NUnit.Framework;

namespace Microsoft.SqlTools.Hosting.UnitTests.Contracts.Utilities
{
    [TestFixture]
    public class FlagsIntConverterTests
    {
        [Test]
        public void NullableValueCanBeDeserialized()
        {
            var jsonObject = JObject.Parse("{\"optionalValue\": [1, 2]}");
            var contract = jsonObject.ToObject<DataContract>();
            Assert.NotNull(contract);
            Assert.NotNull(contract.OptionalValue);
            Assert.AreEqual(TestFlags.FirstItem | TestFlags.SecondItem, contract.OptionalValue);
        }

        [Test]
        public void RegularValueCanBeDeserialized()
        {
            var jsonObject = JObject.Parse("{\"Value\": [1, 3]}");
            var contract = jsonObject.ToObject<DataContract>();
            Assert.NotNull(contract);
            Assert.AreEqual(TestFlags.FirstItem | TestFlags.ThirdItem, contract.Value);
        }

        [Test]
        public void ExplicitNullCanBeDeserialized()
        {
            var jsonObject = JObject.Parse("{\"optionalValue\": null}");
            var contract = jsonObject.ToObject<DataContract>();
            Assert.NotNull(contract);
            Assert.Null(contract.OptionalValue);
        }

        [Flags]
        [JsonConverter(typeof(FlagsIntConverter))]
        private enum TestFlags
        {
            [FlagsIntConverter.SerializeValue(1)]
            FirstItem = 1 << 0,

            [FlagsIntConverter.SerializeValue(2)]
            SecondItem = 1 << 1,

            [FlagsIntConverter.SerializeValue(3)]
            ThirdItem = 1 << 2,
        }

        private class DataContract
        {
            public TestFlags? OptionalValue { get; set; }

            public TestFlags Value { get; set; }
        }
    }
}