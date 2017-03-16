// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    // A xlsx file is a zip with specific folder structure.
    // http://www.ecma-international.org/publications/standards/Ecma-376.htm

    // The page number in the comments are based on
    // ECMA-376, Fifth Edition, Part 1 - Fundamentals And Markup Language Reference 

    // Page 75, SpreadsheetML package structure
    // |- [Content_Types].xml
    // |- _rels
    //   |- .rels
    // |- xl
    //   |- workbook.xml
    //   |- styles.xml
    //   |- _rels
    //     |- workbook.xml.rels
    //   |- worksheets
    //     |- sheet1.xml

    /// <summary>
    /// A helper class for write xlsx file base on ECMA-376. It tries to be minimal,
    /// both in implementation and runtime allocation. 
    /// </summary>
    /// <example> 
    /// This sample shows how to use the class 
    /// <code>
    /// public class TestClass
    /// {
    ///     public static int Main() 
    ///     {
    ///         using (Stream stream = File.Create("test.xlsx"))
    ///         using (var helper = new SaveAsExcelFileStreamWriterHelper(stream, false))
    ///         using (var sheet = helper.AddSheet())
    ///         {
    ///             sheet.AddRow();
    ///             sheet.AddCell("string");
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>

    internal sealed class SaveAsExcelFileStreamWriterHelper : IDisposable
    {
        /// <summary>
        /// Present a Excel sheet
        /// </summary>
        public sealed class ExcelSheet : IDisposable
        {
            // The excel epoch is 1/1/1900, but it has 1/0/1900 and 2/29/1900
            // which is equal to set the epoch back two days to 12/30/1899
            // new DateTime(1899,12,30).Ticks
            private const long ExcelEpochTick = 599264352000000000L;

            // Excel can not use date before 1/0/1900 and
            // date before 3/1/1900 is wrong, off by 1 because of 2/29/1900
            // thus, for any date before 3/1/1900, use string for date
            // new DateTime(1900,3,1).Ticks
            private const long ExcelDateCutoffTick = 599317056000000000L;

            // new TimeSpan(24,0,0).Ticks
            private const long TicksPerDay = 864000000000L;

            private XmlWriter writer;
            private ReferenceManager referenceManager;
            private bool hasOpenRowTag;

            /// <summary>
            /// Initializes a new instance of the ExcelSheet class.
            /// </summary>
            /// <param name="writer">XmlWriter to write the sheet data</param>
            internal ExcelSheet(XmlWriter writer)
            {
                this.writer = writer;
                writer.WriteStartDocument();
                writer.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                writer.WriteAttributeString("xmlns", "r", null, "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
                writer.WriteStartElement("sheetData");
                referenceManager = new ReferenceManager(writer);
            }

            /// <summary>
            /// Start a new row
            /// </summary>
            public void AddRow()
            {
                EndRowIfNeeded();
                hasOpenRowTag = true;

                referenceManager.AssureRowReference();

                writer.WriteStartElement("row");
                referenceManager.WriteAndIncreaseRowReference();
            }

            /// <summary>
            /// Write a string cell
            /// </summary>
            /// <param name="value">string value to write</param>
            public void AddCell(string value)
            {
                // string needs <c t="inlineStr"><is><t>string</t></is></c>
                // This class uses inlineStr instead of more common shared string table
                // to improve write performance and reduce implementation complexity
                referenceManager.AssureColumnReference();
                if (value == null)
                {
                    AddCellEmpty();
                    return;
                }

                writer.WriteStartElement("c");

                referenceManager.WriteAndIncreaseColumnReference();

                writer.WriteAttributeString("t", "inlineStr");

                writer.WriteStartElement("is");
                writer.WriteStartElement("t");
                writer.WriteValue(value);
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            /// <summary>
            /// Write a object cell
            /// </summary>
            /// The program will try to output number/datetime, otherwise, call the ToString 
            /// <param name="o"></param>
            public void AddCell(DbCellValue dbCellValue)
            {
                if (dbCellValue.IsNull)
                {
                    AddCellEmpty();
                    return;
                }
                object o = dbCellValue.RawObject; //Todo: do I need to check null here?

                switch (Type.GetTypeCode(o.GetType()))
                {
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        AddCellBoxedNumber(o);
                        break;
                    case TypeCode.DateTime:
                        AddCell((DateTime)o);
                        break;
                    case TypeCode.String:
                        AddCell((string)o);
                        break;
                    default:
                        if (o is TimeSpan) //TimeSpan doesn't have TypeCode
                        {
                            AddCell((TimeSpan)o);
                            break;
                        }
                        AddCell(dbCellValue.DisplayValue);
                        break;
                }
            }

            /// <summary>
            /// Close the <row><sheetData><worksheet> tags and close the stream
            /// </summary>
            public void Dispose()
            {
                EndRowIfNeeded();
                writer.WriteEndElement(); // <sheetData> 
                writer.WriteEndElement(); // <worksheet>
                writer.Dispose();
            }

            /// <summary>
            /// Write a empty cell
            /// </summary>
            /// This only increases the internal bookmark and doesn't arcturally write out anything.
            private void AddCellEmpty()
            {
                referenceManager.IncreaseColumnReference();
            }

            /// <summary>
            /// Write a TimeSpan cell. 
            /// </summary>
            /// <param name="time"></param>
            private void AddCell(TimeSpan time)
            {
                referenceManager.AssureColumnReference();
                double excelDate = (double)time.Ticks / (double)TicksPerDay;
                // The default hh:mm:ss format do not support more than 24 hours
                // For that case, use the format string [h]:mm:ss
                if (time.Ticks >= TicksPerDay)
                {
                    AddCellDateTimeInternal(excelDate, Style.TimeMoreThan24Hours);
                }
                else
                {
                    AddCellDateTimeInternal(excelDate, Style.Time);
                }
            }

            /// <summary>
            /// Write a DateTime cell.
            /// </summary>
            /// <param name="dateTime">Datetime</param>
            /// <remark>
            /// If the DateTime does not have date part, it will be written as datetime and show as time only
            /// If the DateTime is before 1900-03-01, save as string because excel doesn't support them.
            /// Otherwise, save as datetime, and if the time is 00:00:00, show as yyyy-MM-dd.
            /// Show the datetime as yyyy-MM-dd HH:mm:ss if none of the previous situations
            /// </remark>
            private void AddCell(DateTime dateTime)
            {
                referenceManager.AssureColumnReference();
                long ticks = dateTime.Ticks;
                Style style = Style.DateTime;
                double excelDate;
                if (ticks < TicksPerDay) //date empty, time only
                {
                    style = Style.Time;
                    excelDate = ((double)ticks) / (double)TicksPerDay;
                }
                else if (ticks < ExcelDateCutoffTick) //before excel cut-off, use string
                {
                    AddCell(dateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                    return;
                }
                else
                {
                    if (ticks % TicksPerDay == 0) //time empty, date only
                    {
                        style = Style.Date;
                    }
                    excelDate = ((double)(ticks - ExcelEpochTick)) / (double)TicksPerDay;
                }
                AddCellDateTimeInternal(excelDate, style);
            }

            // number needs <c r="A1"><v>12.5</v></c>
            private void AddCellBoxedNumber(object number)
            {
                referenceManager.AssureColumnReference();

                writer.WriteStartElement("c");

                referenceManager.WriteAndIncreaseColumnReference();

                writer.WriteStartElement("v");
                writer.WriteValue(number);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }


            // datetime needs <c r="A1" s="2"><v>26012.451</v></c>
            private void AddCellDateTimeInternal(double excelDate, Style style)
            {
                writer.WriteStartElement("c");

                referenceManager.WriteAndIncreaseColumnReference();

                writer.WriteStartAttribute("s");
                writer.WriteValue((int)style);
                writer.WriteEndAttribute();

                writer.WriteStartElement("v");
                writer.WriteValue(excelDate);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            private void EndRowIfNeeded()
            {
                if (hasOpenRowTag)
                {
                    writer.WriteEndElement(); // <row>
                }
            }


        }

        /// <summary>
        /// Helper class to track the current cell reference.
        /// </summary>
        /// <remarks>
        /// SpreadsheetML cell needs a reference attribute. (e.g. r="A1"). This class is used
        /// to track the current cell reference.
        /// </remarks>
        internal class ReferenceManager
        {
            private int currColumn; // 0 is invalid, the first AddRow will set to 1
            private int currRow = 1;

            // In order to reduce allocation, current reference is saved in this array,
            // and write to the XmlWriter through WriteChars.
            // For example, when the reference has value AA15,
            // The content of this array will be @AA15xxxxx, with currReferenceRowLength=2
            // and currReferenceColumnLength=2 
            private char[] currReference = new char[3 + 7]; //maximal XFD1048576
            private int currReferenceRowLength;
            private int currReferenceColumnLength;

            private XmlWriter writer;

            /// <summary>
            /// Initializes a new instance of the ReferenceManager class.   
            /// </summary>
            /// <param name="writer">XmlWriter to write the reference attribute to.</param>
            public ReferenceManager(XmlWriter writer)
            {
                this.writer = writer;
            }

            /// <summary>
            /// Check that we have not write too many columns. (xlsx has a limit of 16384 columns)
            /// </summary>
            public void AssureColumnReference()
            {
                if (currColumn == 0)
                {
                    throw new InvalidOperationException("AddRow must be called before AddCell");

                }
                if (currColumn > 16384)
                {
                    throw new InvalidOperationException("max column number is 16384, see https://support.office.com/en-us/article/Excel-specifications-and-limits-1672b34d-7043-467e-8e27-269d656771c3");
                }
            }

            /// <summary>
            /// Write out the r="A1" attribute and increase the column number of internal bookmark
            /// </summary>
            public void WriteAndIncreaseColumnReference()
            {
                writer.WriteStartAttribute("r");
                writer.WriteChars(currReference, 3 - currReferenceColumnLength, currReferenceRowLength + currReferenceColumnLength);
                writer.WriteEndAttribute();
                IncreaseColumnReference();
            }

            /// <summary>
            /// Increase the column of internal bookmark. 
            /// </summary>
            public void IncreaseColumnReference()
            {
                // This function change the first three chars of currReference array
                // The logic is simple, when a start a new row, the array is reset to @@A
                // where @='A'-1. At each increase, check if the current reference is Z
                // and move to AA if needed, since the maximal is 16384, or XFD, the code
                // manipulates the array element directly instead of loop
                char[] reference = currReference;
                currColumn++;
                if ('Z' == reference[2]++)
                {
                    reference[2] = 'A';
                    if (currReferenceColumnLength < 2)
                    {
                        currReferenceColumnLength = 2;
                    }
                    if ('Z' == reference[1]++)
                    {
                        reference[0]++;
                        reference[1] = 'A';
                        currReferenceColumnLength = 3;
                    }
                }
            }

            /// <summary>
            /// Check that we have not write too many rows. (xlsx has a limit of 1048576 rows) 
            /// </summary>
            public void AssureRowReference()
            {
                if (currRow > 1048576)
                {
                    throw new InvalidOperationException("max row number is 1048576, see https://support.office.com/en-us/article/Excel-specifications-and-limits-1672b34d-7043-467e-8e27-269d656771c3");
                }
            }
            /// <summary>
            /// Write out the r="1" attribute and increase the row number of internal bookmark
            /// </summary>
            public void WriteAndIncreaseRowReference()
            {
                writer.WriteStartAttribute("r");
                writer.WriteValue(currRow);
                writer.WriteEndAttribute();

                ResetColumnReference(); //This need to be called before the increase

                currRow++;
            }

            // Reset the Column Reference
            // This will reset the first three chars of currReference array to '@@A'
            // and the rest to the array to the string presentation of the current row.
            private void ResetColumnReference()
            {
                currColumn = 1;
                currReference[0] = currReference[1] = (char)('A' - 1);
                currReference[2] = 'A';
                currReferenceColumnLength = 1;

                string rowReference = XmlConvert.ToString(currRow);
                currReferenceRowLength = rowReference.Length;
                rowReference.CopyTo(0, currReference, 3, rowReference.Length);
            }
        }

        private enum Style
        {
            Normal = 0,
            Date = 1,
            Time = 2,
            DateTime = 3,
            TimeMoreThan24Hours = 4,
        }

        private ZipArchive zipArchive;
        private List<string> sheetNames = new List<string>();
        private XmlWriterSettings writerSetting = new XmlWriterSettings()
        {
            CloseOutput = true,
        };

        /// <summary>
        /// Initializes a new instance of the SaveAsExcelFileStreamWriterHelper class.  
        /// </summary>
        /// <param name="stream">The input or output stream.</param>
        public SaveAsExcelFileStreamWriterHelper(Stream stream)
        {
            zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, false);
        }

        /// <summary>
        /// Initializes a new instance of the SaveAsExcelFileStreamWriterHelper class. 
        /// </summary>
        /// <param name="stream">The input or output stream.</param>
        /// <param name="leaveOpen">true to leave the stream open after the 
        /// SaveAsExcelFileStreamWriterHelper object is disposed; otherwise, false.</param>
        public SaveAsExcelFileStreamWriterHelper(Stream stream, bool leaveOpen)
        {
            zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen);
        }

        /// <summary>
        /// Add sheet inside the Xlsx file.
        /// </summary>
        /// <param name="sheetName">Sheet name</param>
        /// <returns>ExcelSheet for writing the sheet content</returns>
        /// <remarks>
        /// When the sheetName is null, sheet1,shhet2,..., will be used.
        /// The following charactors are not allowed in the sheetName
        /// '\', '/','*','[',']',':','?'
        /// </remarks>
        public ExcelSheet AddSheet(string sheetName = null)
        {
            string sheetFileName = "sheet" + (sheetNames.Count + 1);
            if (sheetName == null)
            {
                sheetName = sheetFileName;
            }
            EnsureValidSheetName(sheetName);

            sheetNames.Add(sheetName);
            XmlWriter sheetWriter = AddEntry($"xl/worksheets/{sheetFileName}.xml");
            return new ExcelSheet(sheetWriter);
        }

        /// <summary>
        /// Write out the rest of the xlsx files and release the resources used by the current instance 
        /// </summary>
        public void Dispose()
        {
            WriteMinimalTemplate();
            zipArchive.Dispose();
        }


        private XmlWriter AddEntry(string entryName)
        {
            ZipArchiveEntry entry = zipArchive.CreateEntry(entryName, CompressionLevel.Fastest);
            return XmlWriter.Create(entry.Open(), writerSetting);
        }

        //ECMA-376 page 75
        private void WriteMinimalTemplate()
        {
            WriteTopRel();
            WriteWorkbook();
            WriteStyle();
            WriteContentType();
            WriteWorkbookRel();
        }

        /// <summary>
        /// write [Content_Types].xml
        /// </summary>
        /// <remarks>
        /// This file need to describe all the files in the zip.
        /// </remarks>
        private void WriteContentType()
        {
            using (XmlWriter xw = AddEntry("[Content_Types].xml"))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");

                xw.WriteStartElement("Default");
                xw.WriteAttributeString("Extension", "rels");
                xw.WriteAttributeString("ContentType", "application/vnd.openxmlformats-package.relationships+xml");
                xw.WriteEndElement();

                xw.WriteStartElement("Override");
                xw.WriteAttributeString("PartName", "/xl/workbook.xml");
                xw.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
                xw.WriteEndElement();

                xw.WriteStartElement("Override");
                xw.WriteAttributeString("PartName", "/xl/styles.xml");
                xw.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
                xw.WriteEndElement();

                for (int i = 1; i <= sheetNames.Count; ++i)
                {
                    xw.WriteStartElement("Override");
                    xw.WriteAttributeString("PartName", "/xl/worksheets/sheet" + i + ".xml");
                    xw.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
                    xw.WriteEndElement();
                }
                xw.WriteEndElement();
                xw.WriteEndDocument();
            }
        }

        /// <summary>
        /// Write _rels/.rels. This file only need to reference main workbook
        /// </summary>
        private void WriteTopRel()
        {
            using (XmlWriter xw = AddEntry("_rels/.rels"))
            {
                xw.WriteStartDocument();

                xw.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");

                xw.WriteStartElement("Relationship");
                xw.WriteAttributeString("Id", "rId1");
                xw.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument");
                xw.WriteAttributeString("Target", "xl/workbook.xml");
                xw.WriteEndElement();

                xw.WriteEndElement();

                xw.WriteEndDocument();
            }
        }

        private static char[] invalidSheetNameCharacters = new char[]
        {
            '\\', '/','*','[',']',':','?'
        };
        private void EnsureValidSheetName(string sheetName)
        {
            if (sheetName.IndexOfAny(invalidSheetNameCharacters) != -1)
            {
                throw new ArgumentException($"Invalid sheetname: sheetName");
            }
            if (sheetNames.IndexOf(sheetName) != -1)
            {
                throw new ArgumentException($"Duplicate sheetName: {sheetName}");
            }
        }

        /// <summary>
        /// Write xl/workbook.xml. This file will references the sheets through ids in xl/_rels/workbook.xml.rels
        /// </summary>
        private void WriteWorkbook()
        {
            using (XmlWriter xw = AddEntry("xl/workbook.xml"))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("workbook", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                xw.WriteAttributeString("xmlns", "r", null, "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
                xw.WriteStartElement("sheets");
                for (int i = 1; i <= sheetNames.Count; i++)
                {
                    xw.WriteStartElement("sheet");
                    xw.WriteAttributeString("name", sheetNames[i - 1]);
                    xw.WriteAttributeString("sheetId", i.ToString());
                    xw.WriteAttributeString("r", "id", null, "rId" + i);
                    xw.WriteEndElement();
                }
                xw.WriteEndDocument();
            }
        }

        /// <summary>
        /// Write xl/_rels/workbook.xml.rels. This file will have the paths of the style and sheets.
        /// </summary>
        private void WriteWorkbookRel()
        {
            using (XmlWriter xw = AddEntry("xl/_rels/workbook.xml.rels"))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");

                xw.WriteStartElement("Relationship");
                xw.WriteAttributeString("Id", "rId0");
                xw.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles");
                xw.WriteAttributeString("Target", "styles.xml");
                xw.WriteEndElement();

                for (int i = 1; i <= sheetNames.Count; i++)
                {
                    xw.WriteStartElement("Relationship");
                    xw.WriteAttributeString("Id", "rId" + i);
                    xw.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet");
                    xw.WriteAttributeString("Target", "worksheets/sheet" + i + ".xml");
                    xw.WriteEndElement();
                }
                xw.WriteEndElement();
                xw.WriteEndDocument();
            }
        }

        // Write the xl/styles.xml
        private void WriteStyle()
        {
            // the style 0 is used for general case, style 1 for date, style 2 for time and style 3 for datetime see Enum Style
            // reference chain: (index start with 0)
            // <c>(in sheet1.xml) --> (by s) <cellXfs> --> (by xfId) <cellStyleXfs>
            //                                               --> (by numFmtId) <numFmts>
            // that is <c s="1"></c> will reference the second element of <cellXfs> <xf numFmtId=""162"" xfId=""0"" applyNumberFormat=""1""/>
            // then, this xf reference numFmt by name and get formatCode "hh:mm:ss"

            using (XmlWriter xw = AddEntry("xl/styles.xml"))
            {
                xw.WriteStartElement("styleSheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");

                xw.WriteStartElement("numFmts");
                xw.WriteAttributeString("count", "4");
                xw.WriteStartElement("numFmt");
                xw.WriteAttributeString("numFmtId", "166");
                xw.WriteAttributeString("formatCode", "yyyy-mm-dd");
                xw.WriteEndElement();
                xw.WriteStartElement("numFmt");
                xw.WriteAttributeString("numFmtId", "167");
                xw.WriteAttributeString("formatCode", "hh:mm:ss");
                xw.WriteEndElement();
                xw.WriteStartElement("numFmt");
                xw.WriteAttributeString("numFmtId", "168");
                xw.WriteAttributeString("formatCode", "yyyy-mm-dd hh:mm:ss");
                xw.WriteEndElement();
                xw.WriteStartElement("numFmt");
                xw.WriteAttributeString("numFmtId", "169");
                xw.WriteAttributeString("formatCode", "[h]:mm:ss");
                xw.WriteEndElement();
                xw.WriteEndElement(); //mumFmts


                xw.WriteStartElement("fonts");
                xw.WriteAttributeString("count", "1");
                xw.WriteStartElement("font");
                xw.WriteStartElement("sz");
                xw.WriteAttributeString("val", "11");
                xw.WriteEndElement();
                xw.WriteStartElement("color");
                xw.WriteAttributeString("theme", "1");
                xw.WriteEndElement();
                xw.WriteStartElement("name");
                xw.WriteAttributeString("val", "Calibri");
                xw.WriteEndElement();
                xw.WriteStartElement("family");
                xw.WriteAttributeString("val", "2");
                xw.WriteEndElement();
                xw.WriteStartElement("scheme");
                xw.WriteAttributeString("val", "minor");
                xw.WriteEndElement();
                xw.WriteEndElement(); // font
                xw.WriteEndElement(); // fonts

                xw.WriteStartElement("fills");
                xw.WriteAttributeString("count", "1");
                xw.WriteStartElement("fill");
                xw.WriteStartElement("patternFill");
                xw.WriteAttributeString("patternType", "none");
                xw.WriteEndElement(); // patternFill
                xw.WriteEndElement(); // fill
                xw.WriteEndElement(); // fills

                xw.WriteStartElement("borders");
                xw.WriteAttributeString("count", "1");
                xw.WriteStartElement("border");
                xw.WriteElementString("left", null);
                xw.WriteElementString("right", null);
                xw.WriteElementString("top", null);
                xw.WriteElementString("bottom", null);
                xw.WriteElementString("diagonal", null);
                xw.WriteEndElement(); // board
                xw.WriteEndElement(); // borders

                xw.WriteStartElement("cellStyleXfs");
                xw.WriteAttributeString("count", "1");
                xw.WriteStartElement("xf");
                xw.WriteAttributeString("numFmtId", "0");
                xw.WriteAttributeString("fontId", "0");
                xw.WriteAttributeString("fillId", "0");
                xw.WriteAttributeString("borderId", "0");
                xw.WriteEndElement(); // xf
                xw.WriteEndElement(); // cellStyleXfs

                xw.WriteStartElement("cellXfs");
                xw.WriteAttributeString("count", "5");
                xw.WriteStartElement("xf");
                xw.WriteAttributeString("xfId", "0");
                xw.WriteEndElement();
                xw.WriteStartElement("xf");
                xw.WriteAttributeString("numFmtId", "166");
                xw.WriteAttributeString("xfId", "0");
                xw.WriteAttributeString("applyNumberFormat", "1");
                xw.WriteEndElement();
                xw.WriteStartElement("xf");
                xw.WriteAttributeString("numFmtId", "167");
                xw.WriteAttributeString("xfId", "0");
                xw.WriteAttributeString("applyNumberFormat", "1");
                xw.WriteEndElement();
                xw.WriteStartElement("xf");
                xw.WriteAttributeString("numFmtId", "168");
                xw.WriteAttributeString("xfId", "0");
                xw.WriteAttributeString("applyNumberFormat", "1");
                xw.WriteEndElement();
                xw.WriteStartElement("xf");
                xw.WriteAttributeString("numFmtId", "169");
                xw.WriteAttributeString("xfId", "0");
                xw.WriteAttributeString("applyNumberFormat", "1");
                xw.WriteEndElement();
                xw.WriteEndElement(); // cellXfs

                xw.WriteStartElement("cellStyles");
                xw.WriteAttributeString("count", "1");
                xw.WriteStartElement("cellStyle");
                xw.WriteAttributeString("name", "Normal");
                xw.WriteAttributeString("builtinId", "0");
                xw.WriteAttributeString("xfId", "0");
                xw.WriteEndElement(); // cellStyle
                xw.WriteEndElement(); // cellStyles
            }
        }
    }
}
