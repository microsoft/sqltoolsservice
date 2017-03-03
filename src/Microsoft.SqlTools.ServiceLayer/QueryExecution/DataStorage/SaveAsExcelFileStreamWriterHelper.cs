using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public sealed class SaveAsExcelFileStreamWriterHelper : IDisposable
    {
        private enum Style
        {
            Normal,
            Date,
            Time,
            DateTime
        }
        public sealed class ExcelSheet : IDisposable
        {
            void AddCellEmpty()
            {
                _currColumn++;
            }
            public void AddCellNumber(object num)
            {
                AddCellNumberStart();
                _dataWriter.Write(num.ToString());
                AddCellNumberEnd();
            }
            public void AddCell(DateTime dateTime)
            {
                AddCellContract();
                if (dateTime == DateTime.MinValue)
                {
                    AddCellEmpty();
                }
                else if (dateTime.Date == DateTime.MinValue)  //date empty, time only
                {
                    AddCellDateTimeInternal(dateTime.ToString("hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture), Style.Time);
                }
                else if (dateTime.Date == dateTime) //time empty
                {
                    AddCellDateTimeInternal(dateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), Style.Date);
                }
                else
                {
                    AddCellDateTimeInternal(dateTime.ToString("s", System.Globalization.CultureInfo.InvariantCulture), Style.DateTime);
                }

            }
            // datetime need <c r="A1" t="d" s="2"><v>2012-14-14T12:30:00</v></c>
            private void AddCellDateTimeInternal(string dateTimeString, Style style)
            {
                _dataWriter.Write("<c r=\"");
                WriteColumnRef(_dataWriter, _currRow, _currColumn);
                _dataWriter.Write("\" t=\"d\" s=\"");
                _dataWriter.Write((int)style);
                _dataWriter.Write("\"><v>");
                _dataWriter.Write(dateTimeString);
                _dataWriter.Write("</v></c>");
                _currColumn++;
            }
            public void AddCell(string value)
            {
                if (value == null)
                {
                    AddCellEmpty();
                    return;
                }
                AddCellContract();
                _dataWriter.Write("<c r=\"");
                WriteColumnRef(_dataWriter, _currRow, _currColumn);
                _currColumn++;
                // page 1598
                _dataWriter.Write("\" t=\"inlineStr\"><is><t>");

                //write data
                XmlEscapeHelper.WriteEscapeColumnValue(_dataWriter, value);

                _dataWriter.Write("</t></is></c>");
            }
            public void AddRow()
            {
                if (_currRow != 0) //there are previous rows
                {
                    _dataWriter.Write("</row>");
                }
                _currColumn = 1;
                _currRow++;
                _dataWriter.Write("<row r=\"");
                _dataWriter.Write(_currRow);
                _dataWriter.Write("\">");
            }

            public ExcelSheet(ZipArchive zipArchive, string sheetFileName)
            {
                ZipArchiveEntry entry = zipArchive.CreateEntry($"xl/worksheets/{sheetFileName}.xml", CompressionLevel.Fastest);
                _dataWriter = new StreamWriter(entry.Open());
                _dataWriter.Write(@"<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships""><sheetData>");
            }

            public void Dispose()
            {
                if (_currRow != 0) //first row
                {
                    _dataWriter.Write("</row>");
                }
                _dataWriter.Write(@"</sheetData>");
                _dataWriter.Write(@"</worksheet>");
                _dataWriter.Dispose();
            }

            private StreamWriter _dataWriter;
            private int _currColumn;
            private int _currRow = 0;

            internal static void WriteColumnRef(StreamWriter _dataWriter, int row, int column)
            {
                if (column <= 26)
                {
                    _dataWriter.Write((char)((int)'A' - 1 + column));
                }
                else
                {
                    WriteColumnRefSlow(_dataWriter, column);
                }
                _dataWriter.Write(row);
            }
            private static void WriteColumnRefSlow(StreamWriter _dataWriter, int column)
            {
                column = column - 1; //change to 0 based index
                if (column < 27 * 26)
                {
                    _dataWriter.Write((char)((int)'A' - 1 + (column / 26)));
                    _dataWriter.Write((char)((int)'A' + (column % 26)));
                }
                else
                {
                    column = column - 27 * 26;
                    _dataWriter.Write((char)((int)'A' + (column / (26 * 26))));
                    _dataWriter.Write((char)((int)'A' + (column / 26 % 26)));
                    _dataWriter.Write((char)((int)'A' + (column % 26)));
                }
            }
            private void AddCellContract()
            {
                if (_currColumn == 0)
                {
                    throw new ExporterException("AddRow Must be called before AddCell");

                }
                if (_currColumn > 16384)
                {
                    throw new ExporterException("max Column number is 16384, see https://support.office.com/en-us/article/Excel-specifications-and-limits-1672b34d-7043-467e-8e27-269d656771c3");
                }
            }

            private void AddCellNumberStart()
            {
                AddCellContract();
                _dataWriter.Write("<c r=\"");
                WriteColumnRef(_dataWriter, _currRow, _currColumn);
                _dataWriter.Write("\" t=\"n\"><v>");
            }

            private void AddCellNumberEnd()
            {
                _dataWriter.Write("</v></c>");
                _currColumn++;
            }


        }

        public static class XmlEscapeHelper
        {
            private static char[] xmlEscapes = new char[] { '"', '\'', '&', '<', '>' };
            public static void WriteEscapeColumnValue(StreamWriter dataWriter, string data)
            {
                //fast path, no need to escape
                if (data.IndexOfAny(xmlEscapes) == -1)
                {
                    dataWriter.Write(data);
                    return;
                }
                WriteEscapeColumnValueSlow(dataWriter, data);
            }
            private static void WriteEscapeColumnValueSlow(StreamWriter dataWriter, string data)
            {
                foreach (char c in data)
                {
                    switch (c)
                    {
                        case '"':
                            dataWriter.Write("&quot;");
                            break;
                        case '\'':
                            dataWriter.Write("&apos;");
                            break;
                        case '&':
                            dataWriter.Write("&amp;");
                            break;
                        case '<':
                            dataWriter.Write("&lt;");
                            break;
                        case '>':
                            dataWriter.Write("&gt;");
                            break;
                        default:
                            dataWriter.Write(c);
                            break;
                    }
                }
            }
        }

        public class ExporterException : Exception
        {
            public ExporterException(string message)
                    : base(message)
            {
            }
        }

        public SaveAsExcelFileStreamWriterHelper(Stream stream)
        {
            zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true);
        }

        private void WriteXmlDeclaration(StreamWriter writer)
        {
            writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n");
        }

        public ExcelSheet AddSheet(string sheetName = null)
        {
            string sheetFileName = "sheet" + (sheetNames.Count + 1);
            if (sheetName == null)
            {
                sheetName = sheetFileName;
            }
            EnsureValidSheetName(sheetName);

            sheetNames.Add(sheetName);
            return new ExcelSheet(zipArchive, sheetFileName);
        }

        public void Dispose()
        {
            WriteMinimalTemplate();
        }

        private ZipArchive zipArchive;
        private List<string> sheetNames = new List<string>();

        private StreamWriter AddEntry(string entryName)
        {
            ZipArchiveEntry entry = zipArchive.CreateEntry(entryName, CompressionLevel.Fastest);
            return new StreamWriter(entry.Open());
        }

        private void AddEntryWithContent(string entryName, string content)
        {
            using (StreamWriter writer = AddEntry(entryName))
            {
                WriteXmlDeclaration(writer);
                writer.Write(content);
            }
        }
        //ECMA-376 page 75
        private void WriteMinimalTemplate()
        {
            AddTopRel();
            AddWorkbook();
            AddStyle();
            AddContentType();
            AddWorkbookRel();
            zipArchive.Dispose();
        }
        private void AddContentType()
        {
            using (StreamWriter sw = AddEntry("[Content_Types].xml"))
            {
                sw.Write(
"<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
"<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
"<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
"<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>"
);
                int numSheets = sheetNames.Count;
                for (int i = 1; i <= numSheets; ++i)
                {
                    sw.Write($"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
                }
                sw.Write("</Types>");
            }
        }

        private void AddTopRel()
        {
            string content = @"<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>";
            AddEntryWithContent("_rels/.rels", content);
        }

        private static char[] _invalidSheetNameCharacters = new char[]
        {
            '\\', '/','*','[',']',':','?'
        };
        internal void EnsureValidSheetName(string sheetName)
        {
            if (sheetName.IndexOfAny(_invalidSheetNameCharacters) != -1)
            {
                throw new ExporterException($"Invalid sheetname: sheetName");
            }
            if (sheetNames.IndexOf(sheetName) != -1)
            {
                throw new ExporterException($"Duplicate sheetName: {sheetName}");
            }
        }



        private void AddWorkbook()
        {
            using (StreamWriter sw = AddEntry("xl/workbook.xml"))
            {
                sw.Write("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
                int numSheets = sheetNames.Count;
                for (int i = 1; i <= numSheets; ++i)
                {
                    sw.Write($"<sheet name=\"");
                    XmlEscapeHelper.WriteEscapeColumnValue(sw, sheetNames[i - 1]);
                    sw.Write($"\" sheetId=\"{i}\" r:id=\"rId{i}\"/>");
                }
                sw.Write("</sheets></workbook>");
            }
        }

        private void AddWorkbookRel()
        {
            using (StreamWriter sw = AddEntry("xl/_rels/workbook.xml.rels"))
            {
                sw.Write(
"<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
"<Relationship Id=\"rId0\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>"
);
                int numSheets = sheetNames.Count;
                for (int i = 1; i <= numSheets; ++i)
                {
                    sw.Write("<Relationship Id=\"rId");
                    sw.Write(i);
                    sw.Write("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet");
                    sw.Write(i);
                    sw.Write(".xml\"/>");
                }
                sw.Write("</Relationships>");
            }
        }

        private void AddStyle()
        {
            // the style 0 is used for general case, style 1 for date, style 2 for time and style 3 for datetime see Enum Style
            // reference chain: (index start with 0)
            // <c>(in sheet1.xml) --> (by s) <cellXfs> --> (by xfId) <cellStyleXfs>
            //                                               --> (by numFmtId) <numFmts>
            // that is <c s="1"></c> will reference the second element of <cellXfs> <xf numFmtId=""162"" xfId=""0"" applyNumberFormat=""1""/>
            // then, this xf reference numFmt by name and get formatCode "hh:mm:ss"

            string content = @"
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <numFmts count=""3"">
    <numFmt numFmtId=""166"" formatCode=""yyyy-mm-dd""/>
    <numFmt numFmtId=""167"" formatCode=""hh:mm:ss""/>
    <numFmt numFmtId=""168"" formatCode=""yyyy-mm-dd hh:mm:ss""/>
  </numFmts>
  <fonts count=""1"">
    <font>
      <sz val=""11""/>
      <color theme=""1""/>
      <name val=""Calibri""/>
      <family val=""2""/>
      <scheme val=""minor""/>
    </font>
  </fonts>
  <fills count=""1"">
    <fill>
      <patternFill patternType=""none""/>
    </fill>
  </fills>
  <borders count=""1"">
    <border>
      <left/>
      <right/>
      <top/>
      <bottom/>
      <diagonal/>
    </border>
  </borders>
  <cellStyleXfs count=""1"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/>
  </cellStyleXfs>
  <cellXfs count=""4"">
    <xf xfId=""0""/>
    <xf numFmtId=""166"" xfId=""0"" applyNumberFormat=""1""/>
    <xf numFmtId=""167"" xfId=""0"" applyNumberFormat=""1""/>
    <xf numFmtId=""168"" xfId=""0"" applyNumberFormat=""1""/>
  </cellXfs>
  <cellStyles count=""1"">
    <cellStyle name=""Normal"" builtinId=""0"" xfId=""0"" />
  </cellStyles>
</styleSheet>";
            AddEntryWithContent("xl/styles.xml", content);
        }
    }
}
