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
        public void Tranform_NullInput()
        {
            DataTransformer transformer = new DataTransformer();
            DataArray array = null;
            array = transformer.Transform(array);
            Assert.Null(array);
        }

        [Fact]
        public void Tranform_TimeSlicerCount_DeduceTypes()
        {
            DataTransformer transformer = new DataTransformer();
            object[][] cells = new object[2][];
            cells[0] = new object[3] { "1/15/2020", "Redmond", 50 };
            cells[1] = new object[3] { "1/25/2020", "Bellevue", 75 };

            DataArray array = new DataArray()
            {
                ColumnNames = new string[] { "Date", "City", "Count" },
                Cells = cells
            };

            array = transformer.Transform(array);
            Assert.Equal(array.TransformedColumnNames[0], "input_t_0");
            Assert.Equal(array.TransformedColumnNames[1], "slicer_0");
            Assert.Equal(array.TransformedColumnNames[2], "output_0");
        }
    }
}
