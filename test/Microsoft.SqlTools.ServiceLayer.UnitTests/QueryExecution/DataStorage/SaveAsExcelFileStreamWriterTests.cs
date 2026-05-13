//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.DataStorage
{
    public class SaveAsExcelFileStreamWriterTests
    {
        [Test]
        public void WriteRowCreatesAdditionalWorksheetsWhenRowLimitIsReached()
        {
            var stream = new NonClosingMemoryStream();
            var columns = new[] { new DbColumnWrapper(new TestDbColumn("id")) };
            var saveParams = new SaveResultsAsExcelRequestParams
            {
                IncludeHeaders = true,
                FreezeHeaderRow = true,
                BoldHeaderRow = true,
                AutoFilterHeaderRow = true
            };

            using (var writer = new SaveAsExcelFileStreamWriter(stream, saveParams, columns, maxWorksheetRows: 3))
            {
                for (int rowNumber = 1; rowNumber <= 5; rowNumber++)
                {
                    writer.WriteRow(CreateRow(rowNumber), columns);
                }
            }

            string sheet1Xml = ReadZipEntry(stream, "xl/worksheets/sheet1.xml");
            string sheet2Xml = ReadZipEntry(stream, "xl/worksheets/sheet2.xml");
            string sheet3Xml = ReadZipEntry(stream, "xl/worksheets/sheet3.xml");
            string workbookXml = ReadZipEntry(stream, "xl/workbook.xml");

            Assert.That(workbookXml, Does.Contain("sheet1"));
            Assert.That(workbookXml, Does.Contain("sheet2"));
            Assert.That(workbookXml, Does.Contain("sheet3"));

            Assert.That(CountRows(sheet1Xml), Is.EqualTo(3));
            Assert.That(CountRows(sheet2Xml), Is.EqualTo(3));
            Assert.That(CountRows(sheet3Xml), Is.EqualTo(2));

            Assert.That(sheet1Xml, Does.Contain("<t>id</t>"));
            Assert.That(sheet2Xml, Does.Contain("<t>id</t>"));
            Assert.That(sheet3Xml, Does.Contain("<t>id</t>"));

            Assert.That(sheet1Xml, Does.Contain("<v>1</v>"));
            Assert.That(sheet1Xml, Does.Contain("<v>2</v>"));
            Assert.That(sheet2Xml, Does.Contain("<v>3</v>"));
            Assert.That(sheet2Xml, Does.Contain("<v>4</v>"));
            Assert.That(sheet3Xml, Does.Contain("<v>5</v>"));
        }

        private static IList<DbCellValue> CreateRow(int value)
        {
            return new[] { CreateCell(value) };
        }

        private static DbCellValue CreateCell(int value)
        {
            return new DbCellValue
            {
                DisplayValue = value.ToString(),
                IsNull = false,
                RawObject = value
            };
        }

        private static string ReadZipEntry(MemoryStream stream, string entryName)
        {
            stream.Position = 0;
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            using var reader = new StreamReader(zip.GetEntry(entryName)!.Open());
            return reader.ReadToEnd();
        }

        private static int CountRows(string sheetXml)
        {
            int count = 0;
            int index = 0;

            while ((index = sheetXml.IndexOf("<row ", index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += "<row ".Length;
            }

            return count;
        }

        private sealed class NonClosingMemoryStream : MemoryStream
        {
            protected override void Dispose(bool disposing)
            {
            }
        }
    }
}
