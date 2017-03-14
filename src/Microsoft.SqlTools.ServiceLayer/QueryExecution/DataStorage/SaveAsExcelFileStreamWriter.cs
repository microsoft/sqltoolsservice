// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Writer for writing rows of results to a Excel file
    /// </summary>
    public class SaveAsExcelFileStreamWriter : SaveAsStreamWriter
    {

        #region Member Variables

        private readonly SaveResultsAsExcelRequestParams saveParams;
        private bool headerWritten;
        private SaveAsExcelFileStreamWriterHelper helper;
        private SaveAsExcelFileStreamWriterHelper.ExcelSheet sheet;

        #endregion

        /// <summary>
        /// Constructor, stores the Excel specific request params locally, chains into the base 
        /// constructor
        /// </summary>
        /// <param name="stream">FileStream to access the Excel file output</param>
        /// <param name="requestParams">Excel save as request parameters</param>
        public SaveAsExcelFileStreamWriter(Stream stream, SaveResultsAsExcelRequestParams requestParams)
            : base(stream, requestParams)
        {
            saveParams = requestParams;
            helper = new SaveAsExcelFileStreamWriterHelper(stream);
            sheet = helper.AddSheet();
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
        public override void WriteRow(IList<DbCellValue> row, IList<DbColumnWrapper> columns)
        {
            int startIndex = ColumnStartIndex ?? 0;
            int endIndex = ColumnCount.HasValue ? startIndex + ColumnCount.Value : columns.Count;
            //Although this shoule not be needed, add this line to match the Take in other writer
            if (endIndex > columns.Count)
            {
                endIndex = columns.Count;
            }
            // Write out the header if we haven't already and the user chose to have it
            if (saveParams.IncludeHeaders && !headerWritten)
            {
                sheet.AddRow();
                for (int i = startIndex; i < endIndex; ++i)
                {
                    sheet.AddCell(columns[i].ColumnName);
                }
                headerWritten = true;
            }

            sheet.AddRow();
            for (int i = startIndex; i < endIndex; ++i)
            {
                object o = row[i].RawObject;
                if (o != null) {
                    TypeCode type = Type.GetTypeCode(o.GetType());
                    switch (type)
                    {
                        case TypeCode.Byte:
                        case TypeCode.SByte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                            sheet.AddCell(o);
                            continue;
                        case TypeCode.DateTime:
                            sheet.AddCell((DateTime)o);
                            continue;
                    }
                }
                sheet.AddCell(row[i].DisplayValue);
            }
        }

        private bool disposed;
        override protected void Dispose(bool disposing)
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