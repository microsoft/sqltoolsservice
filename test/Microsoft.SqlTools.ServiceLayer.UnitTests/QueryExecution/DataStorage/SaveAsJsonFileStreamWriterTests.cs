//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
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
            // Setup:
            // ... Create storage for the output
            byte[] output = new byte[8192];
            SaveResultsAsJsonRequestParams saveParams = new SaveResultsAsJsonRequestParams();

            // If:
            // ... I create and then destruct a json writer
            var jsonWriter = new SaveAsJsonFileStreamWriter(new MemoryStream(output), saveParams, Array.Empty<DbColumnWrapper>());
            jsonWriter.Dispose();

            // Then:
            // ... The output should be an empty array
            string outputString = Encoding.UTF8.GetString(output).TrimEnd('\0');
            object[] outputArray = JsonConvert.DeserializeObject<object[]>(outputString);
            Assert.AreEqual(0, outputArray.Length);
        }

        [Test]
        public void WriteRowWithoutColumnSelection()
        {
            // Setup:
            // ... Create a request params that has no selection made
            // ... Create a set of data to write
            // ... Create storage for the output
            SaveResultsAsJsonRequestParams saveParams = new SaveResultsAsJsonRequestParams();
            List<DbCellValue> data = new List<DbCellValue>
            {
                new DbCellValue {DisplayValue = "item1", RawObject = "item1"},
                new DbCellValue {DisplayValue = "null", RawObject = null}
            };
            List<DbColumnWrapper> columns = new List<DbColumnWrapper>
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
                jsonWriter.WriteRow(data, columns);
                jsonWriter.WriteRow(data, columns);
            }

            // Then:
            // ... Upon deserialization to an array of dictionaries
            string outputString = Encoding.UTF8.GetString(output).TrimEnd('\0');
            Dictionary<string, string>[] outputObject =
                JsonConvert.DeserializeObject<Dictionary<string, string>[]>(outputString);

            // ... There should be 2 items in the array,
            // ... The item should have two fields, and two values, assigned appropriately
            Assert.AreEqual(2, outputObject.Length);
            foreach (var item in outputObject)
            {
                Assert.AreEqual(2, item.Count);
                for (int i = 0; i < columns.Count; i++)
                {
                    Assert.True(item.ContainsKey(columns[i].ColumnName));
                    Assert.AreEqual(data[i].RawObject == null ? null : data[i].DisplayValue, item[columns[i].ColumnName]);
                }
            }
        }

        [Test]
        public void WriteRowWithColumnSelection()
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
            List<DbCellValue> data = new List<DbCellValue>
            {
                new DbCellValue { DisplayValue = "item1", RawObject = "item1"},
                new DbCellValue { DisplayValue = "item2", RawObject = "item2"},
                new DbCellValue { DisplayValue = "null", RawObject = null},
                new DbCellValue { DisplayValue = "null", RawObject = null}
            };
            List<DbColumnWrapper> columns = new List<DbColumnWrapper>
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
                jsonWriter.WriteRow(data, columns);
                jsonWriter.WriteRow(data, columns);
            }

            // Then:
            // ... Upon deserialization to an array of dictionaries
            string outputString = Encoding.UTF8.GetString(output).Trim('\0');
            Dictionary<string, string>[] outputObject =
                JsonConvert.DeserializeObject<Dictionary<string, string>[]>(outputString);

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
        public void WriteRowWithSpecialTypesSuccess()
        {

            // Setup:
            // ... Create a request params that has three different types of value
            // ... Create a set of data to write
            // ... Create storage for the output
            SaveResultsAsJsonRequestParams saveParams = new SaveResultsAsJsonRequestParams();
            List<DbCellValue> data = new List<DbCellValue>
            {
                new DbCellValue {DisplayValue = "1", RawObject = 1},
                new DbCellValue {DisplayValue = "1.234", RawObject = 1.234},
                new DbCellValue {DisplayValue = "2017-07-08T00:00:00", RawObject = new DateTime(2017, 07, 08)},

            };
            List<DbColumnWrapper> columns = new List<DbColumnWrapper>
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
                jsonWriter.WriteRow(data, columns);
                jsonWriter.WriteRow(data, columns);
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
                for (int i = 0; i < columns.Count; i++)
                {
                    Assert.True(item.ContainsKey(columns[i].ColumnName));
                    Assert.AreEqual(data[i].RawObject == null ? null : data[i].DisplayValue, item[columns[i].ColumnName]);
                }
            }
        }
        [Test]
        [TestCase("sl-SI")]
        [TestCase("de-DE")]
        [TestCase("fr-FR")]
        public void WriteRowDecimalUsesInvariantCulture(string cultureName)
        {
            // Setup:
            // ... Switch to an EU locale where '.' is the thousands separator and ',' is decimal
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureName);

                SaveResultsAsJsonRequestParams saveParams = new SaveResultsAsJsonRequestParams();
                // DisplayValue is always dot-delimited as returned by SQL Server
                List<DbCellValue> data = new List<DbCellValue>
                {
                    new DbCellValue { DisplayValue = "12.34",   RawObject = 12.34m },
                    new DbCellValue { DisplayValue = "0.99",    RawObject = 0.99m },
                    new DbCellValue { DisplayValue = "1234.56", RawObject = 1234.56m },
                    new DbCellValue { DisplayValue = "0.01",    RawObject = 0.01m },
                };
                List<DbColumnWrapper> columns = new List<DbColumnWrapper>
                {
                    new DbColumnWrapper(new TestDbColumn("col1", typeof(decimal))),
                    new DbColumnWrapper(new TestDbColumn("col2", typeof(decimal))),
                    new DbColumnWrapper(new TestDbColumn("col3", typeof(decimal))),
                    new DbColumnWrapper(new TestDbColumn("col4", typeof(decimal))),
                };
                byte[] output = new byte[8192];

                // If:
                // ... I write a row while the current culture uses comma as the decimal separator
                var jsonWriter = new SaveAsJsonFileStreamWriter(new MemoryStream(output), saveParams, columns);
                using (jsonWriter)
                {
                    jsonWriter.WriteRow(data, columns);
                }

                // Then:
                // ... The JSON output should contain the original dot-decimal values, not locale-mangled ones
                string outputString = Encoding.UTF8.GetString(output).TrimEnd('\0');
                Dictionary<string, decimal>[] outputObject =
                    JsonConvert.DeserializeObject<Dictionary<string, decimal>[]>(outputString);

                Assert.AreEqual(1, outputObject.Length);
                Assert.AreEqual(12.34m,   outputObject[0]["col1"], "col1 should be 12.34, not 1234");
                Assert.AreEqual(0.99m,    outputObject[0]["col2"], "col2 should be 0.99, not 99");
                Assert.AreEqual(1234.56m, outputObject[0]["col3"], "col3 should be 1234.56, not 123456");
                Assert.AreEqual(0.01m,    outputObject[0]["col4"], "col4 should be 0.01, not 1");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }

        [Test]
        [TestCase("sl-SI")]
        [TestCase("de-DE")]
        public void WriteRowFloatUsesInvariantCulture(string cultureName)
        {
            // Setup:
            // ... Switch to an EU locale where '.' is the thousands separator
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureName);

                SaveResultsAsJsonRequestParams saveParams = new SaveResultsAsJsonRequestParams();
                List<DbCellValue> data = new List<DbCellValue>
                {
                    new DbCellValue { DisplayValue = "3.14", RawObject = 3.14 },
                    new DbCellValue { DisplayValue = "2.72", RawObject = 2.72 },
                };
                List<DbColumnWrapper> columns = new List<DbColumnWrapper>
                {
                    new DbColumnWrapper(new TestDbColumn("floatCol1", typeof(double))),
                    new DbColumnWrapper(new TestDbColumn("floatCol2", typeof(double))),
                };
                byte[] output = new byte[8192];

                var jsonWriter = new SaveAsJsonFileStreamWriter(new MemoryStream(output), saveParams, columns);
                using (jsonWriter)
                {
                    jsonWriter.WriteRow(data, columns);
                }

                string outputString = Encoding.UTF8.GetString(output).TrimEnd('\0');
                Dictionary<string, double>[] outputObject =
                    JsonConvert.DeserializeObject<Dictionary<string, double>[]>(outputString);

                Assert.AreEqual(1, outputObject.Length);
                Assert.AreEqual(3.14, outputObject[0]["floatCol1"], 1e-10, "floatCol1 should be 3.14, not 314");
                Assert.AreEqual(2.72, outputObject[0]["floatCol2"], 1e-10, "floatCol2 should be 2.72, not 272");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }
    }
}
