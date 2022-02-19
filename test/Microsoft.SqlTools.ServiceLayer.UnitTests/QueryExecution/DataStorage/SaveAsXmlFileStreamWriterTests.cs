//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.DataStorage
{
    [TestFixture]
    public class SaveAsXmlFileStreamWriterTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ConstructAndDispose(bool formatted)
        {
            // Setup: Create test request and storage for the output
            var saveParams = new SaveResultsAsXmlRequestParams { Formatted = formatted };
            var columns = Array.Empty<DbColumnWrapper>();
            var output = new byte[8192];

            // If: I create and dispose of an XML file writer
            var xmlWriter = new SaveAsXmlFileStreamWriter(new MemoryStream(output), saveParams, columns);
            xmlWriter.Dispose();

            // Then:
            // ... The output should be just the XML node and the root node
            var rootNode = ParseOutput(output, Encoding.UTF8);
            Assert.IsEmpty(rootNode.ChildNodes);

            // ... If the output is formatted, there should be multiple lines
            //     otherwise, there should be only one line
            if (formatted)
            {
                CollectionAssert.Contains(output, (byte)'\n');
            }
            else
            {
                CollectionAssert.DoesNotContain(output, (byte)'\n');
            }
        }

        [Test]
        public void WriteRow_WithoutColumnSelection()
        {
            // Setup:
            // ... Create request params that has no selection made
            // ... Create a set of data to write
            // ... Create storage for the output
            var saveParams = new SaveResultsAsXmlRequestParams();
            var data = new[]
            {
                new DbCellValue { DisplayValue = "item1", RawObject = "item1" },
                new DbCellValue { DisplayValue = "null", RawObject = null }
            };
            var columns = new[]
            {
                new DbColumnWrapper(new TestDbColumn("column1")),
                new DbColumnWrapper(new TestDbColumn("column2"))
            };
            var output = new byte[8192];

            // If: I write two rows
            using (var xmlWriter = new SaveAsXmlFileStreamWriter(new MemoryStream(output), saveParams, columns))
            {
                xmlWriter.WriteRow(data);
                xmlWriter.WriteRow(data);
            }

            // Then:
            // ... XML should be well formed
            var rootNode = ParseOutput(output, Encoding.UTF8);

            // ... Data node should have two nodes for the two rows
            Assert.AreEqual(2, rootNode.ChildNodes.Count);
            for (int i = 0; i < 2; i++)
            {
                // ... Each row should have two nodes for the two cells
                var row = rootNode.ChildNodes[i];
                Assert.IsNotNull(row);
                Assert.AreEqual(2, row.ChildNodes.Count);
                for (int j = 0; j < 2; j++)
                {
                    var cell = row.ChildNodes[j];
                    Assert.IsNotNull(cell);

                    // ... Node name should be column name
                    Assert.AreEqual(columns[j].ColumnName, cell.Name);

                    // ... Node value should be cell value
                    if (data[j].RawObject == null)
                    {
                        Assert.IsEmpty(cell.InnerText);
                    }
                    else
                    {
                        Assert.AreEqual(data[j].DisplayValue, cell.InnerText);
                    }
                }
            }
        }

        [Test]
        public void WriteRow_WithColumnSelection()
        {
            // Setup:
            // ... Create request params that has a selection made
            // ... Create a set of data to write
            // ... Create storage for the output
            var saveParams = new SaveResultsAsXmlRequestParams
            {
                ColumnEndIndex = 2,
                ColumnStartIndex = 1,
                RowEndIndex = 0, // Required for being considered a "selection"
                RowStartIndex = 0
            };
            var data = new[]
            {
                new DbCellValue { DisplayValue = "foo" },
                new DbCellValue { DisplayValue = "item1", RawObject = "item1" },
                new DbCellValue { DisplayValue = "null", RawObject = null },
                new DbCellValue { DisplayValue = "bar" }
            };
            var columns = new[]
            {
                new DbColumnWrapper(new TestDbColumn("ignoredCol")),
                new DbColumnWrapper(new TestDbColumn("column1")),
                new DbColumnWrapper(new TestDbColumn("column2")),
                new DbColumnWrapper(new TestDbColumn("ignoredCol"))
            };
            var output = new byte[8192];

            // If: I write two rows
            using (var xmlWriter = new SaveAsXmlFileStreamWriter(new MemoryStream(output), saveParams, columns))
            {
                xmlWriter.WriteRow(data);
                xmlWriter.WriteRow(data);
            }

            // Then:
            // ... XML should be well formed
            var rootNode = ParseOutput(output, Encoding.UTF8);

            // ... Data node should have two nodes for the two rows
            Assert.AreEqual(2, rootNode.ChildNodes.Count);
            for (int i = 0; i < 2; i++)
            {
                // ... Each row should have two nodes for the two cells
                var row = rootNode.ChildNodes[i];
                Assert.IsNotNull(row);
                Assert.AreEqual(2, row.ChildNodes.Count);
                for (int j = 0; j < 1; j++)
                {
                    var cell = row.ChildNodes[j];
                    var columnIndex = j + 1;
                    Assert.IsNotNull(cell);

                    // ... Node name should be column name
                    Assert.AreEqual(columns[columnIndex].ColumnName, cell.Name);

                    // ... Node value should be cell value
                    if (data[columnIndex].RawObject == null)
                    {
                        Assert.IsEmpty(cell.InnerText);
                    }
                    else
                    {
                        Assert.AreEqual(data[columnIndex].DisplayValue, cell.InnerText);
                    }
                }
            }
        }

        [Test]
        public void WriteRow_NonDefaultEncoding()
        {
            // Setup:
            // ... Create request params that uses a special encoding
            // ... Create a set of data to write
            // ... Create storage for the output
            var saveParams = new SaveResultsAsXmlRequestParams { Encoding = "Windows-1252" };
            var data = new[] { new DbCellValue { DisplayValue = "ü", RawObject = "ü" } };
            var columns = new[] { new DbColumnWrapper(new TestDbColumn("column1")) };
            byte[] output = new byte[8192];

            // If: I write the row
            using (var xmlWriter = new SaveAsXmlFileStreamWriter(new MemoryStream(output), saveParams, columns))
            {
                xmlWriter.WriteRow(data);
            }

            // Then:
            // ... The XML file should have been written properly in windows-1252 encoding
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding("Windows-1252");
            var rootNode = ParseOutput(output, encoding);

            // ... The umlaut should be written using Windows-1252
            Assert.IsNotNull(rootNode.ChildNodes[0]);               // <row>
            Assert.IsNotNull(rootNode.ChildNodes[0].ChildNodes[0]); // <column1>
            Assert.AreEqual(rootNode.ChildNodes[0].ChildNodes[0].InnerText, "ü");
        }

        private XmlNode ParseOutput(byte[] bytes, Encoding encoding)
        {
            var outputString = encoding.GetString(bytes)
                .TrimStart(encoding.GetString(encoding.Preamble).ToCharArray()) // Trim any BOM
                .Trim('\0');
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(outputString);

            // Assert: Two elements at the root, XML and the root node
            Assert.AreEqual(2, xmlDoc.ChildNodes.Count);
            Assert.AreEqual("xml", xmlDoc.ChildNodes[0]?.Name);
            Assert.AreEqual("data", xmlDoc.ChildNodes[1]?.Name);

            return xmlDoc.ChildNodes[1];
        }
    }
}
