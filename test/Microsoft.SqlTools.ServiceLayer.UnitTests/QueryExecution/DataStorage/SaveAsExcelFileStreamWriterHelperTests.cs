// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using System;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.DataStorage
{
    public partial class SaveAsExcelFileStreamWriterHelperTests
    {
        [Test]
        public void DefaultSheet()
        {
            var stream = new MemoryStream();
            using (var helper = new SaveAsExcelFileStreamWriterHelper(stream, true))
            using (var sheet = helper.AddSheet(null, 7))
            {
                var value = new DbCellValue();
                sheet.AddRow();

                value.IsNull = true;
                sheet.AddCell(value);

                value.IsNull = false;
                value.RawObject = "";
                sheet.AddCell(value);

                value.RawObject = "test string";
                sheet.AddCell(value);

                value.RawObject = 3;
                sheet.AddCell(value);

                value.RawObject = 3.5;
                sheet.AddCell(value);

                value.RawObject = false;
                sheet.AddCell(value);

                value.RawObject = true;
                sheet.AddCell(value);

                sheet.AddRow();

                value.RawObject = new DateTime(1900, 2, 28);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1900, 3, 1);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1900, 3, 1, 15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1, 1, 1, 15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new TimeSpan(15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new TimeSpan(24, 00, 00);
                sheet.AddCell(value);
            }

            ContentMatch(stream, "[Content_Types].xml");
            ContentMatch(stream, "_rels/.rels");
            ContentMatch(stream, "xl/_rels/workbook.xml.rels");
            ContentMatch(stream, "xl/styles.xml");
            ContentMatch(stream, "xl/workbook.xml");
            ContentMatch(stream, "xl/worksheets/sheet1.xml");

            stream.Dispose();
        }

        [Test]
        public void SheetWithFrozenHeaderRow()
        {
            var stream = new MemoryStream();
            using (var helper = new SaveAsExcelFileStreamWriterHelper(stream, true))
            using (var sheet = helper.AddSheet(null, 7))
            {
                sheet.FreezeHeaderRow();

                var value = new DbCellValue();
                sheet.AddRow();

                value.IsNull = true;
                sheet.AddCell(value);

                value.IsNull = false;
                value.RawObject = "";
                sheet.AddCell(value);

                value.RawObject = "test string";
                sheet.AddCell(value);

                value.RawObject = 3;
                sheet.AddCell(value);

                value.RawObject = 3.5;
                sheet.AddCell(value);

                value.RawObject = false;
                sheet.AddCell(value);

                value.RawObject = true;
                sheet.AddCell(value);

                sheet.AddRow();

                value.RawObject = new DateTime(1900, 2, 28);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1900, 3, 1);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1900, 3, 1, 15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1, 1, 1, 15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new TimeSpan(15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new TimeSpan(24, 00, 00);
                sheet.AddCell(value);
            }

            ContentMatch(stream, "[Content_Types].xml");
            ContentMatch(stream, "_rels/.rels");
            ContentMatch(stream, "xl/_rels/workbook.xml.rels");
            ContentMatch(stream, "xl/styles.xml");
            ContentMatch(stream, "xl/workbook.xml");
            ContentMatch(stream, "xl/worksheets/sheet1.xml", "xl/worksheets/sheet1-headerRowFrozen.xml");

            stream.Dispose();
        }

        [Test]
        public void SheetWithFrozenHeaderRowCalledTooLate()
        {
            var expectedException = new InvalidOperationException("Must be called before calling AddRow");
            var actualException = new Exception();
            var stream = new MemoryStream();
            using (var helper = new SaveAsExcelFileStreamWriterHelper(stream, true))
            using (var sheet = helper.AddSheet(null, 7))
            {
                sheet.AddRow();

                try
                {
                    sheet.FreezeHeaderRow();

                    Assert.Fail("Did not throw an exception when calling FreezeHeaderRow too late");
                }
                catch (Exception e)
                {
                    actualException = e;
                }
            }

            Assert.AreEqual(expectedException.GetType(), actualException.GetType());
            Assert.AreEqual(expectedException.Message, actualException.Message);

            stream.Dispose();
        }

        [Test]
        public void SheetWithBoldHeaderRow()
        {
            var stream = new MemoryStream();
            using (var helper = new SaveAsExcelFileStreamWriterHelper(stream, true))
            using (var sheet = helper.AddSheet(null, 7))
            {
                var value = new DbCellValue();
                sheet.AddRow();

                value.IsNull = true;
                sheet.AddCell(value);

                value.IsNull = false;
                value.RawObject = "";
                sheet.AddCell(value);

                value.RawObject = "test string";
                sheet.AddCell(value);

                value.RawObject = 3;
                sheet.AddCell(value);

                value.RawObject = 3.5;
                sheet.AddCell(value);

                value.RawObject = false;
                sheet.AddCell(value);

                value.RawObject = true;
                sheet.AddCell(value);

                sheet.AddRow();

                value.RawObject = new DateTime(1900, 2, 28);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1900, 3, 1);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1900, 3, 1, 15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1, 1, 1, 15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new TimeSpan(15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new TimeSpan(24, 00, 00);
                sheet.AddCell(value);
            }

            ContentMatch(stream, "[Content_Types].xml");
            ContentMatch(stream, "_rels/.rels");
            ContentMatch(stream, "xl/_rels/workbook.xml.rels");
            ContentMatch(stream, "xl/styles.xml");
            ContentMatch(stream, "xl/workbook.xml");
            ContentMatch(stream, "xl/worksheets/sheet1.xml", "xl/worksheets/sheet1-headerRowBold.xml");

            stream.Dispose();
        }

        [Test]
        public void SheetWithAutoFilterEnabled()
        {
            var stream = new MemoryStream();
            using (var helper = new SaveAsExcelFileStreamWriterHelper(stream, true))
            using (var sheet = helper.AddSheet(null, 7))
            {
                sheet.EnableAutoFilter();

                var value = new DbCellValue();
                sheet.AddRow();

                value.IsNull = true;
                sheet.AddCell(value);

                value.IsNull = false;
                value.RawObject = "";
                sheet.AddCell(value);

                value.RawObject = "test string";
                sheet.AddCell(value);

                value.RawObject = 3;
                sheet.AddCell(value);

                value.RawObject = 3.5;
                sheet.AddCell(value);

                value.RawObject = false;
                sheet.AddCell(value);

                value.RawObject = true;
                sheet.AddCell(value);

                sheet.AddRow();

                value.RawObject = new DateTime(1900, 2, 28);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1900, 3, 1);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1900, 3, 1, 15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1, 1, 1, 15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new TimeSpan(15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new TimeSpan(24, 00, 00);
                sheet.AddCell(value);
            }

            ContentMatch(stream, "[Content_Types].xml");
            ContentMatch(stream, "_rels/.rels");
            ContentMatch(stream, "xl/_rels/workbook.xml.rels");
            ContentMatch(stream, "xl/styles.xml");
            ContentMatch(stream, "xl/workbook.xml");
            ContentMatch(stream, "xl/worksheets/sheet1.xml", "xl/worksheets/sheet1-autoFilterEnabled.xml");

            stream.Dispose();
        }

        [Test]
        public void SheetWriteColumnInformationCalledTooLate()
        {
            var expectedException = new InvalidOperationException("Must be called before calling AddRow");
            var actualException = new Exception();
            var stream = new MemoryStream();
            using (var helper = new SaveAsExcelFileStreamWriterHelper(stream, true))
            using (var sheet = helper.AddSheet(null, 7))
            {
                sheet.AddRow();

                try
                {
                    sheet.WriteColumnInformation(new []{ 1F, 2F, 3F, 4F, 5F, 6F, 7F });

                    Assert.Fail("Did not throw an exception when calling WriteColumnInformation too late");
                }
                catch (Exception e)
                {
                    actualException = e;
                }
            }

            Assert.AreEqual(expectedException.GetType(), actualException.GetType());
            Assert.AreEqual(expectedException.Message, actualException.Message);

            stream.Dispose();
        }

        [Test]
        public void SheetWriteColumnInformationWithWrongAmount()
        {
            var expectedException = new InvalidOperationException("Column count mismatch");
            var actualException = new Exception();
            var stream = new MemoryStream();
            using (var helper = new SaveAsExcelFileStreamWriterHelper(stream, true))
            using (var sheet = helper.AddSheet(null, 7))
            {
                try
                {
                    sheet.WriteColumnInformation(new[] { 1F, 2F, 3F, 4F, 5F });

                    Assert.Fail("Did not throw an exception when calling WriteColumnInformation with the wrong number of columns");
                }
                catch (Exception e)
                {
                    actualException = e;
                }

                var value = new DbCellValue();
                sheet.AddRow();

                value.IsNull = true;
                sheet.AddCell(value);

                value.IsNull = false;
                value.RawObject = "";
                sheet.AddCell(value);

                value.RawObject = "test string";
                sheet.AddCell(value);

                value.RawObject = 3;
                sheet.AddCell(value);

                value.RawObject = 3.5;
                sheet.AddCell(value);

                value.RawObject = false;
                sheet.AddCell(value);

                value.RawObject = true;
                sheet.AddCell(value);

                sheet.AddRow();

                value.RawObject = new DateTime(1900, 2, 28);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1900, 3, 1);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1900, 3, 1, 15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1, 1, 1, 15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new TimeSpan(15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new TimeSpan(24, 00, 00);
                sheet.AddCell(value);
            }

            Assert.AreEqual(expectedException.GetType(), actualException.GetType());
            Assert.AreEqual(expectedException.Message, actualException.Message);

            stream.Dispose();
        }

        [Test]
        public void SheetWriteColumnInformation()
        {
            var stream = new MemoryStream();
            using (var helper = new SaveAsExcelFileStreamWriterHelper(stream, true))
            using (var sheet = helper.AddSheet(null, 7))
            {
                sheet.WriteColumnInformation(new[] { 1F, 2F, 3F, 4F, 5F, 6F, 7F });

                var value = new DbCellValue();
                sheet.AddRow();

                value.IsNull = true;
                sheet.AddCell(value);

                value.IsNull = false;
                value.RawObject = "";
                sheet.AddCell(value);

                value.RawObject = "test string";
                sheet.AddCell(value);

                value.RawObject = 3;
                sheet.AddCell(value);

                value.RawObject = 3.5;
                sheet.AddCell(value);

                value.RawObject = false;
                sheet.AddCell(value);

                value.RawObject = true;
                sheet.AddCell(value);

                sheet.AddRow();

                value.RawObject = new DateTime(1900, 2, 28);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1900, 3, 1);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1900, 3, 1, 15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1, 1, 1, 15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new TimeSpan(15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new TimeSpan(24, 00, 00);
                sheet.AddCell(value);
            }

            ContentMatch(stream, "[Content_Types].xml");
            ContentMatch(stream, "_rels/.rels");
            ContentMatch(stream, "xl/_rels/workbook.xml.rels");
            ContentMatch(stream, "xl/styles.xml");
            ContentMatch(stream, "xl/workbook.xml");
            ContentMatch(stream, "xl/worksheets/sheet1.xml", "xl/worksheets/sheet1-withColumns.xml");

            stream.Dispose();
        }

        [GeneratedRegex("\\r?\\n\\s*")]
        private static partial Regex GetContentRemoveLinebreakLeadingSpaceRegex();

        private void ContentMatch(Stream stream, string fileName)
        {
            ContentMatch(stream, fileName, fileName);
        }

        private void ContentMatch(Stream stream, string realFileName, string referenceFileName)
        {
            string referencePath = Path.Combine(RunEnvironmentInfo.GetTestDataLocation(),
                "DataStorage",
                "SaveAsExcelFileStreamWriterHelperTests",
                referenceFileName);
            string referenceContent = File.ReadAllText(referencePath);
            referenceContent = GetContentRemoveLinebreakLeadingSpaceRegex().Replace(referenceContent, "");

            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                using (var reader = new StreamReader(zip?.GetEntry(realFileName)?.Open()!))
                {
                    string realContent = reader.ReadToEnd();
                    Assert.AreEqual(referenceContent, realContent);
                }
            }
        }
    }

    public class SaveAsExcelFileStreamWriterHelperReferenceManagerTests
    {
        private Mock<XmlWriter> _xmlWriterMock;
        private string? LastWrittenReference { get; set; }
        private int LastWrittenRow { get; set; }

        public SaveAsExcelFileStreamWriterHelperReferenceManagerTests()
        {
            _xmlWriterMock = new Mock<XmlWriter>(MockBehavior.Strict);
            _xmlWriterMock
                .Setup(_xmlWriter => _xmlWriter.WriteChars(
                    It.IsAny<char[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback<char[], int, int>((array, index, count) =>
                    LastWrittenReference = new string(array, index, count));
            _xmlWriterMock.Setup(a => a.WriteStartAttribute(null, "r", null));
            _xmlWriterMock.Setup(a => a.WriteEndAttribute());
            _xmlWriterMock.Setup(a => a.WriteValue(It.IsAny<int>()))
                .Callback<int>(row => LastWrittenRow = row);
        }

        [Test]
        public void ReferenceA1()
        {
            var xmlWriter = _xmlWriterMock.Object;
            var manager = new SaveAsExcelFileStreamWriterHelper.ReferenceManager(xmlWriter);
            manager.WriteAndIncreaseRowReference();
            manager.WriteAndIncreaseColumnReference();
            Assert.AreEqual("A1", LastWrittenReference);
        }
        [Test]
        public void ReferenceZ1()
        {
            var xmlWriter = _xmlWriterMock.Object;
            var manager = new SaveAsExcelFileStreamWriterHelper.ReferenceManager(xmlWriter);
            manager.WriteAndIncreaseRowReference();
            for (int i = 0; i < 26 - 1; i++)
            {
                manager.IncreaseColumnReference();
            }
            manager.WriteAndIncreaseColumnReference();
            Assert.AreEqual("Z1", LastWrittenReference);
            manager.WriteAndIncreaseColumnReference();
            Assert.AreEqual("AA1", LastWrittenReference);
        }
        [Test]
        public void ReferenceZZ1()
        {
            var xmlWriter = _xmlWriterMock.Object;
            var manager = new SaveAsExcelFileStreamWriterHelper.ReferenceManager(xmlWriter);
            manager.WriteAndIncreaseRowReference();

            for (int i = 0; i < 27 * 26 - 1; i++)
            {
                manager.IncreaseColumnReference();
            }
            manager.WriteAndIncreaseColumnReference();
            Assert.AreEqual("ZZ1", LastWrittenReference);
            manager.WriteAndIncreaseColumnReference();
            Assert.AreEqual("AAA1", LastWrittenReference);
        }
        [Test]
        public void ReferenceXFD()
        {
            var xmlWriter = _xmlWriterMock.Object;
            var manager = new SaveAsExcelFileStreamWriterHelper.ReferenceManager(xmlWriter);
            manager.WriteAndIncreaseRowReference();

            for (int i = 0; i < 16384 - 1; i++)
            {
                manager.IncreaseColumnReference();
            }
            //The 16384 should be the maximal column and not throw
            manager.AssureColumnReference();
            manager.WriteAndIncreaseColumnReference();
            Assert.AreEqual("XFD1", LastWrittenReference);
            var ex = Assert.Throws<InvalidOperationException>(manager.AssureColumnReference);
            Assert.That(ex.Message, Does.Contain("max column number is 16384"));
        }
        [Test]
        public void ReferenceRowReset()
        {
            var xmlWriter = _xmlWriterMock.Object;
            var manager = new SaveAsExcelFileStreamWriterHelper.ReferenceManager(xmlWriter);
            manager.WriteAndIncreaseRowReference();
            Assert.AreEqual(1, LastWrittenRow);
            manager.WriteAndIncreaseColumnReference();
            Assert.AreEqual("A1", LastWrittenReference);
            manager.WriteAndIncreaseColumnReference();
            Assert.AreEqual("B1", LastWrittenReference);

            //add row should reset column reference 
            manager.WriteAndIncreaseRowReference();
            Assert.AreEqual(2, LastWrittenRow);
            manager.WriteAndIncreaseColumnReference();
            Assert.AreEqual("A2", LastWrittenReference);
        }

        [Test]
        public void AddRowMustBeCalledBeforeAddCellException()
        {
            var xmlWriter = _xmlWriterMock.Object;
            var manager = new SaveAsExcelFileStreamWriterHelper.ReferenceManager(xmlWriter);

            var ex = Assert.Throws<InvalidOperationException>(manager.AssureColumnReference);
            Assert.That(ex.Message, Does.Contain("AddRow must be called before AddCell"));
        }

    }
}
