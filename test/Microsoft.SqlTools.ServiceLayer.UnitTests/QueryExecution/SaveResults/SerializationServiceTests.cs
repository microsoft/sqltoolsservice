// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.SaveResults
{

    public class SerializationServiceTests
    {

        private static readonly DbCellValue[][] DefaultData = new DbCellValue[3][] {
            new DbCellValue[] { 
                new DbCellValue() { DisplayValue = "1", IsNull = false },
                new DbCellValue() { DisplayValue = "Hello", IsNull = false },
                new DbCellValue() { DisplayValue = "false", IsNull = false },
            },
            new DbCellValue[] { 
                new DbCellValue() { DisplayValue = "2", IsNull = false },
                new DbCellValue() { DisplayValue = null, IsNull = true },
                new DbCellValue() { DisplayValue = "true", IsNull = false },
            },
            new DbCellValue[] { 
                new DbCellValue() { DisplayValue = "3", IsNull = false },
                new DbCellValue() { DisplayValue = "World", IsNull = false },
                new DbCellValue() { DisplayValue = "True", IsNull = false },
            }
        };

        private static readonly ColumnInfo[] DefaultColumns = {
            new ColumnInfo("IntCol", "Int"),
            new ColumnInfo("StringCol", "NVarChar"),
            new ColumnInfo("BitCol", "Bit")
        };

        public SerializationServiceTests()
        {
            HostMock = new Mock<IProtocolEndpoint>();
            ServiceProvider = ExtensionServiceProvider.CreateDefaultServiceProvider();
            HostLoader.InitializeHostedServices(ServiceProvider, HostMock.Object);
            SerializationService = ServiceProvider.GetService<SerializationService>();
        }
        protected ExtensionServiceProvider ServiceProvider { get; private set; }
        protected Mock<IProtocolEndpoint> HostMock { get; private set; }
        protected SerializationService SerializationService { get; private set; }

        [TestCase(true)]
        [TestCase(false)]
        public async Task TestSaveAsCsvSuccess(bool includeHeaders)
        {
            await this.RunFileSaveTest(async (filePath) =>
            {
                // Given: 
                // ... A simple data set that requires 1 message
                SerializeDataStartRequestParams saveParams = new SerializeDataStartRequestParams()
                {
                    FilePath = filePath,
                    Columns = DefaultColumns,
                    Rows = DefaultData,
                    IsLastBatch = true,
                    SaveFormat = "csv",
                    IncludeHeaders = includeHeaders
                };
                // When: I attempt to save this to a file
                var efv = new EventFlowValidator<SerializeDataResult>()
                    .AddStandardResultValidator()
                    .Complete();

                await SerializationService.RunSerializeStartRequest(saveParams, efv.Object);

                // Then:
                // ... There should not have been an error
                efv.Validate();
                // ... And the file should look as expected
                VerifyContents.VerifyCsvMatchesData(saveParams.Rows, saveParams.Columns, saveParams.IncludeHeaders, saveParams.FilePath);
            });
        }

        [TestCase(true)]
        [TestCase(false)]
        public Task TestSaveAsMarkdownSuccess(bool includeHeaders)
        {
            return this.RunFileSaveTest(async filePath =>
            {
                // Give:
                // ... A simple data set that requires 1 message
                var saveParams = new SerializeDataStartRequestParams
                {
                    FilePath = filePath,
                    Columns = DefaultColumns,
                    Rows = DefaultData,
                    IsLastBatch = true,
                    SaveFormat = "markdown",
                    IncludeHeaders = includeHeaders,
                };

                // When: I attempt to save this to a file
                var efv = new EventFlowValidator<SerializeDataResult>()
                    .AddStandardResultValidator()
                    .Complete();

                await SerializationService.RunSerializeStartRequest(saveParams, efv.Object);

                // Then:
                // ... There should not have been any errors
                efv.Validate();

                // ... And the file should look as expected
                VerifyContents.VerifyMarkdownMatchesData(
                    saveParams.Rows,
                    saveParams.Columns,
                    saveParams.IncludeHeaders,
                    saveParams.FilePath);
            });
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task TestSaveAsCsvMultiRequestSuccess(bool includeHeaders)
        {
            Action<SerializeDataStartRequestParams> setParams = (serializeParams) => {
                serializeParams.SaveFormat = "csv";
                serializeParams.IncludeHeaders = includeHeaders;
            };
            Action<string> validation = (filePath) => {
                VerifyContents.VerifyCsvMatchesData(DefaultData, DefaultColumns, includeHeaders, filePath);
            };
            await this.TestSerializeDataMultiRequestSuccess(setParams, validation);
        }
        
        [Test]
        public async Task SaveAsJsonMultiRequestSuccess()
        {
            Action<SerializeDataStartRequestParams> setParams = (serializeParams) => {
                serializeParams.SaveFormat = "json";
            };
            Action<string> validation = (filePath) => {
                VerifyContents.VerifyJsonMatchesData(DefaultData, DefaultColumns, filePath);
            };
            await this.TestSerializeDataMultiRequestSuccess(setParams, validation);
        }

        [Test]
        public async Task SaveAsXmlMultiRequestSuccess()
        {
            Action<SerializeDataStartRequestParams> setParams = (serializeParams) => {
                serializeParams.SaveFormat = "xml";
            };
            Action<string> validation = (filePath) => {
                VerifyContents.VerifyXmlMatchesData(DefaultData, DefaultColumns, filePath);
            };
            await this.TestSerializeDataMultiRequestSuccess(setParams, validation);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task SaveAsMarkdownMultiRequestSuccess(bool includeHeaders)
        {
            Action<SerializeDataStartRequestParams> setParams = serializeParams =>
            {
                serializeParams.SaveFormat = "markdown";
                serializeParams.IncludeHeaders = includeHeaders;
            };
            Action<string> validation = filePath =>
            {
                VerifyContents.VerifyMarkdownMatchesData(DefaultData, DefaultColumns, includeHeaders, filePath);
            };
            await this.TestSerializeDataMultiRequestSuccess(setParams, validation);
        }

        private async Task TestSerializeDataMultiRequestSuccess(Action<SerializeDataStartRequestParams> setStandardParams, Action<string> verify)
        {
            await this.RunFileSaveTest(async (filePath) =>
            {
                // Given: 
                // ... A simple data set that requires 3 messages
                var startParams = new SerializeDataStartRequestParams()
                {
                    FilePath = filePath,
                    Columns = DefaultColumns,
                    Rows = new DbCellValue[][] { DefaultData[0] },
                    IsLastBatch = false
                };
                setStandardParams(startParams);

                // When I send all 3 messages
                await SendAndVerifySerializeStartRequest(startParams);
                var continueParams = new SerializeDataContinueRequestParams()
                {
                    FilePath = filePath,
                    Rows = new DbCellValue[][] { DefaultData[1] },
                    IsLastBatch = false
                };
                await SendAndVerifySerializeContinueRequest(continueParams);
                continueParams.Rows = new DbCellValue[][] { DefaultData[2] };
                continueParams.IsLastBatch = true;
                await SendAndVerifySerializeContinueRequest(continueParams);

                // ... Then the file should look as expected
                verify(filePath);
            });
        }

        private async Task SendAndVerifySerializeStartRequest(SerializeDataStartRequestParams request1)
        {
            // When: I attempt to save this to a file
            var efv = new EventFlowValidator<SerializeDataResult>()
                .AddStandardResultValidator()
                .Complete();

            await SerializationService.RunSerializeStartRequest(request1, efv.Object);
        
            // Then:
            // ... There should not have been an error
            efv.Validate();
        }
        private async Task SendAndVerifySerializeContinueRequest(SerializeDataContinueRequestParams request1)
        {
            // When: I attempt to save this to a file
            var efv = new EventFlowValidator<SerializeDataResult>()
                .AddStandardResultValidator()
                .Complete();

            await SerializationService.RunSerializeContinueRequest(request1, efv.Object);

            // Then:
            // ... There should not have been an error
            efv.Validate();
        }

        private Task RunFileSaveTest(Func<string, Task> doSave)
        {
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            {
                return doSave(tempFile.FilePath);
            }
        }
    }

    public static class SerializeDataEventFlowValidatorExtensions
    {
        public static EventFlowValidator<SerializeDataResult> AddStandardResultValidator(
            this EventFlowValidator<SerializeDataResult> efv)
        {
            return efv.AddResultValidation(r =>
            {
                Assert.NotNull(r);
                Assert.Null(r.Messages);
                Assert.True(r.Succeeded);
            });
        }
    }

    public static class VerifyContents
    {
        public static void VerifyCsvMatchesData(DbCellValue[][] data, ColumnInfo[] columns, bool includeHeaders, string filePath)
        {
            Assert.True(File.Exists(filePath), "Expected file to have been written");
            string[] lines = File.ReadAllLines(filePath);
            int expectedLength = includeHeaders ? data.Length + 1 : data.Length;
            Assert.AreEqual(expectedLength, lines.Length);
            int lineIndex = 0;
            if (includeHeaders)
            {
                AssertLineEquals(lines[lineIndex], columns.Select((c) => c.Name).ToArray());
                lineIndex++;
            }
            for (int dataIndex =0; dataIndex < data.Length && lineIndex < lines.Length; dataIndex++, lineIndex++)
            {
                AssertLineEquals(lines[lineIndex], data[dataIndex].Select((d) => GetCsvPrintValue(d)).ToArray());
            }
        }

        private static string GetCsvPrintValue(DbCellValue d)
        {
            return d.IsNull ? "NULL" : d.DisplayValue;
        }

        private static void AssertLineEquals(string line, string[] expected)
        {
            var actual = line.Split(',');
            Assert.True(actual.Length == expected.Length, $"Line '{line}' does not match values {string.Join(",", expected)}");
            for (int i = 0; i < actual.Length; i++)
            {
                Assert.True(expected[i] == actual[i], $"Line '{line}' does not match values '{string.Join(",", expected)}' as '{expected[i]}' does not equal '{actual[i]}'");
            }
        }

        public static void VerifyMarkdownMatchesData(
            DbCellValue[][] data,
            ColumnInfo[] columns,
            bool includeHeaders,
            string filePath)
        {
            Assert.True(File.Exists(filePath));
            string[] lines = File.ReadAllLines(filePath);

            int expectedLength = includeHeaders ? data.Length + 2 : data.Length;
            Assert.AreEqual(expectedLength, lines.Length);

            int lineOffset = 0;
            if (includeHeaders)
            {
                // First line is |col1|col2|...
                Assert.AreEqual($"|{string.Join("|", columns.Select(c => c.Name))}|", lines[0]);
                // Second line is |---|---|...
                Assert.AreEqual($"|{string.Join("", Enumerable.Repeat("---|", columns.Length))}", lines[1]);

                lineOffset = 2;
            }

            for (int i = 0; i < data.Length; i++)
            {
                var expectedLine = $"|{string.Join("|", data[i].Select(GetMarkdownPrintValue).ToArray())}|";
                Assert.AreEqual(expectedLine, lines[i + lineOffset]);
            }
        }

        private static string GetMarkdownPrintValue(DbCellValue d) =>
            d.IsNull ? "NULL" : d.DisplayValue;

        public static void VerifyJsonMatchesData(DbCellValue[][] data, ColumnInfo[] columns, string filePath)
        {
                        // ... Upon deserialization to an array of dictionaries
            Assert.True(File.Exists(filePath), "Expected file to have been written");
            string output = File.ReadAllText(filePath);
            Dictionary<string, object>[] outputObject =
                JsonConvert.DeserializeObject<Dictionary<string, object>[]>(output);

            // ... There should be 2 items in the array,
            // ... The item should have three fields, and three values, assigned appropriately
            // ... The deserialized values should match the display value
            Assert.AreEqual(data.Length, outputObject.Length);
            for (int rowIndex = 0; rowIndex < outputObject.Length; rowIndex++)
            {
                Dictionary<string,object> item = outputObject[rowIndex];
                Assert.AreEqual(columns.Length, item.Count);
                for (int columnIndex = 0; columnIndex < columns.Length; columnIndex++)
                {
                    var key = columns[columnIndex].Name;
                    Assert.True(item.ContainsKey(key));
                    DbCellValue value = data[rowIndex][columnIndex];
                    object expectedValue = GetJsonExpectedValue(value, columns[columnIndex]);
                    Assert.AreEqual(expectedValue, item[key]);
                }
            }
        }


        private static object GetJsonExpectedValue(DbCellValue value, ColumnInfo column)
        {
            if (value.IsNull)
            {
                return null;
            }
            else if (column.DataTypeName == "Int")
            {
                return Int64.Parse(value.DisplayValue.ToLower());
            }
            else if (column.DataTypeName == "Bit")
            {
                return Boolean.Parse(value.DisplayValue.ToLower());
            }
            return value.DisplayValue;
        }
        public static void VerifyXmlMatchesData(DbCellValue[][] data, ColumnInfo[] columns, string filePath)
        {
                        // ... Upon deserialization to an array of dictionaries
            Assert.True(File.Exists(filePath), "Expected file to have been written");
            string output = File.ReadAllText(filePath);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(output);

            // ... There should be 2 items in the array,
            // ... The item should have three fields, and three values, assigned appropriately
            // ... The deserialized values should match the display value
            string xpath = "data/row";
            var rows = xmlDoc.SelectNodes(xpath);

            Assert.AreEqual(data.Length, rows.Count);
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var rowValue = rows.Item(rowIndex);
                var xmlCols = rowValue.ChildNodes.Cast<XmlNode>().ToArray();
                Assert.AreEqual(columns.Length, xmlCols.Length);
                for (int columnIndex = 0; columnIndex < columns.Length; columnIndex++)
                {
                    var columnName = columns[columnIndex].Name;
                    var xmlColumn = xmlCols.FirstOrDefault(x => x.Name == columnName);
                    Assert.NotNull(xmlColumn);
                    DbCellValue value = data[rowIndex][columnIndex];
                    object expectedValue = GetXmlExpectedValue(value);
                    Assert.AreEqual(expectedValue, xmlColumn.InnerText);
                }
            }
        }

        private static string GetXmlExpectedValue(DbCellValue d)
        {
            return d.IsNull ? "" : d.DisplayValue;
        }
    }
}