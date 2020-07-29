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

        [Fact]
        public void Tranform_TimeSlicerCount_ProvidedTypes()
        {
            DataTransformer transformer = new DataTransformer();
            object[][] cells = new object[2][];
            cells[0] = new object[3] { "1/15/2020", "Redmond", 50 };
            cells[1] = new object[3] { "1/25/2020", "Bellevue", 75 };

            DataArray array = new DataArray()
            {
                ColumnNames = new string[] { "Date", "City", "Count" },
                ColumnDataType = new DataArray.DataType[] {
                    DataArray.DataType.String, 
                    DataArray.DataType.DateTime, 
                    DataArray.DataType.Number },
                Cells = cells
            };

            array = transformer.Transform(array);
            Assert.Equal(array.TransformedColumnNames[0], "slicer_0");
            Assert.Equal(array.TransformedColumnNames[1], "input_t_0");
            Assert.Equal(array.TransformedColumnNames[2], "output_0");
        }

        [Fact]
        public void Tranform_TimeGroupSlicerCount()
        {
            DataTransformer transformer = new DataTransformer();
            object[][] cells = new object[5][];
            cells[0] = new object[4] { "1/15/2020", "Redmond", "1st Street", 50 };
            cells[1] = new object[4] { "1/25/2020", "Redmond", "2nd Street", 75 };
            cells[2] = new object[4] { "1/10/2020", "Bellevue", "3rd Street", 125 };
            cells[3] = new object[4] { "1/13/2020", "Bellevue", "4th Street", 55 };
            cells[4] = new object[4] { "1/20/2020", "Bellevue", "5th Street", 95 };

            DataArray array = new DataArray()
            {
                ColumnNames = new string[] { "Date", "City", "Address", "Count" },
                Cells = cells
            };

            array = transformer.Transform(array);
            Assert.Equal(array.TransformedColumnNames[0], "input_t_0");
            Assert.Equal(array.TransformedColumnNames[1], "input_g_0");
            Assert.Equal(array.TransformedColumnNames[2], "slicer_0");
            Assert.Equal(array.TransformedColumnNames[3], "output_0");
        }

        [Fact]
        public void Tranform_TimeSlicerCountGroup()
        {
            DataTransformer transformer = new DataTransformer();
            object[][] cells = new object[5][];
            cells[0] = new object[4] { "1/15/2020", "1st Street", 50, "Redmond" };
            cells[1] = new object[4] { "1/25/2020", "2nd Street", 75, "Redmond" };
            cells[2] = new object[4] { "1/10/2020", "3rd Street", 125, "Bellevue" };
            cells[3] = new object[4] { "1/13/2020", "4th Street", 55, "Bellevue" };
            cells[4] = new object[4] { "1/20/2020", "5th Street", 95, "Bellevue" };

            DataArray array = new DataArray()
            {
                ColumnNames = new string[] { "Date", "Address", "Count", "City" },
                Cells = cells
            };

            array = transformer.Transform(array);
            Assert.Equal(array.TransformedColumnNames[0], "input_t_0");
            Assert.Equal(array.TransformedColumnNames[1], "slicer_0");
            Assert.Equal(array.TransformedColumnNames[2], "output_0");
            Assert.Equal(array.TransformedColumnNames[3], "input_g_0");
        }

         [Fact]
        public void Tranform_TimeSlicerCountCount()
        {
            DataTransformer transformer = new DataTransformer();
            object[][] cells = new object[2][];
            cells[0] = new object[4] { "1/15/2020", "1st Street", 50, 110 };
            cells[1] = new object[4] { "1/25/2020", "2nd Street", 75, 160 };

            DataArray array = new DataArray()
            {
                ColumnNames = new string[] { "Date", "Adress", "Count1", "Count2" },
                Cells = cells
            };

            array = transformer.Transform(array);
            Assert.Equal(array.TransformedColumnNames[0], "input_t_0");
            Assert.Equal(array.TransformedColumnNames[1], "slicer_0");
            Assert.Equal(array.TransformedColumnNames[2], "output_0");
            Assert.Equal(array.TransformedColumnNames[3], "output_1");
        }
    }
}
