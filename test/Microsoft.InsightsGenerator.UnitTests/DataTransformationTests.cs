//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Xunit;

namespace Microsoft.InsightsGenerator.UnitTests
{
    /// <summary>
    /// DataTransformation tests
    /// </summary>
    public class DataTransformerTests
    {
        [Fact]
        public void TransformTest()
        {
            DataTransformer transformer = new DataTransformer();
            DataArray array = new DataArray();
            array = transformer.Transform(array);
            Assert.NotNull(array);
        }
    }
}

