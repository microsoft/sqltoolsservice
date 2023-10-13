//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using SkiaSharp;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Writer for writing rows of results to a Excel file
    /// </summary>
    public class SaveAsExcelFileStreamWriter : SaveAsStreamWriter
    {
        // Font family used in Excel sheet
        private const string FontFamily = "Calibri";

        // Font size in Excel sheet (points with conversion to pixels)
        private const float FontSizePixels = 11F * (96F / 72F);

        // Pixel width of auto-filter button
        private const float AutoFilterPixelWidth = 17F;
        
        #region Member Variables

        private readonly SaveResultsAsExcelRequestParams saveParams;
        private readonly float[] columnWidths;
        private readonly int columnEndIndex;
        private readonly int columnStartIndex;
        private readonly SaveAsExcelFileStreamWriterHelper helper;

        private bool headerWritten;
        private SaveAsExcelFileStreamWriterHelper.ExcelSheet sheet;

        private SKPaint paint;

        #endregion

        /// <summary>
        /// Constructor, stores the Excel specific request params locally, chains into the base
        /// constructor
        /// </summary>
        /// <param name="stream">FileStream to access the Excel file output</param>
        /// <param name="requestParams">Excel save as request parameters</param>
        /// <param name="columns">
        /// The entire list of columns for the result set. They will be filtered down as per the
        /// request params.
        /// </param>
        public SaveAsExcelFileStreamWriter(Stream stream, SaveResultsAsExcelRequestParams requestParams, IReadOnlyList<DbColumnWrapper> columns)
            : base(stream, requestParams, columns)
        {
            saveParams = requestParams;
            helper = new SaveAsExcelFileStreamWriterHelper(stream);

            // Do some setup if the caller requested automatically sized columns
            if (requestParams.AutoSizeColumns)
            {
                // Set column counts depending on whether save request is for entire set or a subset
                columnEndIndex = columns.Count;
                columnStartIndex = 0;
                var columnCount = columns.Count;

                if (requestParams.IsSaveSelection)
                {
                    // ReSharper disable PossibleInvalidOperationException  IsSaveSelection verifies these values exist
                    columnEndIndex = requestParams.ColumnEndIndex.Value + 1;
                    columnStartIndex = requestParams.ColumnStartIndex.Value;
                    columnCount = columnEndIndex - columnStartIndex;
                    // ReSharper restore PossibleInvalidOperationException
                }

                columnWidths = new float[columnCount];

                // If the caller requested headers the column widths can be initially set based on the header values
                if (requestParams.IncludeHeaders)
                {
                    // Setup for measuring the header, set font style based on whether the header should be bold or not
                    using var headerPaint = new SKPaint();
                    headerPaint.Typeface = SKTypeface.FromFamilyName(FontFamily, requestParams.BoldHeaderRow ? SKFontStyle.Bold : SKFontStyle.Normal);
                    headerPaint.TextSize = FontSizePixels;
                    var skBounds = SKRect.Empty;

                    // Loop over all the columns
                    for (int columnIndex = columnStartIndex; columnIndex < columnEndIndex; ++columnIndex)
                    {
                        var columnNumber = columnIndex - columnStartIndex;

                        // Measure the header text
                        var textWidth = headerPaint.MeasureText(columns[columnIndex].ColumnName.AsSpan(), ref skBounds);

                        // Add extra for the auto filter button if requested
                        if (requestParams.AutoFilterHeaderRow)
                        {
                            textWidth += AutoFilterPixelWidth;
                        }

                        // Just store the width as a starting point
                        columnWidths[columnNumber] = textWidth;
                    }
                }
            }
        }

        /// <summary>
        /// Excel supports specifying column widths so measure if the user wants the columns automatically sized
        /// </summary>
        public override bool ShouldMeasureRowColumns => saveParams.AutoSizeColumns;

        /// <summary>
        /// Measures each column of a row of data and stores updates the maximum width of the column if needed
        /// </summary>
        /// <param name="row">The row of data to measure</param>
        public override void MeasureRowColumns(IList<DbCellValue> row)
        {
            // Create the paint object if not done already
            if (paint == null)
            {
                paint = new SKPaint();

                paint.Typeface = SKTypeface.FromFamilyName(FontFamily);
                paint.TextSize = FontSizePixels;
            }

            var skBounds = SKRect.Empty;

            // Loop over all the columns
            for (int columnIndex = columnStartIndex; columnIndex < columnEndIndex; ++columnIndex)
            {
                var columnNumber = columnIndex - columnStartIndex;

                // Measure the width of the text
                var textWidth = paint.MeasureText(row[columnIndex].DisplayValue.AsSpan(), ref skBounds);

                // Update the max if the new width is greater
                columnWidths[columnNumber] = Math.Max(columnWidths[columnNumber], textWidth);
            }
        }

        /// <summary>
        /// Writes a row of data as a Excel row. If this is the first row and the user has requested
        /// it, the headers for the column will be emitted as well.
        /// </summary>
        /// <param name="row">The data of the row to output to the file</param>
        /// <param name="columns">
        /// The entire list of columns for the result set. They will be filtered down as per the
        /// request params.
        /// </param>
        public override void WriteRow(IList<DbCellValue> row, IReadOnlyList<DbColumnWrapper> columns)
        {
            // Check to make sure the sheet has been created
            if (sheet == null)
            {
                // Get rid of any paint object from the auto-sizing
                paint?.Dispose();

                // Create the blank sheet
                sheet = helper.AddSheet(null, columns.Count);

                // The XLSX format has strict ordering requirements so these must be done in the proper order
                
                // First freeze the header row if the caller has requested header rows and that the header should be frozen
                if (saveParams.IncludeHeaders && saveParams.FreezeHeaderRow)
                {
                    sheet.FreezeHeaderRow();
                }

                // Next if column widths have been specified they should be saved to the sheet
                if (columnWidths != null)
                {
                    sheet.WriteColumnInformation(columnWidths);
                }

                // Lastly enable auto filter if the caller has requested header rows and that the header should be frozen 
                if (saveParams.IncludeHeaders && saveParams.AutoFilterHeaderRow)
                {
                    sheet.EnableAutoFilter();
                }
            }

            // Write out the header if we haven't already and the user chose to have it
            if (saveParams.IncludeHeaders && !headerWritten)
            {
                sheet.AddRow();
                for (int i = ColumnStartIndex; i <= ColumnEndIndex; i++)
                {
                    // Add the header text and bold if requested
                    sheet.AddCell(columns[i].ColumnName, saveParams.BoldHeaderRow);
                }
                headerWritten = true;
            }

            sheet.AddRow();
            for (int i = ColumnStartIndex; i <= ColumnEndIndex; i++)
            {
                sheet.AddCell(row[i]);
            }
        }

        private bool disposed;
        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            sheet.Dispose();
            helper.Dispose();

            disposed = true;
            base.Dispose(disposing);
        }

    }
}