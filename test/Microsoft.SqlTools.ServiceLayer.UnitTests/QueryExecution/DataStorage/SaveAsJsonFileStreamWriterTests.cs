//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.DataStorage
{
    public class SaveAsJsonFileStreamWriterTests
    {
        [Test]
        public void ArrayWrapperTest()
        {
            // Setup: Create storage for the output
            byte[] output = new byte[8192];
            var saveParams = new SaveResultsAsJsonRequestParams();
            var columns = Array.Empty<DbColumnWrapper>();

            // If: I create and then destruct a json writer
            var jsonWriter = new SaveAsJsonFileStreamWriter(new MemoryStream(output), saveParams, columns);
            jsonWriter.Dispose();

            // Then: The output should be an empty array
            string outputString = Encoding.UTF8.GetString(output).TrimEnd('\0');
            object[] outputArray = JsonConvert.DeserializeObject<object[]>(outputString);
            Assert.AreEqual(0, outputArray.Length);
        }

        [Test]
        public void WriteRow_WithoutColumnSelection()
        {
            // Setup:
            // ... Create a request params that has no selection made
            // ... Create a set of data to write
            // ... Create storage for the output
            SaveResultsAsJsonRequestParams saveParams = new SaveResultsAsJsonRequestParams();
            var data = new[]
            {
                new DbCellValue {DisplayValue = "item1", RawObject = "item1"},
                new DbCellValue {DisplayValue = "null", RawObject = null}
            };
            var columns = new[]
            {
                new DbColumnWrapper(new TestDbColumn("column1")),
                new DbColumnWrapper(new TestDbColumn("column2"))
            };
            byte[] output = new byte[8192];

            // If:
            // ... I write two rows
            var jsonWriter = new SaveAsJsonFileStreamWriter(new MemoryStream(output), saveParams, columns);
            using (jsonWriter)
            {
                jsonWriter.WriteRow(data);
                jsonWriter.WriteRow(data);
            }

            // Then:
            // ... Upon deserialization to an array of dictionaries
            string outputString = Encoding.UTF8.GetString(output).TrimEnd('\0');
            var outputObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(outputString);

            // ... There should be 2 items in the array,
            // ... The item should have two fields, and two values, assigned appropriately
            Assert.AreEqual(2, outputObject.Length);
            foreach (var item in outputObject)
            {
                Assert.AreEqual(2, item.Count);
                for (int i = 0; i < columns.Length; i++)
                {
                    Assert.True(item.ContainsKey(columns[i].ColumnName));
                    Assert.AreEqual(data[i].RawObject == null ? null : data[i].DisplayValue, item[columns[i].ColumnName]);
                }
            }
        }

        [Test]
        public void WriteRow_WithColumnSelection()
        {
            // Setup:
            // ... Create a request params that selects n-1 columns from the front and back
            // ... Create a set of data to write
            // ... Create a memory location to store the data
            var saveParams = new SaveResultsAsJsonRequestParams
            {
                ColumnStartIndex = 1,
                ColumnEndIndex = 2,
                RowStartIndex = 0,          // Including b/c it is required to be a "save selection"
                RowEndIndex = 10
            };
            var data = new[]
            {
                new DbCellValue { DisplayValue = "item1", RawObject = "item1"},
                new DbCellValue { DisplayValue = "item2", RawObject = "item2"},
                new DbCellValue { DisplayValue = "null", RawObject = null},
                new DbCellValue { DisplayValue = "null", RawObject = null}
            };
            var columns = new[]
            {
                new DbColumnWrapper(new TestDbColumn("column1")),
                new DbColumnWrapper(new TestDbColumn("column2")),
                new DbColumnWrapper(new TestDbColumn("column3")),
                new DbColumnWrapper(new TestDbColumn("column4"))
            };
            byte[] output = new byte[8192];

            // If: I write two rows
            var jsonWriter = new SaveAsJsonFileStreamWriter(new MemoryStream(output), saveParams, columns);
            using (jsonWriter)
            {
                jsonWriter.WriteRow(data);
                jsonWriter.WriteRow(data);
            }

            // Then:
            // ... Upon deserialization to an array of dictionaries
            string outputString = Encoding.UTF8.GetString(output).Trim('\0');
            var outputObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(outputString);

            // ... There should be 2 items in the array
            // ... The items should have 2 fields and values
            Assert.AreEqual(2, outputObject.Length);
            foreach (var item in outputObject)
            {
                Assert.AreEqual(2, item.Count);
                for (int i = 1; i <= 2; i++)
                {
                    Assert.True(item.ContainsKey(columns[i].ColumnName));
                    Assert.AreEqual(data[i].RawObject == null ? null : data[i].DisplayValue, item[columns[i].ColumnName]);
                }
            }
        }

        [Test]
        public void WriteRow_WithSpecialTypesSuccess()
        {
            // Setup:
            // ... Create a request params that has three different types of value
            // ... Create a set of data to write
            // ... Create storage for the output
            SaveResultsAsJsonRequestParams saveParams = new SaveResultsAsJsonRequestParams();
            var data = new[]
            {
                new DbCellValue {DisplayValue = "1", RawObject = 1},
                new DbCellValue {DisplayValue = "1.234", RawObject = 1.234},
                new DbCellValue {DisplayValue = "2017-07-08T00:00:00", RawObject = new DateTime(2017, 07, 08)},

            };
            var columns = new[]
            {
                new DbColumnWrapper(new TestDbColumn("numberCol", typeof(int))),
                new DbColumnWrapper(new TestDbColumn("decimalCol", typeof(decimal))),
                new DbColumnWrapper(new TestDbColumn("datetimeCol", typeof(DateTime)))
            };
            byte[] output = new byte[8192];

            // If:
            // ... I write two rows
            var jsonWriter = new SaveAsJsonFileStreamWriter(new MemoryStream(output), saveParams, columns);
            using (jsonWriter)
            {
                jsonWriter.WriteRow(data);
                jsonWriter.WriteRow(data);
            }

            // Then:
            // ... Upon deserialization to an array of dictionaries
            string outputString = Encoding.UTF8.GetString(output).TrimEnd('\0');
            Dictionary<string, string>[] outputObject =
                JsonConvert.DeserializeObject<Dictionary<string, string>[]>(outputString);

            // ... There should be 2 items in the array,
            // ... The item should have three fields, and three values, assigned appropriately
            // ... The deserialized values should match the display value
            Assert.AreEqual(2, outputObject.Length);
            foreach (var item in outputObject)
            {
                Assert.AreEqual(3, item.Count);
                for (int i = 0; i < columns.Length; i++)
                {
                    Assert.True(item.ContainsKey(columns[i].ColumnName));
                    Assert.AreEqual(data[i].RawObject == null ? null : data[i].DisplayValue, item[columns[i].ColumnName]);
                }
            }
        }

        [Test]
        public void WriteRow_InconvertibleDataType()
        {
            // Setup:
            // ... Create columns that have null data types
            // ... Create set of data to write
            // ... Create output storage
            var requestParams = new SaveResultsAsJsonRequestParams();
            var columns = new[]
            {
                new DbColumnWrapper(new TestDbColumn("nullCol1", colType: null)),
                new DbColumnWrapper(new TestDbColumn("nullCol2", typeof(SaveAsStreamWriter)))
            };
            var data = new[]
            {
                new DbCellValue {DisplayValue = "1", RawObject = "foobar"},
                new DbCellValue {DisplayValue = "2", RawObject = "foobar"},
            };
            var output = new byte[8192];

            // If: I write the row
            using (var jsonWriter = new SaveAsJsonFileStreamWriter(new MemoryStream(output), requestParams, columns))
            {
                jsonWriter.WriteRow(data);
            }

            // Then:
            // ... Data should deserialize to a dictionary
            var outputString = Encoding.UTF8.GetString(output).Trim('\0');
            var outputObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(outputString);

            // ... There should be one row in the array
            // ... Array should have two elements that corresponds to the display value of the data
            Assert.AreEqual(1, outputObject.Length);
            Assert.AreEqual(2, outputObject[0].Count);
            Assert.AreEqual(data[0].DisplayValue, outputObject[0][columns[0].ColumnName]);
            Assert.AreEqual(data[1].DisplayValue, outputObject[0][columns[1].ColumnName]);
        }
    }
}
